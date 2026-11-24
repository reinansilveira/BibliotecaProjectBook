namespace Library.Application.Reports;

public interface IReportRepository
{
    Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(DateTime? from, DateTime? toExclusive, int top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetAlsoReadBookIdsAsync(Guid bookId, int top, CancellationToken cancellationToken = default);

    Task<DashboardResponse> GetDashboardAsync(DateTime now, CancellationToken cancellationToken = default);

    Task<ReadingStatsResponse> GetReadingStatsAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoanExportRow>> GetLoansAsync(DateTime? from, DateTime? toExclusive, CancellationToken cancellationToken = default);
}
