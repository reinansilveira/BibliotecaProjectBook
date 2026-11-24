using Library.Application.Common.Pagination;

namespace Library.Application.Inbox;

public sealed class NotificationListRequest : PageRequest
{
    public bool UnreadOnly { get; init; }
}
