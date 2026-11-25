using Library.Application.Common.Pagination;

namespace Library.Application.Audit;

public sealed class AuditListRequest : PageRequest
{
    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public Guid? ActorId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}
