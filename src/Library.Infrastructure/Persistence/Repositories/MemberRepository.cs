using Library.Application.Common.Pagination;
using Library.Application.Members;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Members.FirstOrDefaultAsync(member => member.Id == id, cancellationToken);

    public Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Members.FirstOrDefaultAsync(member => member.Email == email, cancellationToken);

    public Task<PagedResult<Member>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Members.AsNoTracking().Where(member => !member.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(member => member.Name.Contains(search) || member.Email.Contains(search));
        }

        return query
            .OrderBy(member => member.Name)
            .ThenBy(member => member.Id)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<Member>> ListByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var memberIds = ids.ToList();

        return await _context.Members
            .Where(member => memberIds.Contains(member.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsEmailInUseAsync(string email, Guid? excludingMemberId = null, CancellationToken cancellationToken = default) =>
        _context.Members.AnyAsync(
            member => member.Email == email && (excludingMemberId == null || member.Id != excludingMemberId),
            cancellationToken);

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default) =>
        await _context.Members.AddAsync(member, cancellationToken);
}
