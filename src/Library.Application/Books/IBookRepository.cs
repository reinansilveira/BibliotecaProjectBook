using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Books;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Book>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);
}
