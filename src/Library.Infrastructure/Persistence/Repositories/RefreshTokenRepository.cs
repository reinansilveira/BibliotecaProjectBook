using Library.Application.Auth;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly LibraryDbContext _context;

    public RefreshTokenRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens
            .Where(token => token.MemberId == memberId && token.RevokedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
}
