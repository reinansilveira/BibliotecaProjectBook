namespace Library.Application.Auth;

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    Guid MemberId,
    string Name,
    string Role);
