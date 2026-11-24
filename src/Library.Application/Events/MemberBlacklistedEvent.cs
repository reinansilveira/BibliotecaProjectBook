namespace Library.Application.Events;

public sealed record MemberBlacklistedEvent(Guid MemberId, string MemberName, string MemberEmail) : IIntegrationEvent;
