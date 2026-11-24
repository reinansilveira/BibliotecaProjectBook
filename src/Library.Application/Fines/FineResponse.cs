namespace Library.Application.Fines;

public sealed record FineResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid LoanId,
    string BookTitle,
    int DaysOverdue,
    decimal DailyRate,
    decimal Amount,
    string Currency,
    string Status,
    DateTime IssuedAt,
    DateTime? ResolvedAt,
    string? PaymentReference);
