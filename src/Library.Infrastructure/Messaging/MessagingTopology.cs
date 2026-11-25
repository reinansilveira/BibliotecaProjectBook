using RabbitMQ.Client;

namespace Library.Infrastructure.Messaging;

public static class MessagingTopology
{
    public const string Exchange = "library.events";
    public const string NotificationsQueue = "library.notifications";
    public const string DeadLetterQueue = "library.notifications.dead-letter";

    public static Task DeclareExchangeAsync(IChannel channel, CancellationToken cancellationToken) =>
        channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

    public static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await DeclareExchangeAsync(channel, cancellationToken);

        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = DeadLetterQueue
        };

        await channel.QueueDeclareAsync(NotificationsQueue, durable: true, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: cancellationToken);

        foreach (var routingKey in IntegrationEventRegistry.RoutingKeys)
        {
            await channel.QueueBindAsync(NotificationsQueue, Exchange, routingKey, cancellationToken: cancellationToken);
        }
    }
}
