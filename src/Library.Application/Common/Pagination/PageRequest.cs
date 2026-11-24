namespace Library.Application.Common.Pagination;

public class PageRequest
{
    public const int MaxPageSize = 50;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
