namespace Library.Application.Events;

public sealed record LoanReturnedEvent(
    Guid LoanId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    DateTime ReturnedAt,
    bool WasOverdue) : IIntegrationEvent;
