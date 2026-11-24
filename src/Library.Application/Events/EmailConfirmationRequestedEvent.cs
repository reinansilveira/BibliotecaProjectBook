namespace Library.Application.Events;

public sealed record EmailConfirmationRequestedEvent(Guid MemberId, string Name, string Email, string Link) : IIntegrationEvent;
