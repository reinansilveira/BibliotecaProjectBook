using Library.Domain.Entities;

namespace Library.Application.Fines;

public sealed record FineView(Fine Fine, string MemberName, string BookTitle)
{
    public FineResponse ToResponse(string currency) => new(
        Fine.Id,
        Fine.MemberId,
        MemberName,
        Fine.LoanId,
        BookTitle,
        Fine.DaysOverdue,
        Fine.DailyRate,
        Fine.Amount,
        currency,
        Fine.Status.ToString(),
        Fine.IssuedAt,
        Fine.ResolvedAt,
        Fine.PaymentReference);
}
