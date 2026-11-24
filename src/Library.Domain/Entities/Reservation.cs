using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Reservation
{
    public const int HoldPeriodInHours = 48;

    private Reservation()
    {
    }

    public Reservation(Guid bookId, Guid memberId, BookFormat format, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        MemberId = memberId;
        Format = format;
        CreatedAt = createdAt;
        Status = ReservationStatus.Waiting;
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public BookFormat Format { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ReadyAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public bool IsOpen => Status is ReservationStatus.Waiting or ReservationStatus.Ready;

    public void MarkReady(DateTime now)
    {
        if (Status != ReservationStatus.Waiting)
        {
            throw new DomainException("Only waiting reservations can become ready.");
        }

        Status = ReservationStatus.Ready;
        ReadyAt = now;
        ExpiresAt = now.AddHours(HoldPeriodInHours);
    }

    public void Fulfill(DateTime now)
    {
        if (Status != ReservationStatus.Ready)
        {
            throw new DomainException("Only ready reservations can be fulfilled.");
        }

        Close(ReservationStatus.Fulfilled, now);
    }

    public void Cancel(DateTime now)
    {
        if (!IsOpen)
        {
            throw new DomainException("This reservation is already closed.");
        }

        Close(ReservationStatus.Cancelled, now);
    }

    public bool TryExpire(DateTime now)
    {
        if (Status != ReservationStatus.Ready || now < ExpiresAt)
        {
            return false;
        }

        Close(ReservationStatus.Expired, now);

        return true;
    }

    private void Close(ReservationStatus status, DateTime now)
    {
        Status = status;
        ClosedAt = now;
    }
}
