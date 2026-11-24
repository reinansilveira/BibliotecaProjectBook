using FluentValidation;
using FluentValidation.Results;

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

    public async Task<IReadOnlyList<MostBorrowedBookResponse>> GetMostBorrowedBooksAsync(
        MostBorrowedBooksRequest request,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        DateTime? from = request.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? toExclusive = request.To?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _reports.GetMostBorrowedBooksAsync(from, toExclusive, request.Top, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyOverdueResponse>> GetMonthlyOverdueAsync(int year, CancellationToken cancellationToken = default)
    {
        if (year is < MinimumYear or > MaximumYear)
        {
            throw new ValidationException([new ValidationFailure("Year", $"Year must be between {MinimumYear} and {MaximumYear}.")]);
        }

        return await _reports.GetMonthlyOverdueAsync(year, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
