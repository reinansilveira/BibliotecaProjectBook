using System.Net.Http.Headers;
using System.Net.Http.Json;
using Library.Application.Auth;
using Library.Application.Books;
using Library.Application.Members;

namespace Library.IntegrationTests.Infrastructure;

public abstract class ApiTestBase
{
    protected ApiTestBase(LibraryApiFactory factory)
    {
        Factory = factory;
        Anonymous = factory.CreateClient();
    }

    protected LibraryApiFactory Factory { get; }

    protected HttpClient Anonymous { get; }

    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    protected static HttpClient WithToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    protected async Task<TokenResponse> LoginAsync(string email, string password)
    {
        var response = await Anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenResponse>(Token))!;
    }

    protected async Task<string> GetAccessTokenAsync(string email, string password) =>
        (await LoginAsync(email, password)).AccessToken;

    protected async Task<HttpClient> CreateAdminClientAsync() =>
        WithToken(Factory.CreateClient(), await GetAccessTokenAsync(LibraryApiFactory.AdminEmail, LibraryApiFactory.AdminPassword));

    protected async Task<(MemberResponse Member, string AccessToken, HttpClient Client)> RegisterAndSignInAsync()
    {
        var request = NewMemberRequest();

        var response = await Anonymous.PostAsJsonAsync("/api/auth/register", request, Token);
        response.EnsureSuccessStatusCode();
        var member = (await response.Content.ReadFromJsonAsync<MemberResponse>(Token))!;

        var accessToken = await GetAccessTokenAsync(request.Email, request.Password);

        return (member, accessToken, WithToken(Factory.CreateClient(), accessToken));
    }

    protected async Task<BookResponse> CreateBookAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/books", new BookRequest("Dune", "Frank Herbert", "Science fiction"), Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BookResponse>(Token))!;
    }

    protected static RegisterMemberRequest NewMemberRequest() =>
        new("Test Reader", new DateTime(1995, 5, 10), "555-0100", $"reader-{Guid.NewGuid():N}@tests.com", "Password@123");
}
