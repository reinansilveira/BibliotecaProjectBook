using FluentValidation;
using Library.Application.Common.Pagination;

namespace Library.Application.Audit;

public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;
    private readonly IValidator<AuditListRequest> _validator;

    public AuditService(IAuditRepository audit, IValidator<AuditListRequest> validator)
    {
        _audit = audit;
        _validator = validator;
    }

    public async Task<PagedResult<AuditEntryResponse>> ListAsync(AuditListRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _audit.ListAsync(request, cancellationToken);

        return page.Map(entry => new AuditEntryResponse(
            entry.Id,
            entry.OccurredAt,
            entry.ActorId,
            entry.ActorName,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.Changes));
    }
}
