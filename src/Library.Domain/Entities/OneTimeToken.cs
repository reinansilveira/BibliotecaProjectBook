using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class OneTimeToken
{
    private OneTimeToken()
    {
    }

    public OneTimeToken(Guid memberId, TokenPurpose purpose, string tokenHash, DateTime createdAt, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        MemberId = memberId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public TokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public bool IsUsable(DateTime now) => !UsedAt.HasValue && now < ExpiresAt;

    public void Use(DateTime now)
    {
        if (!IsUsable(now))
        {
            throw new DomainException("This link is invalid or has expired.");
        }

        UsedAt = now;
    }

    public void Invalidate(DateTime now)
    {
        UsedAt ??= now;
    }
}
