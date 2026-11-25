using Library.Application.Inbox;

namespace Library.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task SendAsync(Guid memberId, NotificationResponse notification, CancellationToken cancellationToken = default);
}
