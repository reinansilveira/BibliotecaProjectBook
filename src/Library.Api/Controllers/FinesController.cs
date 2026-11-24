using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Common.Pagination;
using Library.Application.Fines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/fines")]
[Produces("application/json")]
[Authorize(Policy = Policies.MemberOrAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class FinesController : ControllerBase
{
    private readonly IFineService _fineService;
    private readonly IAuthorizationService _authorizationService;

    public FinesController(IFineService fineService, IAuthorizationService authorizationService)
    {
        _fineService = fineService;
        _authorizationService = authorizationService;
    }

    /// <summary>All fines, unpaid first, optionally filtered by status. Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<PagedResult<FineResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FineResponse>>> List([FromQuery] FineListRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _fineService.ListAsync(null, request, cancellationToken));
    }

    /// <summary>Gets a fine (the member or an admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FineResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FineResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var fine = await _fineService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, fine.MemberId))
        {
            return Forbid();
        }

        return Ok(fine);
    }

    /// <summary>Pays a fine. The payment is simulated and returns a reference; no real money is involved.</summary>
    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType<FineResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FineResponse>> Pay(Guid id, CancellationToken cancellationToken)
    {
        var fine = await _fineService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, fine.MemberId))
        {
            return Forbid();
        }

        return Ok(await _fineService.PayAsync(id, cancellationToken));
    }

    /// <summary>Waives a fine. Admin only.</summary>
    [HttpPost("{id:guid}/waive")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<FineResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FineResponse>> Waive(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _fineService.WaiveAsync(id, cancellationToken));
    }
}
