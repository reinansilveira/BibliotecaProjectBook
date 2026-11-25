namespace Library.Application.Events;

public sealed record MemberRegisteredEvent(Guid MemberId, string Name, string Email) : IIntegrationEvent;
