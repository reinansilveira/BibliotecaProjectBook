namespace Library.Application.Events;

public sealed record ReservationReadyEvent(
    Guid ReservationId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    DateTime ExpiresAt) : IIntegrationEvent;
