namespace Library.Application.Reports;

public sealed record MostBorrowedBookResponse
{
    public int Rank { get; init; }

    public Guid BookId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public int TotalLoans { get; init; }
}
