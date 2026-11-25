namespace Library.Application.Events;

public sealed record LoanDueSoonEvent(
    Guid LoanId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    DateTime DueDate,
    bool IsEbook) : IIntegrationEvent;
