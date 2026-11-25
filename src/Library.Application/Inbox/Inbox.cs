using Library.Domain.Entities;

namespace Library.Application.Inbox;

public sealed class Inbox : IInbox
{
    private readonly INotificationRepository _notifications;
    private readonly TimeProvider _timeProvider;

    public Inbox(INotificationRepository notifications, TimeProvider timeProvider)
    {
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    public void Add(Guid memberId, string type, string title, string message, string? link = null)
    {
        _notifications.Add(new Notification(memberId, type, title, message, link, _timeProvider.GetUtcNow().UtcDateTime));
    }
}
