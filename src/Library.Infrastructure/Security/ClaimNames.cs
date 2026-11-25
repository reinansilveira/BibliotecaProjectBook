using Microsoft.IdentityModel.JsonWebTokens;

namespace Library.Infrastructure.Security;

public static class ClaimNames
{
    public const string Role = "role";
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string MemberId = JwtRegisteredClaimNames.Sub;
}
