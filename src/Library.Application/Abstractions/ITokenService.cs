using Library.Application.Auth;
using Library.Domain.Entities;

namespace Library.Application.Abstractions;

public interface ITokenService
{
    GeneratedAccessToken GenerateAccessToken(Member member);

    GeneratedRefreshToken GenerateRefreshToken();

    OpaqueToken GenerateOpaqueToken();

    string ComputeHash(string token);
}
