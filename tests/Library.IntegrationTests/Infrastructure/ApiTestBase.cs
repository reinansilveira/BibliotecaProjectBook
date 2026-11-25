using System.Net.Http.Headers;
using System.Net.Http.Json;
using Library.Application.Auth;
using Library.Application.Books;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Enums;

namespace Library.IntegrationTests.Infrastructure;

public abstract class ApiTestBase
{
    protected const string Api = "/api/v1";

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
        var response = await Anonymous.PostAsJsonAsync($"{Api}/auth/login", new LoginRequest(email, password), Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenResponse>(Token))!;
    }

    protected async Task<string> GetAccessTokenAsync(string email, string password) =>
        (await LoginAsync(email, password)).AccessToken;

    protected Task<string> GetAdminTokenAsync() => GetAccessTokenAsync(LibraryApiFactory.AdminEmail, LibraryApiFactory.AdminPassword);

    protected async Task<HttpClient> CreateAdminClientAsync() => WithToken(Factory.CreateClient(), await GetAdminTokenAsync());

    protected async Task<(MemberResponse Member, string AccessToken, HttpClient Client)> RegisterAndSignInAsync(string name = "Test Reader")
    {
        var request = NewMemberRequest() with { Name = name };

        var response = await Anonymous.PostAsJsonAsync($"{Api}/auth/register", request, Token);
        response.EnsureSuccessStatusCode();
        var member = (await response.Content.ReadFromJsonAsync<MemberResponse>(Token))!;

        var accessToken = await GetAccessTokenAsync(request.Email, request.Password);

        return (member, accessToken, WithToken(Factory.CreateClient(), accessToken));
    }

    protected async Task<BookResponse> CreateBookAsync(HttpClient admin, int copies = 1, string? title = null, string genre = "Science Fiction")
    {
        var request = new CreateBookRequest
        {
            Title = title ?? $"Dune {Guid.NewGuid():N}"[..20],
            Author = "Frank Herbert",
            Description = "Science fiction",
            Genre = genre,
            PublishedYear = 1965,
            Copies = copies
        };

        var response = await admin.PostAsJsonAsync($"{Api}/books", request, Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BookResponse>(Token))!;
    }

    protected Task<HttpResponseMessage> BorrowAsync(HttpClient client, Guid bookId, Guid memberId, BookFormat format = BookFormat.Print) =>
        client.PostAsJsonAsync($"{Api}/loans", new CreateLoanRequest(bookId, memberId, format), Token);

    protected async Task<LoanResponse> BorrowOkAsync(HttpClient client, Guid bookId, Guid memberId, BookFormat format = BookFormat.Print)
    {
        var response = await BorrowAsync(client, bookId, memberId, format);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LoanResponse>(Token))!;
    }

    protected static RegisterMemberRequest NewMemberRequest() =>
        new("Test Reader", new DateTime(1995, 5, 10), "555-0100", $"reader-{Guid.NewGuid():N}@tests.com", "Password@123");
}
