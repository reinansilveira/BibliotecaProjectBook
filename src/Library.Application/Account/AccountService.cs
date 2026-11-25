using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Auth;
using Library.Application.Common.Exceptions;
using Library.Application.Events;
using Library.Application.Inbox;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Library.Application.Account;

public sealed class AccountService : IAccountService
{
    private const string InvalidLink = "This link is invalid or has expired.";

    private readonly IMemberRepository _members;
    private readonly IOneTimeTokenRepository _tokens;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEventPublisher _events;
    private readonly IInbox _inbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ConfirmEmailRequest> _confirmValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;
    private readonly IValidator<ChangePasswordRequest> _changeValidator;
    private readonly AccountSettings _settings;
    private readonly TimeProvider _timeProvider;

    public AccountService(
        IMemberRepository members,
        IOneTimeTokenRepository tokens,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IEventPublisher events,
        IInbox inbox,
        IUnitOfWork unitOfWork,
        IValidator<ConfirmEmailRequest> confirmValidator,
        IValidator<ForgotPasswordRequest> forgotValidator,
        IValidator<ResetPasswordRequest> resetValidator,
        IValidator<ChangePasswordRequest> changeValidator,
        IOptions<AccountSettings> settings,
        TimeProvider timeProvider)
    {
        _members = members;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _events = events;
        _inbox = inbox;
        _unitOfWork = unitOfWork;
        _confirmValidator = confirmValidator;
        _forgotValidator = forgotValidator;
        _resetValidator = resetValidator;
        _changeValidator = changeValidator;
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task SendEmailConfirmationAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var member = await GetActiveMemberAsync(memberId, cancellationToken);

        if (member.IsEmailConfirmed)
        {
            return;
        }

        var token = await IssueAsync(member, TokenPurpose.EmailConfirmation, TimeSpan.FromHours(_settings.EmailConfirmationHours), cancellationToken);
        _events.Publish(new EmailConfirmationRequestedEvent(member.Id, member.Name, member.Email, BuildLink("confirm-email", token)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        await _confirmValidator.ValidateAndThrowAsync(request, cancellationToken);

        var (token, member) = await RedeemAsync(request.Token, TokenPurpose.EmailConfirmation, cancellationToken);

        token.Use(Now);
        member.ConfirmEmail(Now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await _forgotValidator.ValidateAndThrowAsync(request, cancellationToken);

        var member = await _members.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (member is null || member.IsDeleted)
        {
            return;
        }

        var token = await IssueAsync(member, TokenPurpose.PasswordReset, TimeSpan.FromMinutes(_settings.PasswordResetMinutes), cancellationToken);
        _events.Publish(new PasswordResetRequestedEvent(member.Id, member.Name, member.Email, BuildLink("reset-password", token)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await _resetValidator.ValidateAndThrowAsync(request, cancellationToken);

        var (token, member) = await RedeemAsync(request.Token, TokenPurpose.PasswordReset, cancellationToken);

        token.Use(Now);
        member.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        member.ConfirmEmail(Now);
        await SignOutEverywhereAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid memberId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        await _changeValidator.ValidateAndThrowAsync(request, cancellationToken);

        var member = await GetActiveMemberAsync(memberId, cancellationToken);

        if (!_passwordHasher.Verify(request.CurrentPassword, member.PasswordHash))
        {
            throw new DomainException("The current password is not correct.");
        }

        member.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        await SignOutEverywhereAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> IssueAsync(Member member, TokenPurpose purpose, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        foreach (var previous in await _tokens.ListUsableAsync(member.Id, purpose, Now, cancellationToken))
        {
            previous.Invalidate(Now);
        }

        var token = _tokenService.GenerateOpaqueToken();
        await _tokens.AddAsync(new OneTimeToken(member.Id, purpose, token.Hash, Now, Now.Add(lifetime)), cancellationToken);

        return token.Token;
    }

    private async Task<(OneTimeToken Token, Member Member)> RedeemAsync(string rawToken, TokenPurpose purpose, CancellationToken cancellationToken)
    {
        var token = await _tokens.GetByHashAsync(_tokenService.ComputeHash(rawToken), cancellationToken);

        if (token is null || token.Purpose != purpose || !token.IsUsable(Now))
        {
            throw new DomainException(InvalidLink);
        }

        var member = await _members.GetByIdAsync(token.MemberId, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            throw new DomainException(InvalidLink);
        }

        return (token, member);
    }

    private async Task SignOutEverywhereAsync(Member member, CancellationToken cancellationToken)
    {
        foreach (var refreshToken in await _refreshTokens.ListActiveByMemberAsync(member.Id, Now, cancellationToken))
        {
            refreshToken.Revoke(Now);
        }

        _inbox.Add(member.Id, NotificationTypes.PasswordChanged, "Password changed", "Your password was changed and every other session was signed out.", "/account");
    }

    private async Task<Member> GetActiveMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await _members.GetByIdAsync(memberId, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            throw new NotFoundException("Member not found.");
        }

        return member;
    }

    private string BuildLink(string path, string token) =>
        $"{_settings.FrontEndUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
}
