using Library.Domain.Entities;

namespace Library.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Token_is_active_until_it_expires()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", Now, Now.AddDays(7));

        Assert.True(token.IsActive(Now.AddDays(6)));
        Assert.False(token.IsActive(Now.AddDays(7)));
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_revocation()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", Now, Now.AddDays(7));
        var replacement = Guid.NewGuid();

        token.Revoke(Now.AddHours(1), replacement);
        token.Revoke(Now.AddHours(2));

        Assert.Equal(Now.AddHours(1), token.RevokedAt);
        Assert.Equal(replacement, token.ReplacedByTokenId);
    }
}
