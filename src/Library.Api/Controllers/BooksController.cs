using Asp.Versioning;
using Library.Api.Authorization;
using Library.Api.Contracts;
using Library.Api.Versioning;
using Library.Application.Books;
using Library.Application.Books.Import;
using Library.Application.Books.Inventory;
using Library.Application.Common.Pagination;
using Library.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route(ApiRoutes.Prefix + "/books")]
[Produces("application/json")]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookInventoryService _inventoryService;
    private readonly IBookImportService _importService;
    private readonly IReservationService _reservationService;

    public BooksController(
        IBookService bookService,
        IBookInventoryService inventoryService,
        IBookImportService importService,
        IReservationService reservationService)
    {
        _bookService = bookService;
        _inventoryService = inventoryService;
        _importService = importService;
        _reservationService = reservationService;
    }

    /// <summary>Searches the catalog. Full-text search when SQL Server has it, filters by genre, author, year, format and availability, and sorting by title, newest, rating, popularity or year. Public and cached.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<BookResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BookResponse>>> Search([FromQuery] BookSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.SearchAsync(request, cancellationToken));
    }

    /// <summary>Title and author suggestions while the user types (at least 2 characters). Public.</summary>
    [HttpGet("suggestions")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<BookSuggestion>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookSuggestion>>> Suggestions([FromQuery] string query, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.SuggestAsync(query ?? string.Empty, cancellationToken));
    }

    /// <summary>Genres in the catalog with the number of books in each. Public.</summary>
    [HttpGet("genres")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<GenreResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GenreResponse>>> Genres(CancellationToken cancellationToken)
    {
        return Ok(await _bookService.ListGenresAsync(cancellationToken));
    }

    /// <summary>Gets a book with its availability (printed copies and e-book licenses), waiting list size and rating. Public.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>"Members who read this also read": books borrowed by the readers of this book, most shared first. Public.</summary>
    [HttpGet("{id:guid}/also-read")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> AlsoRead(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _bookService.ListAlsoReadAsync(id, cancellationToken));
    }

    /// <summary>Adds a book with a number of printed copies (0 for e-book only). Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = await _bookService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = book.Id, version = ApiRoutes.Version1 }, book);
    }

    /// <summary>Finds title, author, description, year and cover of an ISBN on Open Library, without saving. Admin only.</summary>
    [HttpGet("lookup")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookMetadata>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookMetadata>> Lookup([FromQuery] string isbn, CancellationToken cancellationToken)
    {
        return Ok(await _importService.LookupAsync(isbn ?? string.Empty, cancellationToken));
    }

    /// <summary>Adds a book from its ISBN, filled in by Open Library. Admin only.</summary>
    [HttpPost("import")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Import(ImportBookRequest request, CancellationToken cancellationToken)
    {
        var book = await _importService.ImportAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = book.Id, version = ApiRoutes.Version1 }, book);
    }

    /// <summary>Updates a book. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
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

    /// <summary>Lists the printed copies of a book with their barcode and status. Admin only.</summary>
    [HttpGet("{id:guid}/copies")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<IReadOnlyList<BookCopyResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> ListCopies(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.ListCopiesAsync(id, cancellationToken));
    }

    /// <summary>Adds printed copies. The first members in the waiting list are notified right away. Admin only.</summary>
    [HttpPost("{id:guid}/copies")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<IReadOnlyList<BookCopyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> AddCopies(Guid id, AddCopiesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.AddCopiesAsync(id, request, cancellationToken));
    }

    /// <summary>Withdraws a damaged or lost copy that is not on loan. Admin only.</summary>
    [HttpDelete("{id:guid}/copies/{copyId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> WithdrawCopy(Guid id, Guid copyId, CancellationToken cancellationToken)
    {
        await _inventoryService.WithdrawCopyAsync(id, copyId, cancellationToken);

        return NoContent();
    }

    /// <summary>Uploads the e-book (EPUB or PDF, up to 25 MB) and sets how many members can read it at the same time. Admin only.</summary>
    [HttpPost("{id:guid}/ebook")]
    [Authorize(Policy = Policies.AdminOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(EbookFiles.MaxSizeInBytes + (1024 * 1024))]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookResponse>> UploadEbook(Guid id, [FromForm] EbookUploadForm form, CancellationToken cancellationToken)
    {
        await using var stream = form.File.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return Ok(await _inventoryService.UploadEbookAsync(id, new EbookUpload(form.File.FileName, buffer.ToArray(), form.Licenses), cancellationToken));
    }

    /// <summary>Downloads the public-domain EPUB of a Project Gutenberg book and attaches it as the e-book. Admin only.</summary>
    [HttpPost("{id:guid}/ebook/gutenberg")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookResponse>> ImportGutenbergEbook(Guid id, GutenbergImportRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.ImportGutenbergEbookAsync(id, request, cancellationToken));
    }

    /// <summary>Changes how many members can read the e-book at the same time. Admin only.</summary>
    [HttpPut("{id:guid}/ebook/licenses")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookResponse>> ChangeLicenses(Guid id, ChangeLicensesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _inventoryService.ChangeEbookLicensesAsync(id, request, cancellationToken));
    }

    /// <summary>The waiting list of a book: holds ready for pick-up first, then members in order. Admin only.</summary>
    [HttpGet("{id:guid}/reservations")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<IReadOnlyList<ReservationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> Queue(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _reservationService.ListQueueAsync(id, cancellationToken));
    }
}
