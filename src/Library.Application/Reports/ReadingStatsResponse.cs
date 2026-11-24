namespace Library.Application.Reports;

public sealed record ReadingStatsResponse
{
    public int TotalLoans { get; init; }

    public int BooksRead { get; init; }

    public int EbooksRead { get; init; }

    public int ActiveLoans { get; init; }

    public int BooksThisYear { get; init; }

    public int ReviewsWritten { get; init; }

    public decimal AverageRatingGiven { get; init; }

    public string? FavoriteGenre { get; init; }

    public IReadOnlyList<GenreCount> TopGenres { get; init; } = [];

    public IReadOnlyList<MonthCount> LastTwelveMonths { get; init; } = [];
}

public sealed record GenreCount
{
    public string Genre { get; init; } = string.Empty;

    public int Loans { get; init; }
}

public sealed record MonthCount
{
    public string Month { get; init; } = string.Empty;

    public int Loans { get; init; }
}
