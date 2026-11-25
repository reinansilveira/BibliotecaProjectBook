using Library.Application.Account;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class OneTimeTokenRepository : IOneTimeTokenRepository
{
    private readonly LibraryDbContext _context;

    public OneTimeTokenRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<OneTimeToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.OneTimeTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<OneTimeToken>> ListUsableAsync(Guid memberId, TokenPurpose purpose, DateTime now, CancellationToken cancellationToken = default) =>
        await _context.OneTimeTokens
            .Where(token => token.MemberId == memberId && token.Purpose == purpose && token.UsedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OneTimeToken token, CancellationToken cancellationToken = default) =>
        await _context.OneTimeTokens.AddAsync(token, cancellationToken);
}
