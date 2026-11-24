namespace Library.Application.Events;

public sealed record FineIssuedEvent(
    Guid FineId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    int DaysOverdue,
    decimal Amount,
    string Currency) : IIntegrationEvent;
