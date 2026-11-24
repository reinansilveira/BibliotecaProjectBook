using System.Net;
using System.Net.Http.Json;
using Library.Application.Auth;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Library.IntegrationTests;

public class RateLimitingTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    private const int PermitLimit = 3;

    private HttpClient CreateClientWithStrictLimit() =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:AuthenticationPermitLimit", PermitLimit.ToString()))
            .CreateClient();

    [Fact]
    public async Task Too_many_sign_in_attempts_return_429_with_retry_after()
    {
        var client = CreateClientWithStrictLimit();
        var credentials = new LoginRequest("attacker@tests.com", "guess");

        for (var attempt = 0; attempt < PermitLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/login", credentials, Token);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", credentials, Token);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
        var problem = await blocked.Content.ReadFromJsonAsync<ProblemDetails>(Token);
        Assert.Equal("Too many requests", problem!.Title);
    }

    [Fact]
    public async Task Other_endpoints_are_not_limited()
    {
        var client = CreateClientWithStrictLimit();

        for (var request = 0; request < PermitLimit * 3; request++)
        {
            var response = await client.GetAsync("/api/v1/books", Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
