using Library.Application.Common.Pagination;

namespace Library.Application.Books;

public interface IBookService
{
    Task<BookResponse> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default);

    Task<BookResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<BookResponse>> SearchAsync(BookSearchRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GenreResponse>> ListGenresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookResponse>> ListAlsoReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BookResponse> UpdateAsync(Guid id, BookRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
