namespace Library.Application.Audit;

public sealed record AuditEntryResponse(
    Guid Id,
    DateTime OccurredAt,
    Guid? ActorId,
    string ActorName,
    string Action,
    string EntityType,
    string EntityId,
    string Changes);
