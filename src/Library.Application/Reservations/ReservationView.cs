using Library.Domain.Entities;

namespace Library.Application.Reservations;

public sealed record ReservationView(Reservation Reservation, string BookTitle, string BookAuthor, string? CoverUrl, string MemberName, int? Position)
{
    public ReservationResponse ToResponse() => new(
        Reservation.Id,
        Reservation.BookId,
        BookTitle,
        BookAuthor,
        CoverUrl,
        Reservation.MemberId,
        MemberName,
        Reservation.Format.ToString(),
        Reservation.Status.ToString(),
        Position,
        Reservation.CreatedAt,
        Reservation.ReadyAt,
        Reservation.ExpiresAt);
}
