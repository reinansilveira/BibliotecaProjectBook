namespace Library.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(MostBorrowedBooksRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default);
}
