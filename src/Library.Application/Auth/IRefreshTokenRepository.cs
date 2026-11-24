using Library.Domain.Entities;

namespace Library.Application.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListActiveByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
