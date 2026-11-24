using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class LoanTests
{
    private static readonly DateTime LoanDate = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_loan_is_due_in_seven_days()
    {
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), LoanDate);

        Assert.Equal(LoanDate.AddDays(7), loan.DueDate);
        Assert.False(loan.IsReturned);
    }

    [Fact]
    public void Loan_is_not_overdue_within_the_period()
    {
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), LoanDate);

        Assert.False(loan.IsOverdue(LoanDate.AddDays(7)));
    }

    [Fact]
    public void Loan_is_overdue_after_the_due_date()
    {
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), LoanDate);

        Assert.True(loan.IsOverdue(LoanDate.AddDays(8)));
    }

    [Fact]
    public void Returned_loan_is_never_overdue()
    {
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), LoanDate);

        loan.Return(LoanDate.AddDays(3));

        Assert.True(loan.IsReturned);
        Assert.False(loan.IsOverdue(LoanDate.AddDays(30)));
    }

    [Fact]
    public void Loan_cannot_be_returned_twice()
    {
        var loan = new Loan(Guid.NewGuid(), Guid.NewGuid(), LoanDate);
        loan.Return(LoanDate.AddDays(1));

        var exception = Assert.Throws<DomainException>(() => loan.Return(LoanDate.AddDays(2)));

        Assert.Equal("This loan has already been returned.", exception.Message);
    }
}
