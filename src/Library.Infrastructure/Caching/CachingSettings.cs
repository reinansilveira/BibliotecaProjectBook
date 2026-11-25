namespace Library.Infrastructure.Caching;

public sealed class CachingSettings
{
    public const string SectionName = "Caching";

    public bool Enabled { get; set; } = true;

    public int CatalogSeconds { get; set; } = 120;
}
