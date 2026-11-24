using Library.Application.Common.Csv;

namespace Library.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(MostBorrowedBooksRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default);

    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<ReadingStatsResponse> GetReadingStatsAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<CsvFile> ExportMostBorrowedBooksAsync(MostBorrowedBooksRequest request, CancellationToken cancellationToken = default);

    Task<CsvFile> ExportMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default);

    Task<CsvFile> ExportLoansAsync(LoanExportRequest request, CancellationToken cancellationToken = default);
}
