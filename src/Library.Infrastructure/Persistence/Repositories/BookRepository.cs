using Library.Application.Books;
using Library.Application.Common.Pagination;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;
    private readonly FullTextSearch _fullTextSearch;

    public BookRepository(LibraryDbContext context, FullTextSearch fullTextSearch)
    {
        _context = context;
        _fullTextSearch = fullTextSearch;
    }

    private IQueryable<Book> ActiveBooks => _context.Books.AsNoTracking().Where(book => !book.IsDeleted);

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Books.FirstOrDefaultAsync(book => book.Id == id, cancellationToken);

    public async Task<BookResponse?> GetViewAsync(Guid id, DateTime now, CancellationToken cancellationToken = default)
    {
        var view = await ActiveBooks.Where(book => book.Id == id).ToViews(_context, now).FirstOrDefaultAsync(cancellationToken);

        return view?.Normalize();
    }

    public async Task<PagedResult<BookResponse>> SearchAsync(BookSearchRequest request, DateTime now, CancellationToken cancellationToken = default)
    {
        var books = await ApplySearchAsync(ActiveBooks, request.Search, cancellationToken);
        books = ApplyFilters(books, request, now);

        var page = await Sort(books, request.Sort).ToPagedResultAsync(query => query.ToViews(_context, now), request, cancellationToken);

        return page.Map(view => view.Normalize());
    }

    public async Task<IReadOnlyList<BookResponse>> ListViewsAsync(IReadOnlyCollection<Guid> ids, DateTime now, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var views = await ActiveBooks.Where(book => ids.Contains(book.Id)).ToViews(_context, now).ToListAsync(cancellationToken);

        return views.Select(view => view.Normalize()).ToList();
    }

    public async Task<IReadOnlyList<BookSuggestion>> SuggestAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var wordStart = $" {query}";

        return await ActiveBooks
            .Where(book => book.Title.StartsWith(query) || book.Author.StartsWith(query) || book.Title.Contains(wordStart) || book.Author.Contains(wordStart))
            .OrderByDescending(book => book.Title.StartsWith(query))
            .ThenByDescending(book => book.RatingsCount)
            .ThenBy(book => book.Title)
            .Take(limit)
            .Select(book => new BookSuggestion(book.Id, book.Title, book.Author, book.CoverUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GenreResponse>> ListGenresAsync(CancellationToken cancellationToken = default) =>
        await ActiveBooks
            .GroupBy(book => book.Genre)
            .OrderBy(group => group.Key)
            .Select(group => new GenreResponse(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    public Task<bool> IsIsbnInUseAsync(string isbn, Guid? excludingBookId = null, CancellationToken cancellationToken = default) =>
        ActiveBooks.AnyAsync(book => book.Isbn == isbn && (excludingBookId == null || book.Id != excludingBookId), cancellationToken);

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default) =>
        await _context.Books.AddAsync(book, cancellationToken);

    private async Task<IQueryable<Book>> ApplySearchAsync(IQueryable<Book> books, string? search, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return books;
        }

        var term = search.Trim();
        var isbn = Isbn.Normalize(term);
        var condition = FullTextSearch.BuildPrefixCondition(term);

        if (condition is not null && await _fullTextSearch.IsAvailableAsync(_context, cancellationToken))
        {
            return books.Where(book =>
                EF.Functions.Contains(book.Title, condition)
                || EF.Functions.Contains(book.Author, condition)
                || EF.Functions.Contains(book.Description, condition)
                || book.Isbn == isbn);
        }

        return books.Where(book => book.Title.Contains(term) || book.Author.Contains(term) || book.Isbn == isbn);
    }

    private IQueryable<Book> ApplyFilters(IQueryable<Book> books, BookSearchRequest request, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            var genre = request.Genre.Trim();
            books = books.Where(book => book.Genre == genre);
        }

        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var author = request.Author.Trim();
            books = books.Where(book => book.Author.Contains(author));
        }

        if (request.YearFrom.HasValue)
        {
            books = books.Where(book => book.PublishedYear >= request.YearFrom);
        }

        if (request.YearTo.HasValue)
        {
            books = books.Where(book => book.PublishedYear <= request.YearTo);
        }

        if (request.Format == BookFormat.Ebook)
        {
            books = books.Where(book => book.EbookFileKey != null && book.EbookLicenses > 0);
        }

        if (request.Format == BookFormat.Print)
        {
            books = books.Where(book => _context.BookCopies.Any(copy => copy.BookId == book.Id && copy.WithdrawnAt == null));
        }

        return request.AvailableOnly ? books.WhereAvailable(_context, now) : books;
    }

    private IOrderedQueryable<Book> Sort(IQueryable<Book> books, BookSort sort) => sort switch
    {
        BookSort.Newest => books.OrderByDescending(book => book.CreatedAt).ThenBy(book => book.Id),
        BookSort.Rating => books.OrderByDescending(book => book.AverageRating).ThenByDescending(book => book.RatingsCount).ThenBy(book => book.Title).ThenBy(book => book.Id),
        BookSort.Popular => books.OrderByDescending(book => _context.Loans.Count(loan => loan.BookId == book.Id)).ThenBy(book => book.Title).ThenBy(book => book.Id),
        BookSort.Year => books.OrderByDescending(book => book.PublishedYear).ThenBy(book => book.Title).ThenBy(book => book.Id),
        _ => books.OrderBy(book => book.Title).ThenBy(book => book.Id)
    };
}
