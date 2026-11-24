namespace Library.Application.Reports;

public sealed class MostBorrowedBooksRequest
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public int Top { get; init; } = 10;
}
