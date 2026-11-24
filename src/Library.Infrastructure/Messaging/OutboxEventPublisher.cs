using System.Text.Json;
using Library.Application.Abstractions;
using Library.Application.Events;
using Library.Infrastructure.Persistence;

namespace Library.Infrastructure.Messaging;

public sealed class OutboxEventPublisher : IEventPublisher
{
    private readonly LibraryDbContext _context;
    private readonly TimeProvider _timeProvider;

    public OutboxEventPublisher(LibraryDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public void Publish(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();
        var payload = JsonSerializer.Serialize(integrationEvent, eventType, IntegrationEventRegistry.SerializerOptions);
        var message = new OutboxMessage(
            Guid.NewGuid(),
            IntegrationEventRegistry.GetName(eventType),
            payload,
            _timeProvider.GetUtcNow().UtcDateTime);

        _context.OutboxMessages.Add(message);
    }
}
