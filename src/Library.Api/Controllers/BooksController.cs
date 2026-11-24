using Library.Api.Authorization;
using Library.Application.Books;
using Library.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    /// <summary>Lists the catalog with pagination and an optional search by title or author. Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<BookResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BookResponse>>> List([FromQuery] SearchPageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.ListAsync(request, cancellationToken));
    }

    /// <summary>Gets a book by id. Public.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Adds a book to the catalog. Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookResponse>> Create(BookRequest request, CancellationToken cancellationToken)
    {
        var book = await _bookService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    /// <summary>Updates a book. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> Update(Guid id, BookRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Removes a book from the catalog (soft delete). Books on loan cannot be removed. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _bookService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
