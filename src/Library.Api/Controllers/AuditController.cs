using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Audit;
using Library.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/audit")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>Who changed what and when, recorded automatically by an EF Core interceptor. Filter by entity, actor or period. Admin only.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditEntryResponse>>> List([FromQuery] AuditListRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _auditService.ListAsync(request, cancellationToken));
    }
}
