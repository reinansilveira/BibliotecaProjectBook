namespace Library.Application.Reports;

public sealed record MonthlyOverdueResponse
{
    public int Month { get; init; }

    public int TotalLoans { get; init; }

    public int OverdueLoans { get; init; }

    public decimal OverduePercentage { get; init; }

    public decimal AverageDaysOverdue { get; init; }
}
