using Library.Application.Blacklist;
using Library.Application.Common.Pagination;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class BlacklistRepository : IBlacklistRepository
{
    private readonly LibraryDbContext _context;

    public BlacklistRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<BlacklistEntry?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _context.BlacklistEntries.FirstOrDefaultAsync(entry => entry.MemberId == memberId, cancellationToken);

    public Task<PagedResult<BlacklistEntry>> ListAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        _context.BlacklistEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.AddedAt)
            .ThenBy(entry => entry.Id)
            .ToPagedResultAsync(request, cancellationToken);

    public Task<bool> IsBlacklistedAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _context.BlacklistEntries.AnyAsync(entry => entry.MemberId == memberId, cancellationToken);

    public async Task AddAsync(BlacklistEntry entry, CancellationToken cancellationToken = default) =>
        await _context.BlacklistEntries.AddAsync(entry, cancellationToken);

    public void Remove(BlacklistEntry entry) => _context.BlacklistEntries.Remove(entry);
}
