namespace Library.Application.Common.Pagination;

public class SearchPageRequest : PageRequest
{
    public string? Search { get; init; }
}
