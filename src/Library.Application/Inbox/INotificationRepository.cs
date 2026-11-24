using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Inbox;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> ListAsync(Guid memberId, bool unreadOnly, PageRequest request, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    void Add(Notification notification);
}
