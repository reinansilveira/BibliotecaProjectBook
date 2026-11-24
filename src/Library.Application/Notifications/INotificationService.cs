using Library.Application.Events;

namespace Library.Application.Notifications;

public interface INotificationService
{
    Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
