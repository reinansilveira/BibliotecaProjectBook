using Library.Application.Abstractions;
using Library.Application.Inbox;

namespace Library.Infrastructure.Notifications;

public sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task SendAsync(Guid memberId, NotificationResponse notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
