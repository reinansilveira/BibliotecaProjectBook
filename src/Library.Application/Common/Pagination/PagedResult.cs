namespace Library.Application.Common.Pagination;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;

    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalItems);
}
