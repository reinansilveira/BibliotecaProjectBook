using Library.Application.Common.Pagination;

namespace Library.Application.Books;

public interface IBookService
{
    Task<BookResponse> CreateAsync(BookRequest request, CancellationToken cancellationToken = default);

    Task<BookResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<BookResponse>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default);

    Task<BookResponse> UpdateAsync(Guid id, BookRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
