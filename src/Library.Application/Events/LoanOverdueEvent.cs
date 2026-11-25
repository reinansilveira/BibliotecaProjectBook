namespace Library.Application.Events;

public sealed record LoanOverdueEvent(
    Guid LoanId,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string BookTitle,
    DateTime DueDate,
    decimal DailyRate,
    string Currency) : IIntegrationEvent;
