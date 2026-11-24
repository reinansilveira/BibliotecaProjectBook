using Library.Application.Abstractions;
using Library.Application.Inbox;
using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Realtime;

public sealed class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRNotifier(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public Task SendAsync(Guid memberId, NotificationResponse notification, CancellationToken cancellationToken = default) =>
        _hub.Clients.User(memberId.ToString()).SendAsync(NotificationsHub.NotificationMethod, notification, cancellationToken);
}
