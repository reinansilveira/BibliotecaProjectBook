using Library.Application.Books;
using Library.Application.Common.Pagination;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Books.FirstOrDefaultAsync(book => book.Id == id, cancellationToken);

    public Task<PagedResult<Book>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Books.AsNoTracking().Where(book => !book.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(book => book.Title.Contains(search) || book.Author.Contains(search));
        }

        return query
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default) =>
        await _context.Books.AddAsync(book, cancellationToken);
}
