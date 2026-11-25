using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/reservations")]
[Produces("application/json")]
[Authorize(Policy = Policies.MemberOrAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IAuthorizationService _authorizationService;

    public ReservationsController(IReservationService reservationService, IAuthorizationService authorizationService)
    {
        _reservationService = reservationService;
        _authorizationService = authorizationService;
    }

    /// <summary>Gets a reservation with its position in the waiting list (the member or an admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await _reservationService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, reservation.MemberId))
        {
            return Forbid();
        }

        return Ok(reservation);
    }

    /// <summary>
    /// Joins the waiting list of a book that has no free copy or license. When one is returned, the first member in the list
    /// is notified in real time and by email, and the book is held for 48 hours.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> Create(ReservationRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, request.MemberId))
        {
            return Forbid();
        }

        var reservation = await _reservationService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id, version = ApiRoutes.Version1 }, reservation);
    }

    /// <summary>Leaves the waiting list or gives up a held book, which goes to the next member.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await _reservationService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, reservation.MemberId))
        {
            return Forbid();
        }

        await _reservationService.CancelAsync(id, cancellationToken);

        return NoContent();
    }
}
