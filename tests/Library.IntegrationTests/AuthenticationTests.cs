using System.Net;
using System.Net.Http.Json;
using Library.Application.Auth;
using Library.Application.Books;
using Library.Application.Loans;
using Library.Application.Members;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Library.IntegrationTests;

public class AuthenticationTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Wrong_password_returns_401()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(LibraryApiFactory.AdminEmail, "wrong-password"), Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Token);
        Assert.Equal("Invalid email or password.", problem!.Detail);
    }

    [Fact]
    public async Task Protected_route_without_token_returns_401()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/books", new BookRequest("Book", "Author", ""), Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_token_returns_401()
    {
        var client = WithToken(Factory.CreateClient(), "not.a.real-token");

        var response = await client.GetAsync("/api/auth/me", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_is_public()
    {
        var response = await Anonymous.GetAsync("/api/books", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_add_books()
    {
        var (_, _, member) = await RegisterAndSignInAsync();

        var response = await member.PostAsJsonAsync("/api/books", new BookRequest("Book", "Author", ""), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_access_the_blacklist()
    {
        var (_, _, member) = await RegisterAndSignInAsync();

        var response = await member.GetAsync("/api/blacklist", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_only_sees_their_own_data()
    {
        var (me, _, myClient) = await RegisterAndSignInAsync();
        var (other, _, _) = await RegisterAndSignInAsync();

        Assert.Equal(HttpStatusCode.OK, (await myClient.GetAsync($"/api/members/{me.Id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await myClient.GetAsync($"/api/members/{other.Id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await myClient.GetAsync($"/api/members/{other.Id}/loans", Token)).StatusCode);
    }

    [Fact]
    public async Task Admin_sees_any_member()
    {
        var admin = await CreateAdminClientAsync();
        var (member, _, _) = await RegisterAndSignInAsync();

        var response = await admin.GetAsync($"/api/members/{member.Id}", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_borrow_on_behalf_of_someone_else()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);
        var (_, _, myClient) = await RegisterAndSignInAsync();
        var (other, _, _) = await RegisterAndSignInAsync();

        var response = await myClient.PostAsJsonAsync("/api/loans", new CreateLoanRequest(book.Id, other.Id), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_renews_the_session_and_can_be_used_only_once()
    {
        var login = await SignInNewMemberAsync();

        var refresh = await Anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken), Token);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = (await refresh.Content.ReadFromJsonAsync<TokenResponse>(Token))!;

        var client = WithToken(Factory.CreateClient(), refreshed.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me", Token)).StatusCode);

        var reuse = await Anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken), Token);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var afterReuse = await Anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshed.RefreshToken), Token);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_prevents_renewing_the_session()
    {
        var login = await SignInNewMemberAsync();

        var logout = await Anonymous.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest(login.RefreshToken), Token);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await Anonymous.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken), Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_member_from_the_token()
    {
        var (member, _, client) = await RegisterAndSignInAsync();

        var me = await client.GetFromJsonAsync<MemberResponse>("/api/auth/me", Token);

        Assert.Equal(member.Id, me!.Id);
        Assert.Equal("Member", me.Role);
    }

    private async Task<TokenResponse> SignInNewMemberAsync()
    {
        var request = NewMemberRequest();
        var registration = await Anonymous.PostAsJsonAsync("/api/auth/register", request, Token);
        registration.EnsureSuccessStatusCode();

        return await LoginAsync(request.Email, request.Password);
    }
}
