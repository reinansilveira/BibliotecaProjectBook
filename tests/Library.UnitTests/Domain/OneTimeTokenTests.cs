using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class OneTimeTokenTests
{
    private static readonly DateTime Now = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    private static OneTimeToken NewToken() => new(Guid.NewGuid(), TokenPurpose.PasswordReset, "hash", Now, Now.AddHours(1));

    [Fact]
    public void Token_can_be_used_only_once()
    {
        var token = NewToken();

        token.Use(Now.AddMinutes(5));

        Assert.False(token.IsUsable(Now.AddMinutes(6)));
        Assert.Throws<DomainException>(() => token.Use(Now.AddMinutes(6)));
    }

    [Fact]
    public void Expired_token_cannot_be_used()
    {
        Assert.Throws<DomainException>(() => NewToken().Use(Now.AddHours(2)));
    }

    [Fact]
    public void Invalidated_token_is_no_longer_usable()
    {
        var token = NewToken();

        token.Invalidate(Now);

        Assert.False(token.IsUsable(Now));
    }
}
