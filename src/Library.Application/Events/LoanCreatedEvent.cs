namespace Library.Application.Events;

public sealed record LoanCreatedEvent(
    Guid LoanId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    DateTime DueDate) : IIntegrationEvent;
