namespace Library.Application.Events;

public sealed record PasswordResetRequestedEvent(Guid MemberId, string Name, string Email, string Link) : IIntegrationEvent;
