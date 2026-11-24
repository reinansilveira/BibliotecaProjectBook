using Library.Application.Common.Pagination;
using Library.Domain.Enums;

namespace Library.Application.Books;

public sealed class BookSearchRequest : SearchPageRequest
{
    public string? Genre { get; init; }

    public string? Author { get; init; }

    public int? YearFrom { get; init; }

    public int? YearTo { get; init; }

    public BookFormat? Format { get; init; }

    public bool AvailableOnly { get; init; }

    public BookSort Sort { get; init; } = BookSort.Title;

    public string CacheKey =>
        $"books:{Page}:{PageSize}:{Search}:{Genre}:{Author}:{YearFrom}:{YearTo}:{Format}:{AvailableOnly}:{Sort}".ToLowerInvariant();
}
