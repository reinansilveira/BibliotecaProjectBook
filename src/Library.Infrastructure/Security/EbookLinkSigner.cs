using System.Security.Cryptography;
using System.Text;
using Library.Application.Loans.Reading;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Library.Infrastructure.Security;

public sealed class EbookLinkSigner : IEbookLinkSigner
{
    private readonly byte[] _key;

    public EbookLinkSigner(IOptions<JwtSettings> settings)
    {
        _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(settings.Value.Key), "library-ebook-links"u8);
    }

    public string Sign(Guid loanId, long expiresAtUnixSeconds) =>
        Base64UrlEncoder.Encode(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{loanId:N}.{expiresAtUnixSeconds}")));

    public bool Verify(Guid loanId, long expiresAtUnixSeconds, string signature) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Sign(loanId, expiresAtUnixSeconds)),
            Encoding.ASCII.GetBytes(signature));
}
