using FluentValidation;
using FluentValidation.Results;
using Library.Application.Common.Csv;

namespace Library.Application.Reports;

public sealed class ReportService : IReportService
{
    private const int MinimumYear = 2000;
    private const int MaximumYear = 2100;

    private readonly IReportRepository _reports;
    private readonly IValidator<MostBorrowedBooksRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public ReportService(IReportRepository reports, IValidator<MostBorrowedBooksRequest> validator, TimeProvider timeProvider)
    {
        _reports = reports;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(
        MostBorrowedBooksRequest request,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        return await _reports.GetMostBorrowedBooksAsync(StartOf(request.From), EndOf(request.To), request.Top, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default)
    {
        if (year is < MinimumYear or > MaximumYear)
        {
            throw new ValidationException([new ValidationFailure("Year", $"Year must be between {MinimumYear} and {MaximumYear}.")]);
        }

        return await _reports.GetMonthlyOverdueAsync(year, Now, cancellationToken);
    }

    public Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        _reports.GetDashboardAsync(Now, cancellationToken);

    public Task<ReadingStatsResponse> GetReadingStatsAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _reports.GetReadingStatsAsync(memberId, Now, cancellationToken);

    public async Task<CsvFile> ExportMostBorrowedBooksAsync(MostBorrowedBooksRequest request, CancellationToken cancellationToken = default)
    {
        var rows = await GetMostBorrowedBooksAsync(request, cancellationToken);

        return new CsvFile("most-borrowed-books.csv", CsvWriter.Write(
            rows,
            ("Rank", row => row.Rank),
            ("Title", row => row.Title),
            ("Author", row => row.Author),
            ("Loans", row => row.TotalLoans)));
    }

    public async Task<CsvFile> ExportMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default)
    {
        var rows = await GetMonthlyOverdueAsync(year, cancellationToken);

        return new CsvFile($"overdue-by-month-{year}.csv", CsvWriter.Write(
            rows,
            ("Month", row => row.Month),
            ("Loans", row => row.TotalLoans),
            ("Overdue", row => row.OverdueLoans),
            ("Overdue %", row => row.OverduePercentage),
            ("Average days overdue", row => row.AverageDaysOverdue)));
    }

    public async Task<CsvFile> ExportLoansAsync(LoanExportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.From.HasValue && request.To.HasValue && request.To < request.From)
        {
            throw new ValidationException([new ValidationFailure("To", "The end date must be on or after the start date.")]);
        }

        var rows = await _reports.GetLoansAsync(StartOf(request.From), EndOf(request.To), cancellationToken);

        return new CsvFile("loans.csv", CsvWriter.Write(
            rows,
            ("Loan", row => row.LoanId),
            ("Book", row => row.BookTitle),
            ("Author", row => row.BookAuthor),
            ("Member", row => row.MemberName),
            ("Email", row => row.MemberEmail),
            ("Format", row => row.Format),
            ("Loan date", row => row.LoanDate),
            ("Due date", row => row.DueDate),
            ("Returned at", row => row.ReturnedAt),
            ("Renewals", row => row.RenewalCount)));
    }

    private static DateTime? StartOf(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static DateTime? EndOf(DateOnly? date) => date?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
