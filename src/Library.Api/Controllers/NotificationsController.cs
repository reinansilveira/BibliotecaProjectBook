using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Common.Pagination;
using Library.Application.Inbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/notifications")]
[Produces("application/json")]
[Authorize(Policy = Policies.MemberOrAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class NotificationsController : ControllerBase
{
    private readonly IInboxService _inboxService;

    public NotificationsController(IInboxService inboxService)
    {
        _inboxService = inboxService;
    }

    private Guid MemberId => User.GetMemberId()!.Value;

    /// <summary>Notifications of the signed-in member, newest first. New ones are also pushed in real time through the /hubs/notifications SignalR hub.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<NotificationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> List([FromQuery] NotificationListRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _inboxService.ListAsync(MemberId, request, cancellationToken));
    }

    /// <summary>Number of unread notifications, for the bell badge.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await _inboxService.CountUnreadAsync(MemberId, cancellationToken));
    }

    /// <summary>Marks one notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _inboxService.MarkReadAsync(MemberId, id, cancellationToken);

        return NoContent();
    }

    /// <summary>Marks every notification as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _inboxService.MarkAllReadAsync(MemberId, cancellationToken);

        return NoContent();
    }
}
