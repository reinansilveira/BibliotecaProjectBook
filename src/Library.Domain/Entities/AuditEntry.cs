namespace Library.Domain.Entities;

public class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(
        DateTime occurredAt,
        Guid? actorId,
        string actorName,
        string action,
        string entityType,
        string entityId,
        string changes)
    {
        Id = Guid.NewGuid();
        OccurredAt = occurredAt;
        ActorId = actorId;
        ActorName = actorName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Changes = changes;
    }

    public Guid Id { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public Guid? ActorId { get; private set; }

    public string ActorName { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Changes { get; private set; } = string.Empty;
}
