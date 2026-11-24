using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Account;

public interface IOneTimeTokenRepository
{
    Task<OneTimeToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OneTimeToken>> ListUsableAsync(Guid memberId, TokenPurpose purpose, DateTime now, CancellationToken cancellationToken = default);

    Task AddAsync(OneTimeToken token, CancellationToken cancellationToken = default);
}
