using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Loan
{
    public const int LoanPeriodInDays = 7;

    private Loan()
    {
    }

    public Loan(Guid bookId, Guid memberId, DateTime loanDate)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        MemberId = memberId;
        LoanDate = loanDate;
        DueDate = loanDate.AddDays(LoanPeriodInDays);
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public DateTime LoanDate { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? ReturnedAt { get; private set; }

    public bool IsReturned => ReturnedAt.HasValue;

    public bool IsOverdue(DateTime now) => !IsReturned && now > DueDate;

    public void Return(DateTime returnedAt)
    {
        if (IsReturned)
        {
            throw new DomainException("This loan has already been returned.");
        }

        ReturnedAt = returnedAt;
    }
}
