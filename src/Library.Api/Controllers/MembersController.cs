using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Books;
using Library.Application.Common.Pagination;
using Library.Application.Fines;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reports;
using Library.Application.Reservations;
using Library.Application.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/members")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILoanService _loanService;
    private readonly IReservationService _reservationService;
    private readonly IFineService _fineService;
    private readonly IMemberStandingService _standingService;
    private readonly IWishlistService _wishlistService;
    private readonly IReportService _reportService;
    private readonly IAuthorizationService _authorizationService;

    public MembersController(
        IMemberService memberService,
        ILoanService loanService,
        IReservationService reservationService,
        IFineService fineService,
        IMemberStandingService standingService,
        IWishlistService wishlistService,
        IReportService reportService,
        IAuthorizationService authorizationService)
    {
        _memberService = memberService;
        _loanService = loanService;
        _reservationService = reservationService;
        _fineService = fineService;
        _standingService = standingService;
        _wishlistService = wishlistService;
        _reportService = reportService;
        _authorizationService = authorizationService;
    }

    /// <summary>Lists members with pagination and an optional search by name or email. Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<PagedResult<MemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MemberResponse>>> List([FromQuery] SearchPageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _memberService.ListAsync(request, cancellationToken));
    }

    /// <summary>Gets a member by id. Members can only see their own profile; admins can see everyone.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _memberService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Loan history of a member with the book of each loan: active first, then newest (the member or an admin).</summary>
    [HttpGet("{id:guid}/loans")]
    [ProducesResponseType<PagedResult<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LoanResponse>>> ListLoans(Guid id, [FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _loanService.ListByMemberAsync(id, request, cancellationToken));
    }

    /// <summary>Reservations of a member with the position in each waiting list (the member or an admin).</summary>
    [HttpGet("{id:guid}/reservations")]
    [ProducesResponseType<PagedResult<ReservationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> ListReservations(Guid id, [FromQuery] ReservationListRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _reservationService.ListByMemberAsync(id, request, cancellationToken));
    }

    /// <summary>Fines of a member, unpaid first (the member or an admin).</summary>
    [HttpGet("{id:guid}/fines")]
    [ProducesResponseType<PagedResult<FineResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FineResponse>>> ListFines(Guid id, [FromQuery] FineListRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _fineService.ListAsync(id, request, cancellationToken));
    }

    /// <summary>Whether the member can borrow now, and why not (overdue loans, unpaid fines or loan limit).</summary>
    [HttpGet("{id:guid}/standing")]
    [ProducesResponseType<StandingResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StandingResponse>> Standing(Guid id, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _standingService.GetAsync(id, cancellationToken));
    }

    /// <summary>Reading statistics: books read, favorite genre, reviews and loans in the last 12 months.</summary>
    [HttpGet("{id:guid}/reading-stats")]
    [ProducesResponseType<ReadingStatsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReadingStatsResponse>> ReadingStats(Guid id, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _reportService.GetReadingStatsAsync(id, cancellationToken));
    }

    /// <summary>Books on the wishlist, newest first.</summary>
    [HttpGet("{id:guid}/wishlist")]
    [ProducesResponseType<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> Wishlist(Guid id, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _wishlistService.ListAsync(id, cancellationToken));
    }

    /// <summary>Adds a book to the wishlist. Adding it twice has no effect.</summary>
    [HttpPut("{id:guid}/wishlist/{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToWishlist(Guid id, Guid bookId, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        await _wishlistService.AddAsync(id, bookId, cancellationToken);

        return NoContent();
    }

    /// <summary>Removes a book from the wishlist.</summary>
    [HttpDelete("{id:guid}/wishlist/{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromWishlist(Guid id, Guid bookId, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        await _wishlistService.RemoveAsync(id, bookId, cancellationToken);

        return NoContent();
    }

    /// <summary>Updates the member profile (the member themselves or an admin). Changing the email asks for a new confirmation.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> Update(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, id))
        {
            return Forbid();
        }

        return Ok(await _memberService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Removes a member (soft delete). Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _memberService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
