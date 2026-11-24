namespace Library.Infrastructure.Search;

public sealed class SearchSettings
{
    public const string SectionName = "Search";

    public bool UseFullText { get; set; } = true;
}
