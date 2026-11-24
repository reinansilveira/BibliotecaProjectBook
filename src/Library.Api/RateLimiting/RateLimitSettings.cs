namespace Library.Api.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int AuthenticationPermitLimit { get; set; } = 10;

    public int AuthenticationWindowSeconds { get; set; } = 60;

    public bool TrustForwardedHeaders { get; set; }
}
