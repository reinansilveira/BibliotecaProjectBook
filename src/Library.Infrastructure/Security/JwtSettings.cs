using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Library.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32, ErrorMessage = "The JWT key must be at least 32 characters long.")]
    public string Key { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
