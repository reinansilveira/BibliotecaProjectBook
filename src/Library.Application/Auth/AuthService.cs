using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Members;
using Library.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Library.Application.Auth;

public sealed class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidSession = "Invalid or expired session. Please sign in again.";

    private readonly IMemberRepository _members;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IMemberRepository members,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IValidator<LoginRequest> loginValidator,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _members = members;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _loginValidator = loginValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var member = await _members.GetByEmailAsync(email, cancellationToken);

        if (member is null || member.IsDeleted || !_passwordHasher.Verify(request.Password, member.PasswordHash))
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        var (response, _) = await IssueTokensAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var current = await FindRefreshTokenAsync(request, cancellationToken)
            ?? throw new UnauthorizedException(InvalidSession);

        if (current.IsRevoked)
        {
            _logger.LogWarning("Refresh token reuse detected for member {MemberId}. All sessions were revoked.", current.MemberId);
            await RevokeAllSessionsAsync(current.MemberId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(InvalidSession);
        }

        if (!current.IsActive(Now))
        {
            throw new UnauthorizedException(InvalidSession);
        }

        var member = await _members.GetByIdAsync(current.MemberId, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            current.Revoke(Now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(InvalidSession);
        }

        var (response, replacement) = await IssueTokensAsync(member, cancellationToken);
        current.Revoke(Now, replacement.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = await FindRefreshTokenAsync(request, cancellationToken);

        if (token is null || token.IsRevoked)
        {
            return;
        }

        token.Revoke(Now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshToken?> FindRefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var hash = _tokenService.ComputeHash(request.RefreshToken);

        return await _refreshTokens.GetByHashAsync(hash, cancellationToken);
    }

    private async Task<(TokenResponse Response, RefreshToken Entity)> IssueTokensAsync(Member member, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(member);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var entity = new RefreshToken(member.Id, refreshToken.Hash, Now, refreshToken.ExpiresAt);

        await _refreshTokens.AddAsync(entity, cancellationToken);

        var response = new TokenResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            member.Id,
            member.Name,
            member.Role.ToString());

        return (response, entity);
    }

    private async Task RevokeAllSessionsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var activeTokens = await _refreshTokens.ListActiveByMemberAsync(memberId, Now, cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(Now);
        }
    }
}
