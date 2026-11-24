using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Loan
{
    public const int PrintLoanPeriodInDays = 7;
    public const int EbookLoanPeriodInDays = 14;
    public const int MaxRenewals = 1;

    private Loan()
    {
    }

    private Loan(Guid bookId, Guid memberId, BookFormat format, Guid? copyId, DateTime loanDate)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        MemberId = memberId;
        Format = format;
        CopyId = copyId;
        LoanDate = loanDate;
        DueDate = loanDate.AddDays(PeriodInDays(format));
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public BookFormat Format { get; private set; }

    public Guid? CopyId { get; private set; }

    public DateTime LoanDate { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? ReturnedAt { get; private set; }

    public int RenewalCount { get; private set; }

    public string? ReadingLocation { get; private set; }

    public decimal ReadingProgress { get; private set; }

    public DateTime? ReminderSentAt { get; private set; }

    public DateTime? OverdueNoticeSentAt { get; private set; }

    public bool IsReturned => ReturnedAt.HasValue;

    public static Loan ForPrint(Guid bookId, Guid copyId, Guid memberId, DateTime loanDate) =>
        new(bookId, memberId, BookFormat.Print, copyId, loanDate);

    public static Loan ForEbook(Guid bookId, Guid memberId, DateTime loanDate) =>
        new(bookId, memberId, BookFormat.Ebook, null, loanDate);

    public static int PeriodInDays(BookFormat format) =>
        format == BookFormat.Ebook ? EbookLoanPeriodInDays : PrintLoanPeriodInDays;

    public bool IsOverdue(DateTime now) => Format == BookFormat.Print && !IsReturned && now > DueDate;

    public bool IsExpired(DateTime now) => Format == BookFormat.Ebook && !IsReturned && now >= DueDate;

    public bool IsActive(DateTime now) => !IsReturned && !IsExpired(now);

    public int DaysOverdue(DateTime now)
    {
        if (Format != BookFormat.Print)
        {
            return 0;
        }

        var end = ReturnedAt ?? now;

        if (end <= DueDate)
        {
            return 0;
        }

        return (int)Math.Ceiling((end - DueDate).TotalDays);
    }

    public void Return(DateTime returnedAt)
    {
        if (IsReturned)
        {
            throw new DomainException("This loan has already been returned.");
        }

        ReturnedAt = returnedAt;
    }

    public void Expire()
    {
        if (Format != BookFormat.Ebook || IsReturned)
        {
            return;
        }

        ReturnedAt = DueDate;
    }

    public void Renew(DateTime now)
    {
        if (!IsActive(now))
        {
            throw new DomainException("Only active loans can be renewed.");
        }

        if (IsOverdue(now))
        {
            throw new DomainException("Overdue loans cannot be renewed. Return the book first.");
        }

        if (RenewalCount >= MaxRenewals)
        {
            throw new DomainException($"A loan can be renewed only {MaxRenewals} time.");
        }

        DueDate = DueDate.AddDays(PeriodInDays(Format));
        RenewalCount++;
        ReminderSentAt = null;
    }

    public void UpdateReadingProgress(string location, decimal percentage, DateTime now)
    {
        if (Format != BookFormat.Ebook)
        {
            throw new DomainException("Reading progress is only tracked for e-books.");
        }

        if (!IsActive(now))
        {
            throw new DomainException("This e-book loan is no longer active.");
        }

        if (percentage is < 0 or > 100)
        {
            throw new DomainException("The reading progress must be between 0 and 100.");
        }

        ReadingLocation = location;
        ReadingProgress = Math.Round(percentage, 2);
    }

    public void MarkReminderSent(DateTime now)
    {
        ReminderSentAt = now;
    }

    public void MarkOverdueNoticeSent(DateTime now)
    {
        OverdueNoticeSentAt = now;
    }
}
