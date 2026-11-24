using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Versioning;
using Library.Application.Common.Pagination;
using Library.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/books/{bookId:guid}/reviews")]
[Produces("application/json")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Reviews of a book, newest first. Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ReviewResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> List(Guid bookId, [FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reviewService.ListByBookAsync(bookId, request, cancellationToken));
    }

    /// <summary>The review of the signed-in member, or 204 when there is none.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = Policies.MemberOrAdmin)]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ReviewResponse>> GetMine(Guid bookId, CancellationToken cancellationToken)
    {
        var review = await _reviewService.GetMineAsync(bookId, User.GetMemberId()!.Value, cancellationToken);

        if (review is null)
        {
            return NoContent();
        }

        return Ok(review);
    }

    /// <summary>Creates or updates the review of the signed-in member (1 to 5 stars). Only members who borrowed the book can review it.</summary>
    [HttpPut("mine")]
    [Authorize(Policy = Policies.MemberOrAdmin)]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReviewResponse>> Save(Guid bookId, ReviewRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reviewService.SaveAsync(bookId, User.GetMemberId()!.Value, request, cancellationToken));
    }

    /// <summary>Deletes the review of the signed-in member.</summary>
    [HttpDelete("mine")]
    [Authorize(Policy = Policies.MemberOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid bookId, CancellationToken cancellationToken)
    {
        await _reviewService.DeleteAsync(bookId, User.GetMemberId()!.Value, cancellationToken);

        return NoContent();
    }
}
