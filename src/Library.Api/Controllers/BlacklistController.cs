using Library.Api.Authorization;
using Library.Application.Blacklist;
using Library.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/blacklist")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class BlacklistController : ControllerBase
{
    private readonly IBlacklistService _blacklistService;

    public BlacklistController(IBlacklistService blacklistService)
    {
        _blacklistService = blacklistService;
    }

    /// <summary>Lists members blocked because of overdue returns, paginated.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<BlacklistEntryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BlacklistEntryResponse>>> List([FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _blacklistService.ListAsync(request, cancellationToken));
    }

    /// <summary>Runs the overdue check now instead of waiting for the scheduled job. Returns how many members were added.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> Refresh(CancellationToken cancellationToken)
    {
        return Ok(await _blacklistService.RefreshAsync(cancellationToken));
    }

    /// <summary>Removes a member from the blacklist. Only allowed when the member has no overdue loan.</summary>
    [HttpDelete("{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Remove(Guid memberId, CancellationToken cancellationToken)
    {
        await _blacklistService.RemoveAsync(memberId, cancellationToken);

        return NoContent();
    }
}
