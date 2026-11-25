using Library.Application.Abstractions;
using Library.Application.Events;

namespace Library.UnitTests.Fakes;

public sealed class RecordingEventPublisher : IEventPublisher
{
    public List<IIntegrationEvent> Events { get; } = [];

    public void Publish(IIntegrationEvent integrationEvent) => Events.Add(integrationEvent);
}
