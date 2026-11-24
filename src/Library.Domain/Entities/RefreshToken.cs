namespace Library.Domain.Entities;

public class RefreshToken
{
    private RefreshToken()
    {
    }

    public RefreshToken(Guid memberId, string tokenHash, DateTime createdAt, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        MemberId = memberId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive(DateTime now) => !IsRevoked && now < ExpiresAt;

    public void Revoke(DateTime revokedAt, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}
