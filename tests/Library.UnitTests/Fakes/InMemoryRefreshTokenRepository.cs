using Library.Application.Auth;
using Library.Domain.Entities;

namespace Library.UnitTests.Fakes;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> ListActiveByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Tokens.Where(token => token.MemberId == memberId && token.IsActive(now)).ToList());

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Tokens.Add(refreshToken);

        return Task.CompletedTask;
    }
}
