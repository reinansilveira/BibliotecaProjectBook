using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Loans;

public sealed record LoanView(Loan Loan, string BookTitle, string BookAuthor, string? CoverUrl, string MemberName, string MemberEmail)
{
    public LoanResponse ToResponse(DateTime now) => new(
        Loan.Id,
        Loan.BookId,
        BookTitle,
        BookAuthor,
        CoverUrl,
        Loan.MemberId,
        Loan.Format.ToString(),
        Loan.LoanDate,
        Loan.DueDate,
        Loan.ReturnedAt,
        GetStatus(Loan, now),
        Math.Max(0, Loan.MaxRenewals - Loan.RenewalCount),
        Loan.ReadingProgress,
        Loan.ReadingLocation);

    private static string GetStatus(Loan loan, DateTime now)
    {
        if (loan.IsReturned)
        {
            return loan.Format == BookFormat.Ebook && loan.ReturnedAt >= loan.DueDate ? LoanStatus.Expired : LoanStatus.Returned;
        }

        if (loan.IsExpired(now))
        {
            return LoanStatus.Expired;
        }

        if (loan.IsOverdue(now))
        {
            return LoanStatus.Overdue;
        }

        return LoanStatus.Active;
    }
}
