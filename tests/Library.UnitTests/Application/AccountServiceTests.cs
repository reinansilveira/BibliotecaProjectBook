using Library.Application.Abstractions;
using Library.Application.Account;
using Library.Application.Events;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Infrastructure.Security;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class AccountServiceTests
{
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IOneTimeTokenRepository _tokens = Substitute.For<IOneTimeTokenRepository>();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();
    private readonly RecordingEventPublisher _events = new();
    private readonly RecordingInbox _inbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly AccountService _service;
    private readonly Member _member;
    private readonly List<OneTimeToken> _issued = [];

    public AccountServiceTests()
    {
        _tokenService = new JwtTokenService(Options.Create(new JwtSettings { Key = new string('k', 40) }), _clock);
        _member = new Member("Ana Lima", new DateTime(1990, 1, 1), "555", "ana@email.com", _passwordHasher.Hash("Old@1234"), Now);

        _service = new AccountService(
            _members, _tokens, _refreshTokens, _tokenService, _passwordHasher, _events, _inbox, _unitOfWork,
            new ConfirmEmailRequestValidator(), new ForgotPasswordRequestValidator(), new ResetPasswordRequestValidator(),
            new ChangePasswordRequestValidator(), Options.Create(new AccountSettings { FrontEndUrl = "https://library.test/" }), _clock);

        _members.GetByIdAsync(_member.Id, Arg.Any<CancellationToken>()).Returns(_member);
        _members.GetByEmailAsync("ana@email.com", Arg.Any<CancellationToken>()).Returns(_member);
        _tokens.When(tokens => tokens.AddAsync(Arg.Any<OneTimeToken>(), Arg.Any<CancellationToken>())).Do(call => _issued.Add(call.Arg<OneTimeToken>()));
        _tokens.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _issued.FirstOrDefault(token => token.TokenHash == call.Arg<string>()));
        _tokens.ListUsableAsync(_member.Id, Arg.Any<TokenPurpose>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => _issued.Where(token => token.Purpose == call.ArgAt<TokenPurpose>(1) && token.IsUsable(Now)).ToList());
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private static string TokenFrom(string link) => Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + 6)..]);

    [Fact]
    public async Task Forgot_password_for_an_unknown_email_reveals_nothing()
    {
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@email.com"), Token);

        Assert.Empty(_events.Events);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Reset_link_changes_the_password_once_and_signs_out_everywhere()
    {
        await _refreshTokens.AddAsync(new RefreshToken(_member.Id, "hash", Now, Now.AddDays(7)), Token);
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("ANA@email.com"), Token);
        var requested = Assert.IsType<PasswordResetRequestedEvent>(Assert.Single(_events.Events));
        Assert.StartsWith("https://library.test/reset-password?token=", requested.Link);
        var rawToken = TokenFrom(requested.Link);

        await _service.ResetPasswordAsync(new ResetPasswordRequest(rawToken, "New@12345"), Token);

        Assert.True(_passwordHasher.Verify("New@12345", _member.PasswordHash));
        Assert.All(_refreshTokens.Tokens, token => Assert.True(token.IsRevoked));
        await Assert.ThrowsAsync<DomainException>(() => _service.ResetPasswordAsync(new ResetPasswordRequest(rawToken, "Other@12345"), Token));
    }

    [Fact]
    public async Task New_reset_request_invalidates_the_previous_link()
    {
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("ana@email.com"), Token);
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("ana@email.com"), Token);

        Assert.False(_issued[0].IsUsable(Now));
        Assert.True(_issued[1].IsUsable(Now));
    }

    [Fact]
    public async Task Expired_reset_link_is_rejected()
    {
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("ana@email.com"), Token);
        var rawToken = TokenFrom(Assert.IsType<PasswordResetRequestedEvent>(Assert.Single(_events.Events)).Link);
        _clock.Advance(TimeSpan.FromHours(2));

        await Assert.ThrowsAsync<DomainException>(() => _service.ResetPasswordAsync(new ResetPasswordRequest(rawToken, "New@12345"), Token));
    }

    [Fact]
    public async Task Confirmation_link_confirms_the_email()
    {
        await _service.SendEmailConfirmationAsync(_member.Id, Token);
        var requested = Assert.IsType<EmailConfirmationRequestedEvent>(Assert.Single(_events.Events));

        await _service.ConfirmEmailAsync(new ConfirmEmailRequest(TokenFrom(requested.Link)), Token);

        Assert.True(_member.IsEmailConfirmed);
    }

    [Fact]
    public async Task Reset_token_cannot_confirm_an_email()
    {
        await _service.ForgotPasswordAsync(new ForgotPasswordRequest("ana@email.com"), Token);
        var rawToken = TokenFrom(Assert.IsType<PasswordResetRequestedEvent>(Assert.Single(_events.Events)).Link);

        await Assert.ThrowsAsync<DomainException>(() => _service.ConfirmEmailAsync(new ConfirmEmailRequest(rawToken), Token));
    }

    [Fact]
    public async Task Confirmed_member_gets_no_new_confirmation_email()
    {
        _member.ConfirmEmail(Now);

        await _service.SendEmailConfirmationAsync(_member.Id, Token);

        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Change_password_requires_the_current_one()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ChangePasswordAsync(_member.Id, new ChangePasswordRequest("Wrong@123", "New@12345"), Token));

        await _service.ChangePasswordAsync(_member.Id, new ChangePasswordRequest("Old@1234", "New@12345"), Token);

        Assert.True(_passwordHasher.Verify("New@12345", _member.PasswordHash));
    }
}
