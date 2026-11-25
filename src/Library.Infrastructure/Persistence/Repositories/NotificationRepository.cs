using Library.Application.Common.Pagination;
using Library.Application.Inbox;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly LibraryDbContext _context;

    public NotificationRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public Task<PagedResult<Notification>> ListAsync(Guid memberId, bool unreadOnly, PageRequest request, CancellationToken cancellationToken = default)
    {
        var notifications = _context.Notifications.AsNoTracking().Where(notification => notification.MemberId == memberId);

        if (unreadOnly)
        {
            notifications = notifications.Where(notification => notification.ReadAt == null);
        }

        return notifications
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenBy(notification => notification.Id)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public Task<int> CountUnreadAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _context.Notifications.CountAsync(notification => notification.MemberId == memberId && notification.ReadAt == null, cancellationToken);

    public Task<int> MarkAllReadAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        _context.Notifications
            .Where(notification => notification.MemberId == memberId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAt, now), cancellationToken);

    public void Add(Notification notification) => _context.Notifications.Add(notification);
}
