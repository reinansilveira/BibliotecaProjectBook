using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Security;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Library.UnitTests.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings = new()
    {
        Issuer = "tests",
        Audience = "tests-clients",
        Key = "test-key-with-at-least-thirty-two-characters!",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UtcNow);

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private JwtTokenService CreateService() => new(Options.Create(_settings), _clock);

    [Fact]
    public async Task Access_token_carries_the_member_id_and_role()
    {
        var admin = new Member("Ana", new DateTime(1990, 1, 1), "555-0100", "ana@email.com", "hash", Now, Role.Admin);

        var token = CreateService().GenerateAccessToken(admin);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = _settings.GetSigningKey()
        });

        Assert.True(validation.IsValid);
        Assert.Equal(admin.Id.ToString(), validation.Claims[ClaimNames.MemberId]);
        Assert.Equal("Admin", validation.Claims[ClaimNames.Role]);
        Assert.Equal(Now.AddMinutes(15), token.ExpiresAt);
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        var token = CreateService().GenerateAccessToken(TestData.Member());

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new JwtSettings { Key = "another-key-with-at-least-thirty-two-chars" }.GetSigningKey()
        });

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void Refresh_tokens_are_unique_and_the_hash_is_deterministic()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(first.Hash, service.ComputeHash(first.Token));
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(Now.AddDays(7), first.ExpiresAt);
    }
}
