namespace Library.Application.Reports;

public sealed record LoanExportRow
{
    public Guid LoanId { get; init; }

    public string BookTitle { get; init; } = string.Empty;

    public string BookAuthor { get; init; } = string.Empty;

    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public DateTime LoanDate { get; init; }

    public DateTime DueDate { get; init; }

    public DateTime? ReturnedAt { get; init; }

    public int RenewalCount { get; init; }
}
