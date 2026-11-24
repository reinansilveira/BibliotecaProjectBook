using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class ReservationTests
{
    private static readonly DateTime Now = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Reservation NewReservation() => new(Guid.NewGuid(), Guid.NewGuid(), BookFormat.Print, Now);

    [Fact]
    public void New_reservation_waits_in_line()
    {
        var reservation = NewReservation();

        Assert.Equal(ReservationStatus.Waiting, reservation.Status);
        Assert.True(reservation.IsOpen);
    }

    [Fact]
    public void Ready_reservation_is_held_for_48_hours()
    {
        var reservation = NewReservation();

        reservation.MarkReady(Now);

        Assert.Equal(ReservationStatus.Ready, reservation.Status);
        Assert.Equal(Now.AddHours(48), reservation.ExpiresAt);
    }

    [Fact]
    public void Hold_expires_only_after_the_deadline()
    {
        var reservation = NewReservation();
        reservation.MarkReady(Now);

        Assert.False(reservation.TryExpire(Now.AddHours(47)));
        Assert.True(reservation.TryExpire(Now.AddHours(48)));
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.False(reservation.IsOpen);
    }

    [Fact]
    public void Waiting_reservation_never_expires()
    {
        Assert.False(NewReservation().TryExpire(Now.AddDays(30)));
    }

    [Fact]
    public void Only_ready_reservations_can_be_fulfilled()
    {
        var reservation = NewReservation();

        Assert.Throws<DomainException>(() => reservation.Fulfill(Now));

        reservation.MarkReady(Now);
        reservation.Fulfill(Now);

        Assert.Equal(ReservationStatus.Fulfilled, reservation.Status);
        Assert.Equal(Now, reservation.ClosedAt);
    }

    [Fact]
    public void Closed_reservation_cannot_be_cancelled_or_marked_ready()
    {
        var reservation = NewReservation();
        reservation.Cancel(Now);

        Assert.Throws<DomainException>(() => reservation.Cancel(Now));
        Assert.Throws<DomainException>(() => reservation.MarkReady(Now));
    }
}
