namespace Library.Application.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string BookAuthor,
    string? CoverUrl,
    Guid MemberId,
    string MemberName,
    string Format,
    string Status,
    int? Position,
    DateTime CreatedAt,
    DateTime? ReadyAt,
    DateTime? ExpiresAt);
