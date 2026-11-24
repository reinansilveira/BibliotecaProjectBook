using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Books.Inventory;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Application.Reports;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Books;

public sealed class BookService : IBookService
{
    public const int SuggestionLimit = 8;
    public const int AlsoReadLimit = 6;
    private const int MinimumSuggestionLength = 2;

    private readonly IBookRepository _books;
    private readonly IBookCopyRepository _copies;
    private readonly ILoanRepository _loans;
    private readonly IReportRepository _reports;
    private readonly ICatalogCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateBookRequest> _createValidator;
    private readonly IValidator<BookRequest> _updateValidator;
    private readonly IValidator<BookSearchRequest> _searchValidator;
    private readonly TimeProvider _timeProvider;

    public BookService(
        IBookRepository books,
        IBookCopyRepository copies,
        ILoanRepository loans,
        IReportRepository reports,
        ICatalogCache cache,
        IUnitOfWork unitOfWork,
        IValidator<CreateBookRequest> createValidator,
        IValidator<BookRequest> updateValidator,
        IValidator<BookSearchRequest> searchValidator,
        TimeProvider timeProvider)
    {
        _books = books;
        _copies = copies;
        _loans = loans;
        _reports = reports;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<BookResponse> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var details = request.ToDetails();
        await EnsureIsbnIsAvailableAsync(details.Isbn, null, cancellationToken);

        var book = new Book(details, Now);
        await _books.AddAsync(book, cancellationToken);

        foreach (var copy in BookCopyFactory.Create(book.Id, request.Copies, Now))
        {
            await _copies.AddAsync(copy, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.InvalidateAsync(cancellationToken);

        return await GetViewAsync(book.Id, cancellationToken);
    }

    public Task<BookResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync($"book:{id}", token => GetViewAsync(id, token), cancellationToken);

    public async Task<PagedResult<BookResponse>> SearchAsync(BookSearchRequest request, CancellationToken cancellationToken = default)
    {
        await _searchValidator.ValidateAndThrowAsync(request, cancellationToken);

        return await _cache.GetOrCreateAsync(request.CacheKey, token => _books.SearchAsync(request, Now, token), cancellationToken);
    }

    public async Task<IReadOnlyList<BookSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
    {
        var term = query.Trim();

        if (term.Length < MinimumSuggestionLength)
        {
            return [];
        }

        return await _cache.GetOrCreateAsync(
            $"suggestions:{term.ToLowerInvariant()}",
            token => _books.SuggestAsync(term, SuggestionLimit, token),
            cancellationToken);
    }

    public Task<IReadOnlyList<GenreResponse>> ListGenresAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync("genres", _books.ListGenresAsync, cancellationToken);

    public async Task<IReadOnlyList<BookResponse>> ListAlsoReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GetActiveBookAsync(id, cancellationToken);

        return await _cache.GetOrCreateAsync($"also-read:{id}", async token =>
        {
            var ids = await _reports.GetAlsoReadBookIdsAsync(id, AlsoReadLimit, token);
            var views = await _books.ListViewsAsync(ids, Now, token);

            return (IReadOnlyList<BookResponse>)ids.Join(views, bookId => bookId, view => view.Id, (_, view) => view).ToList();
        }, cancellationToken);
    }

    public async Task<BookResponse> UpdateAsync(Guid id, BookRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var book = await GetActiveBookAsync(id, cancellationToken);
        var details = request.ToDetails();
        await EnsureIsbnIsAvailableAsync(details.Isbn, id, cancellationToken);

        book.Update(details);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.InvalidateAsync(cancellationToken);

        return await GetViewAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await GetActiveBookAsync(id, cancellationToken);

        if (await _loans.HasActiveLoanForBookAsync(id, Now, cancellationToken))
        {
            throw new DomainException("A book that is on loan cannot be deleted.");
        }

        book.Delete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.InvalidateAsync(cancellationToken);
    }

    private async Task<BookResponse> GetViewAsync(Guid id, CancellationToken cancellationToken) =>
        await _books.GetViewAsync(id, Now, cancellationToken) ?? throw new NotFoundException("Book not found.");

    private async Task<Book> GetActiveBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(id, cancellationToken);

        if (book is null || book.IsDeleted)
        {
            throw new NotFoundException("Book not found.");
        }

        return book;
    }

    private async Task EnsureIsbnIsAvailableAsync(string? isbn, Guid? bookId, CancellationToken cancellationToken)
    {
        if (isbn is null || !await _books.IsIsbnInUseAsync(isbn, bookId, cancellationToken))
        {
            return;
        }

        throw new ConflictException("A book with this ISBN already exists.");
    }
}
