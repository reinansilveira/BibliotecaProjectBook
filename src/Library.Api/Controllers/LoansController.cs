using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Contracts;
using Library.Api.Versioning;
using Library.Application.Loans;
using Library.Application.Loans.Reading;
using Library.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/loans")]
[Produces("application/json")]
[Authorize(Policy = Policies.MemberOrAdmin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly IEbookReadingService _readingService;
    private readonly IAuthorizationService _authorizationService;

    public LoansController(ILoanService loanService, IEbookReadingService readingService, IAuthorizationService authorizationService)
    {
        _loanService = loanService;
        _readingService = readingService;
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
    /// Borrows a printed copy for 7 days or an e-book for 14 days. Members borrow for themselves; admins can borrow on behalf of any member.
    /// The member needs no overdue loans, no unpaid fines and fewer than 3 active loans, and a copy or license must be free
    /// (or held for the member by a ready reservation).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        if (!await this.CanAccessMemberAsync(_authorizationService, request.MemberId))
        {
            return Forbid();
        }

        var loan = await _loanService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = loan.Id, version = ApiRoutes.Version1 }, loan);
    }

    /// <summary>Registers a return. Printed books are returned at the desk (admin); e-books can also be returned early by the reader. Late printed returns issue a fine.</summary>
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Return(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(id, cancellationToken);
        var isOwnEbook = loan.Format == nameof(BookFormat.Ebook) && User.GetMemberId() == loan.MemberId;

        if (!User.IsAdmin() && !isOwnEbook)
        {
            return Forbid();
        }

        return Ok(await _loanService.ReturnAsync(id, cancellationToken));
    }

    /// <summary>Renews the loan once for another period, when it is not overdue and nobody is waiting for the book.</summary>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Renew(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(id, cancellationToken);

        if (!await this.CanAccessMemberAsync(_authorizationService, loan.MemberId))
        {
            return Forbid();
        }

        return Ok(await _loanService.RenewAsync(id, cancellationToken));
    }

    /// <summary>Creates a signed link to read the e-book, valid for 10 minutes. Only the reader of an active e-book loan.</summary>
    [HttpPost("{id:guid}/reading-link")]
    [ProducesResponseType<EbookLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EbookLinkResponse>> ReadingLink(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(id, cancellationToken);

        if (User.GetMemberId() != loan.MemberId)
        {
            return Forbid();
        }

        var access = await _readingService.CreateAccessAsync(id, cancellationToken);
        var url = Url.Action(
            nameof(EbooksController.Content),
            "Ebooks",
            new { loanId = access.LoanId, expires = access.Expires, signature = access.Signature, version = ApiRoutes.Version1 })!;

        return Ok(new EbookLinkResponse(url, access.ExpiresAt, access.FileFormat));
    }

    /// <summary>Saves where the reader stopped (an EPUB CFI or a page) and the percentage read.</summary>
    [HttpPut("{id:guid}/progress")]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoanResponse>> Progress(Guid id, ReadingProgressRequest request, CancellationToken cancellationToken)
    {
        var loan = await _loanService.GetByIdAsync(id, cancellationToken);

        if (User.GetMemberId() != loan.MemberId)
        {
            return Forbid();
        }

        return Ok(await _readingService.UpdateProgressAsync(id, request, cancellationToken));
    }
}
