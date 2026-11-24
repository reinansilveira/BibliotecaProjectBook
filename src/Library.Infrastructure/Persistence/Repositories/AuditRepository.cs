using Library.Application.Audit;
using Library.Application.Common.Pagination;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly LibraryDbContext _context;

    public AuditRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<PagedResult<AuditEntry>> ListAsync(AuditListRequest request, CancellationToken cancellationToken = default)
    {
        var entries = _context.AuditEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            entries = entries.Where(entry => entry.EntityType == request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            entries = entries.Where(entry => entry.EntityId == request.EntityId);
        }

        if (request.ActorId.HasValue)
        {
            entries = entries.Where(entry => entry.ActorId == request.ActorId);
        }

        if (request.From.HasValue)
        {
            var from = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            entries = entries.Where(entry => entry.OccurredAt >= from);
        }

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            entries = entries.Where(entry => entry.OccurredAt < toExclusive);
        }

        return entries
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Id)
            .ToPagedResultAsync(request, cancellationToken);
    }
}
