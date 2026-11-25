using Library.Domain.Enums;

namespace Library.Application.Reservations;

public sealed record ReservationRequest(Guid BookId, Guid MemberId, BookFormat Format = BookFormat.Print);
