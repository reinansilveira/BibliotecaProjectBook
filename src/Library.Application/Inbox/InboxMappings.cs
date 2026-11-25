using Library.Domain.Entities;

namespace Library.Application.Inbox;

public static class InboxMappings
{
    public static NotificationResponse ToResponse(this Notification notification) =>
        new(notification.Id, notification.Type, notification.Title, notification.Message, notification.Link, notification.CreatedAt, notification.ReadAt);
}
