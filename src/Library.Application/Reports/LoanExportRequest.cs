namespace Library.Application.Reports;

public sealed class LoanExportRequest
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}
