using Library.Application.Events;

namespace Library.Application.Abstractions;

public interface IEventPublisher
{
    void Publish(IIntegrationEvent integrationEvent);
}
