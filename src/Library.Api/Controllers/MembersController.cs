using Library.Api.Authorization;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/members")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILoanService _loanService;
    private readonly IAuthorizationService _authorizationService;

    public MembersController(IMemberService memberService, ILoanService loanService, IAuthorizationService authorizationService)
    {
        _memberService = memberService;
        _loanService = loanService;
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

    /// <summary>Lists the loan history of a member, paginated (the member themselves or an admin).</summary>
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

    /// <summary>Updates the member profile (the member themselves or an admin).</summary>
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
