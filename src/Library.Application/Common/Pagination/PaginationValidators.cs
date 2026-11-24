namespace Library.Application.Common.Pagination;

internal static class PaginationValidators
{
    public static readonly PageRequestValidator Page = new();

    public static readonly SearchPageRequestValidator Search = new();
}
