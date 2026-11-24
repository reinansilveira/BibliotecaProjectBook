using System.Text.Json;
using Library.Application.Events;
using Library.Application.Notifications;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Library.Infrastructure.Messaging;

public sealed class NotificationConsumer : BackgroundService
{
    private const ushort PrefetchCount = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionProvider connectionProvider,
        TimeProvider timeProvider,
        ILogger<NotificationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionProvider = connectionProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "RabbitMQ is unavailable. Retrying in {Seconds} seconds.", RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await MessagingTopology.DeclareTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(MessagingTopology.NotificationsQueue, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("Listening to {Queue}.", MessagingTopology.NotificationsQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessAsync(delivery, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Failed to process message {MessageId}. It was sent to the dead-letter queue.", delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    private async Task ProcessAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        var integrationEvent = Deserialize(delivery);
        var messageId = Guid.Parse(delivery.BasicProperties.MessageId ?? throw new InvalidOperationException("The message has no id."));

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        if (await context.ProcessedMessages.AnyAsync(message => message.MessageId == messageId, cancellationToken))
        {
            _logger.LogInformation("Message {MessageId} was already processed and will be ignored.", messageId);
            return;
        }

        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notifications.HandleAsync(integrationEvent, cancellationToken);

        context.ProcessedMessages.Add(new ProcessedMessage(messageId, _timeProvider.GetUtcNow().UtcDateTime));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IIntegrationEvent Deserialize(BasicDeliverEventArgs delivery)
    {
        var eventType = IntegrationEventRegistry.FindType(delivery.RoutingKey)
            ?? throw new InvalidOperationException($"Unknown event '{delivery.RoutingKey}'.");

        var integrationEvent = JsonSerializer.Deserialize(delivery.Body.Span, eventType, IntegrationEventRegistry.SerializerOptions);

        return integrationEvent as IIntegrationEvent
            ?? throw new InvalidOperationException($"The body of '{delivery.RoutingKey}' is not a valid event.");
    }
}
