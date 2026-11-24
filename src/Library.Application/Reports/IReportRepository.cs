namespace Library.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(DateTime? from, DateTime? toExclusive, int top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, DateTime now, CancellationToken cancellationToken = default);
}
