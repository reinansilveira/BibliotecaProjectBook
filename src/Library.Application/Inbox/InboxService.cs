using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;

namespace Library.Application.Inbox;

public sealed class InboxService : IInboxService
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public InboxService(INotificationRepository notifications, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<NotificationResponse>> ListAsync(Guid memberId, NotificationListRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _notifications.ListAsync(memberId, request.UnreadOnly, request, cancellationToken);

        return page.Map(notification => notification.ToResponse());
    }

    public async Task<UnreadCountResponse> CountUnreadAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        new(await _notifications.CountUnreadAsync(memberId, cancellationToken));

    public async Task MarkReadAsync(Guid memberId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(notificationId, cancellationToken);

        if (notification is null || notification.MemberId != memberId)
        {
            throw new NotFoundException("Notification not found.");
        }

        notification.MarkRead(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task MarkAllReadAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _notifications.MarkAllReadAsync(memberId, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
}
