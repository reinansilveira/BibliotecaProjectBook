using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Library.Infrastructure.Messaging;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly MessagingSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionProvider connectionProvider,
        IOptions<MessagingSettings> settings,
        TimeProvider timeProvider,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionProvider = connectionProvider;
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.OutboxPollingIntervalSeconds));

        do
        {
            await ProcessSafelyAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProcessBatchAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Failed to process the outbox batch.");
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var messages = await LockPendingMessagesAsync(context, cancellationToken);

            if (messages.Count == 0)
            {
                return;
            }

            await PublishAsync(messages, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private Task<List<OutboxMessage>> LockPendingMessagesAsync(LibraryDbContext context, CancellationToken cancellationToken) =>
        context.OutboxMessages
            .FromSql($"""
                SELECT TOP ({_settings.OutboxBatchSize}) *
                FROM dbo.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE ProcessedAt IS NULL AND Attempts < {_settings.MaxPublishAttempts}
                ORDER BY OccurredAt
                """)
            .ToListAsync(cancellationToken);

    private async Task PublishAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(options, cancellationToken);

        await MessagingTopology.DeclareTopologyAsync(channel, cancellationToken);

        foreach (var message in messages)
        {
            await PublishMessageAsync(channel, message, cancellationToken);
        }
    }

    private async Task PublishMessageAsync(IChannel channel, OutboxMessage message, CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAt, DateTimeKind.Utc)).ToUnixTimeSeconds())
        };

        try
        {
            var body = System.Text.Encoding.UTF8.GetBytes(message.Payload);
            await channel.BasicPublishAsync(MessagingTopology.Exchange, message.Type, mandatory: false, properties, body, cancellationToken);
            message.MarkAsProcessed(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            message.RegisterFailure(exception.Message);
            _logger.LogWarning(exception, "Failed to publish outbox message {MessageId} ({Type}).", message.Id, message.Type);
        }
    }
}
