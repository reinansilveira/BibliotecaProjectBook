using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Fine
{
    private Fine()
    {
    }

    public Fine(Guid memberId, Guid loanId, int daysOverdue, decimal dailyRate, DateTime issuedAt)
    {
        if (daysOverdue <= 0)
        {
            throw new DomainException("A fine needs at least one day overdue.");
        }

        if (dailyRate <= 0)
        {
            throw new DomainException("The daily rate must be greater than zero.");
        }

        Id = Guid.NewGuid();
        MemberId = memberId;
        LoanId = loanId;
        DaysOverdue = daysOverdue;
        DailyRate = dailyRate;
        Amount = daysOverdue * dailyRate;
        IssuedAt = issuedAt;
        Status = FineStatus.Unpaid;
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public Guid LoanId { get; private set; }

    public int DaysOverdue { get; private set; }

    public decimal DailyRate { get; private set; }

    public decimal Amount { get; private set; }

    public FineStatus Status { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public string? PaymentReference { get; private set; }

    public void Pay(string paymentReference, DateTime now)
    {
        EnsureUnpaid();
        Status = FineStatus.Paid;
        PaymentReference = paymentReference;
        ResolvedAt = now;
    }

    public void Waive(DateTime now)
    {
        EnsureUnpaid();
        Status = FineStatus.Waived;
        ResolvedAt = now;
    }

    private void EnsureUnpaid()
    {
        if (Status == FineStatus.Unpaid)
        {
            return;
        }

        throw new DomainException("This fine has already been settled.");
    }
}
