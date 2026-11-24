using Library.Domain.Entities;

namespace Library.Application.Loans;

internal static class LoanMappings
{
    public static LoanResponse ToResponse(this Loan loan, DateTime now) =>
        new(loan.Id, loan.BookId, loan.MemberId, loan.LoanDate, loan.DueDate, loan.ReturnedAt, GetStatus(loan, now));

    private static string GetStatus(Loan loan, DateTime now)
    {
        if (loan.IsReturned)
        {
            return LoanStatus.Returned;
        }

        if (loan.IsOverdue(now))
        {
            return LoanStatus.Overdue;
        }

        return LoanStatus.Active;
    }
}
