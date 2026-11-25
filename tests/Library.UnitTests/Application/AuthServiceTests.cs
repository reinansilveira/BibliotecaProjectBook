using Library.Application.Abstractions;
using Library.Application.Auth;
using Library.Application.Common.Exceptions;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Infrastructure.Security;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class AuthServiceTests
{
    private const string Password = "Password@123";

    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly AuthService _service;
    private readonly Member _member = TestData.Member();

    public AuthServiceTests()
    {
        var jwt = new JwtSettings
        {
            Issuer = "tests",
            Audience = "tests",
            Key = "test-key-with-at-least-thirty-two-characters!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _service = new AuthService(
            _members,
            _refreshTokens,
            _unitOfWork,
            _passwordHasher,
            new JwtTokenService(Options.Create(jwt), _clock),
            new LoginRequestValidator(),
            _clock,
            NullLogger<AuthService>.Instance);

        _members.GetByEmailAsync(_member.Email.ToLowerInvariant(), Arg.Any<CancellationToken>()).Returns(_member);
        _members.GetByIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(_member);
        _passwordHasher.Verify(Password, _member.PasswordHash).Returns(true);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private Task<TokenResponse> LoginAsync() => _service.LoginAsync(new LoginRequest(_member.Email, Password), Token);

    private Task<TokenResponse> RefreshAsync(string refreshToken) => _service.RefreshAsync(new RefreshTokenRequest(refreshToken), Token);

    [Fact]
    public async Task Login_returns_a_short_access_token_and_a_long_refresh_token()
    {
        var response = await _service.LoginAsync(new LoginRequest($" {_member.Email.ToUpperInvariant()} ", Password), Token);

        Assert.Equal(Now.AddMinutes(15), response.AccessTokenExpiresAt);
        Assert.Equal(Now.AddDays(7), response.RefreshTokenExpiresAt);
        Assert.NotEmpty(response.RefreshToken);
    }

    [Fact]
    public async Task Only_the_refresh_token_hash_is_stored()
    {
        var response = await LoginAsync();

        var stored = Assert.Single(_refreshTokens.Tokens);
        Assert.NotEqual(response.RefreshToken, stored.TokenHash);
    }

    [Fact]
    public async Task Wrong_password_is_unauthorized()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(new LoginRequest(_member.Email, "wrong"), Token));

        Assert.Equal("Invalid email or password.", exception.Message);
        Assert.Empty(_refreshTokens.Tokens);
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_message_as_a_wrong_password()
    {
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(new LoginRequest("nobody@email.com", Password), Token));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task Deleted_member_cannot_sign_in()
    {
        _member.Delete();

        await Assert.ThrowsAsync<UnauthorizedException>(LoginAsync);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_revokes_the_old_one()
    {
        var login = await LoginAsync();
        _clock.Advance(TimeSpan.FromMinutes(20));

        var refreshed = await RefreshAsync(login.RefreshToken);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(Now.AddMinutes(15), refreshed.AccessTokenExpiresAt);

        var previous = _refreshTokens.Tokens[0];
        var replacement = _refreshTokens.Tokens[1];
        Assert.True(previous.IsRevoked);
        Assert.Equal(replacement.Id, previous.ReplacedByTokenId);
        Assert.True(replacement.IsActive(Now));
    }

    [Fact]
    public async Task Reusing_an_old_refresh_token_revokes_every_session()
    {
        var login = await LoginAsync();
        var refreshed = await RefreshAsync(login.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken));

        Assert.All(_refreshTokens.Tokens, token => Assert.True(token.IsRevoked));
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(refreshed.RefreshToken));
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var login = await LoginAsync();
        _clock.Advance(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken));
    }

    [Fact]
    public async Task Unknown_refresh_token_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("token-that-never-existed"));
    }

    [Fact]
    public async Task Deleted_member_cannot_refresh()
    {
        var login = await LoginAsync();
        _member.Delete();

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken));
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var login = await LoginAsync();

        await _service.LogoutAsync(new RefreshTokenRequest(login.RefreshToken), Token);

        Assert.True(_refreshTokens.Tokens[0].IsRevoked);
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken));
    }
}
