using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/maintenance")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    /// <summary>
    /// Runs now what the scheduled job runs every 15 minutes: ends expired e-book loans, releases holds not picked up in 48 hours,
    /// sends due-tomorrow reminders and overdue notices.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType<MaintenanceResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceResult>> Run(CancellationToken cancellationToken)
    {
        return Ok(await _maintenanceService.RunAsync(cancellationToken));
    }
}
