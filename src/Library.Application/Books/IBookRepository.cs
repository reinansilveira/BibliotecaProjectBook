using Library.Application.Common.Pagination;
using Library.Domain.Entities;

namespace Library.Application.Books;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BookResponse?> GetViewAsync(Guid id, DateTime now, CancellationToken cancellationToken = default);

    Task<PagedResult<BookResponse>> SearchAsync(BookSearchRequest request, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookResponse>> ListViewsAsync(IReadOnlyCollection<Guid> ids, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookSuggestion>> SuggestAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GenreResponse>> ListGenresAsync(CancellationToken cancellationToken = default);

    Task<bool> IsIsbnInUseAsync(string isbn, Guid? excludingBookId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);
}
