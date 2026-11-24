# 0003. Short-lived JWT with rotating refresh tokens

## Context

A JWT cannot be revoked before it expires. A long-lived token is dangerous if stolen; a short-lived one forces users to sign in all the time.

## Decision

- Access tokens last 15 minutes.
- Refresh tokens are 64 random bytes, valid for 7 days, and stored only as a SHA-256 hash.
- Every refresh rotates the token: the old one is revoked and points to its replacement.
- If a revoked refresh token is used again, every active session of that member is revoked (reuse detection).
- Authorization uses roles (`Admin`, `Member`), a fallback policy that requires authentication everywhere, and a resource-based handler so members only access their own data.

## Consequences

- A stolen access token is useful for at most 15 minutes; a stolen refresh token is detected on reuse.
- Logout only revokes the refresh token; the current access token stays valid until it expires.
