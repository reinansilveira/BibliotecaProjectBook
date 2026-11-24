using Library.Application.Common.Pagination;

namespace Library.Application.Inbox;

public interface IInboxService
{
    Task<PagedResult<NotificationResponse>> ListAsync(Guid memberId, NotificationListRequest request, CancellationToken cancellationToken = default);

    Task<UnreadCountResponse> CountUnreadAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid memberId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid memberId, CancellationToken cancellationToken = default);
}
