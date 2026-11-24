using System.Net;
using Library.IntegrationTests.Infrastructure;

namespace Library.IntegrationTests;

public class HostingConfigurationTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    private const string FrontEndOrigin = "https://library-web.example.com";

    private HttpClient CreateClient(params (string Key, string Value)[] settings) =>
        Factory.WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }).CreateClient();

    [Fact]
    public async Task Configured_front_end_origin_passes_the_cors_preflight()
    {
        var client = CreateClient(("Cors:AllowedOrigins:0", FrontEndOrigin));

        var response = await client.SendAsync(Preflight(FrontEndOrigin), Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(FrontEndOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Unknown_origin_gets_no_cors_headers()
    {
        var client = CreateClient(("Cors:AllowedOrigins:0", FrontEndOrigin));

        var response = await client.SendAsync(Preflight("https://evil.example.com"), Token);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Swagger_is_hidden_outside_development_unless_enabled()
    {
        var hidden = await Anonymous.GetAsync("/swagger/v1/swagger.json", Token);
        var enabled = await CreateClient(("Swagger:Enabled", "true")).GetAsync("/swagger/v1/swagger.json", Token);

        Assert.NotEqual(HttpStatusCode.OK, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        return request;
    }
}
