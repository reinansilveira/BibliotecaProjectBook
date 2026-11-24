using Library.Api.Authorization;
using Library.Application.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Produces("application/json")]
[Authorize(Policy = Policies.MemberOrAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly IAuthorizationService _authorizationService;

    public LoansController(ILoanService loanService, IAuthorizationService authorizationService)
    {
        _loanService = loanService;
        _authorizationService = authorizationService;
    }

    /// <summary>Gets a loan by id (the borrower or an admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, loan.MemberId))
        {
            return Forbid();
        }

        return Ok(loan);
    }

    /// <summary>
    /// Borrows a book for 7 days. Members can only borrow for themselves; admins can borrow on behalf of any member.
    /// The member cannot be blacklisted or have another active loan, and the book must be available.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, request.MemberId))
        {
            return Forbid();
        }

        var loan = await _loanService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    /// <summary>Registers the return of the book. Admin only (done at the library desk).</summary>
    [HttpPost("{id:guid}/return")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Return(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _loanService.ReturnAsync(id, cancellationToken));
    }
}
