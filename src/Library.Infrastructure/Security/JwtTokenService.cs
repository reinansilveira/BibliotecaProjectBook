using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Library.Application.Abstractions;
using Library.Application.Auth;
using Library.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Library.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenSizeInBytes = 64;
    private const int OpaqueTokenSizeInBytes = 32;

    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtSettings> settings, TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public GeneratedAccessToken GenerateAccessToken(Member member)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_settings.GetSigningKey(), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, member.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, member.Email),
                new Claim(JwtRegisteredClaimNames.Name, member.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimNames.Role, member.Role.ToString())
            ])
        };

        return new GeneratedAccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenSizeInBytes));
        var expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_settings.RefreshTokenExpirationDays);

        return new GeneratedRefreshToken(token, ComputeHash(token), expiresAt);
    }

    public OpaqueToken GenerateOpaqueToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(OpaqueTokenSizeInBytes));

        return new OpaqueToken(token, ComputeHash(token));
    }

    public string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
