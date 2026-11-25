using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class LoanTests
{
    private static readonly DateTime LoanDate = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Loan PrintLoan() => Loan.ForPrint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), LoanDate);

    private static Loan EbookLoan() => Loan.ForEbook(Guid.NewGuid(), Guid.NewGuid(), LoanDate);

    [Fact]
    public void Printed_loan_is_due_in_seven_days()
    {
        var loan = PrintLoan();

        Assert.Equal(LoanDate.AddDays(7), loan.DueDate);
        Assert.Equal(BookFormat.Print, loan.Format);
        Assert.NotNull(loan.CopyId);
    }

    [Fact]
    public void Ebook_loan_lasts_fourteen_days_and_has_no_copy()
    {
        var loan = EbookLoan();

        Assert.Equal(LoanDate.AddDays(14), loan.DueDate);
        Assert.Null(loan.CopyId);
    }

    [Fact]
    public void Printed_loan_is_overdue_after_the_due_date()
    {
        var loan = PrintLoan();

        Assert.False(loan.IsOverdue(LoanDate.AddDays(7)));
        Assert.True(loan.IsOverdue(LoanDate.AddDays(8)));
    }

    [Fact]
    public void Ebook_expires_instead_of_becoming_overdue()
    {
        var loan = EbookLoan();
        var later = LoanDate.AddDays(15);

        Assert.False(loan.IsOverdue(later));
        Assert.True(loan.IsExpired(later));
        Assert.False(loan.IsActive(later));
    }

    [Fact]
    public void Expiring_an_ebook_closes_it_on_the_due_date()
    {
        var loan = EbookLoan();

        loan.Expire();

        Assert.Equal(loan.DueDate, loan.ReturnedAt);
    }

    [Fact]
    public void Expire_ignores_printed_loans()
    {
        var loan = PrintLoan();

        loan.Expire();

        Assert.False(loan.IsReturned);
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(7.5, 1)]
    [InlineData(10, 3)]
    public void Days_overdue_are_rounded_up(double daysAfterLoan, int expected)
    {
        var loan = PrintLoan();

        Assert.Equal(expected, loan.DaysOverdue(LoanDate.AddDays(daysAfterLoan)));
    }

    [Fact]
    public void Ebooks_never_count_days_overdue()
    {
        Assert.Equal(0, EbookLoan().DaysOverdue(LoanDate.AddDays(30)));
    }

    [Fact]
    public void Returned_loan_is_never_overdue()
    {
        var loan = PrintLoan();

        loan.Return(LoanDate.AddDays(3));

        Assert.False(loan.IsOverdue(LoanDate.AddDays(30)));
        Assert.Equal(0, loan.DaysOverdue(LoanDate.AddDays(30)));
    }

    [Fact]
    public void Loan_cannot_be_returned_twice()
    {
        var loan = PrintLoan();
        loan.Return(LoanDate.AddDays(1));

        var exception = Assert.Throws<DomainException>(() => loan.Return(LoanDate.AddDays(2)));

        Assert.Equal("This loan has already been returned.", exception.Message);
    }

    [Fact]
    public void Renewal_adds_another_period_once()
    {
        var loan = PrintLoan();

        loan.Renew(LoanDate.AddDays(5));

        Assert.Equal(LoanDate.AddDays(14), loan.DueDate);
        Assert.Equal(1, loan.RenewalCount);
        Assert.Throws<DomainException>(() => loan.Renew(LoanDate.AddDays(6)));
    }

    [Fact]
    public void Overdue_loan_cannot_be_renewed()
    {
        var loan = PrintLoan();

        var exception = Assert.Throws<DomainException>(() => loan.Renew(LoanDate.AddDays(9)));

        Assert.Contains("Overdue", exception.Message);
    }

    [Fact]
    public void Returned_loan_cannot_be_renewed()
    {
        var loan = PrintLoan();
        loan.Return(LoanDate.AddDays(1));

        Assert.Throws<DomainException>(() => loan.Renew(LoanDate.AddDays(2)));
    }

    [Fact]
    public void Renewal_resets_the_reminder()
    {
        var loan = PrintLoan();
        loan.MarkReminderSent(LoanDate.AddDays(6));

        loan.Renew(LoanDate.AddDays(6));

        Assert.Null(loan.ReminderSentAt);
    }

    [Fact]
    public void Reading_progress_is_saved_for_active_ebooks()
    {
        var loan = EbookLoan();

        loan.UpdateReadingProgress("epubcfi(/6/4)", 42.456m, LoanDate.AddDays(1));

        Assert.Equal("epubcfi(/6/4)", loan.ReadingLocation);
        Assert.Equal(42.46m, loan.ReadingProgress);
    }

    [Fact]
    public void Reading_progress_is_rejected_for_printed_books_and_bad_values()
    {
        Assert.Throws<DomainException>(() => PrintLoan().UpdateReadingProgress("page 3", 10, LoanDate));
        Assert.Throws<DomainException>(() => EbookLoan().UpdateReadingProgress("page 3", 120, LoanDate));
        Assert.Throws<DomainException>(() => EbookLoan().UpdateReadingProgress("page 3", 10, LoanDate.AddDays(20)));
    }
}
