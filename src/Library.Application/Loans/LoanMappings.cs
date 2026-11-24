using Library.Domain.Entities;

namespace Library.Application.Loans;

internal static class LoanMappings
{
    public static string DescribeDueDate(this Loan loan) => loan.DueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
