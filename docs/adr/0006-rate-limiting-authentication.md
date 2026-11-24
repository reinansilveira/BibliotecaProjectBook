# 0006. Rate limiting on authentication endpoints

## Context

Sign-in accepts an email and a password from anyone. Without a limit, an attacker can try thousands of passwords per minute, and the expensive PBKDF2 check turns the same traffic into a denial-of-service risk.

## Decision

Use the built-in ASP.NET Core rate limiter with a fixed-window policy partitioned by client IP, applied to sign-in, sign-up, refresh and logout. The default is 10 requests per minute and both values come from configuration. Rejected requests return `429 Too Many Requests` as `ProblemDetails` with a `Retry-After` header.

The rest of the API is not limited, so normal browsing and the load test are not affected. When the API runs behind a reverse proxy, `RateLimiting:TrustForwardedHeaders` enables the forwarded headers middleware so the limit applies to the real client IP; it is off by default because trusting `X-Forwarded-For` from the open internet would let clients choose their own IP.

## Consequences

- Password guessing from a single IP is slowed down to a handful of attempts per minute.
- Users behind the same NAT share the limit; the window is short enough that this is rarely noticed.
- A distributed attack from many IPs is not stopped by this alone; it would need a gateway or WAF in front of the API.
