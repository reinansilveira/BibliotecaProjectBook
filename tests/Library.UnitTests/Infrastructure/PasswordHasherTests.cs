using Library.Infrastructure.Security;

namespace Library.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verifies_the_correct_password()
    {
        var hash = _hasher.Hash("MyPassword@123");

        Assert.True(_hasher.Verify("MyPassword@123", hash));
        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Same_password_produces_different_hashes_because_of_the_salt()
    {
        Assert.NotEqual(_hasher.Hash("MyPassword@123"), _hasher.Hash("MyPassword@123"));
    }

    [Fact]
    public void Malformed_hash_is_rejected()
    {
        Assert.False(_hasher.Verify("anything", "malformed"));
    }
}
