using System.Net;
using System.Net.Http.Json;
using Library.Api.Realtime;
using Library.Application.Account;
using Library.Application.Audit;
using Library.Application.Auth;
using Library.Application.Common.Pagination;
using Library.Application.Inbox;
using Library.Application.Members;
using Library.Application.Reports;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Library.IntegrationTests;

public class AccountAndNotificationTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Registration_email_confirms_the_address()
    {
        var (member, _, client) = await RegisterAndSignInAsync();

        var link = await WaitForLinkAsync(member.Email, "Confirm your email");
        var confirmed = await Anonymous.PostAsJsonAsync($"{Api}/auth/confirm-email", new ConfirmEmailRequest(TokenFrom(link)), Token);
        var again = await Anonymous.PostAsJsonAsync($"{Api}/auth/confirm-email", new ConfirmEmailRequest(TokenFrom(link)), Token);

        Assert.StartsWith($"{LibraryApiFactory.FrontEndUrl}/confirm-email?token=", link);
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.True((await client.GetFromJsonAsync<MemberResponse>($"{Api}/auth/me", Token))!.EmailConfirmed);
    }

    [Fact]
    public async Task Password_reset_link_sets_a_new_password_and_ends_old_sessions()
    {
        var request = NewMemberRequest();
        (await Anonymous.PostAsJsonAsync($"{Api}/auth/register", request, Token)).EnsureSuccessStatusCode();
        var session = await LoginAsync(request.Email, request.Password);

        var forgot = await Anonymous.PostAsJsonAsync($"{Api}/auth/forgot-password", new ForgotPasswordRequest(request.Email), Token);
        var unknown = await Anonymous.PostAsJsonAsync($"{Api}/auth/forgot-password", new ForgotPasswordRequest("nobody@tests.com"), Token);
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);

        var link = await WaitForLinkAsync(request.Email, "Reset your password");
        var reset = await Anonymous.PostAsJsonAsync($"{Api}/auth/reset-password", new ResetPasswordRequest(TokenFrom(link), "Brand@New123"), Token);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PostAsJsonAsync($"{Api}/auth/login", new LoginRequest(request.Email, request.Password), Token)).StatusCode);
        Assert.NotNull(await LoginAsync(request.Email, "Brand@New123"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.PostAsJsonAsync($"{Api}/auth/refresh", new RefreshTokenRequest(session.RefreshToken), Token)).StatusCode);
    }

    [Fact]
    public async Task Notifications_arrive_in_real_time_and_stay_in_the_inbox()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);
        var (member, accessToken, client) = await RegisterAndSignInAsync();
        var received = new TaskCompletionSource<NotificationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, NotificationsHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        connection.On<NotificationResponse>(NotificationsHub.NotificationMethod, notification => received.TrySetResult(notification));
        await connection.StartAsync(Token);

        await BorrowOkAsync(client, book.Id, member.Id);

        var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(30), Token);
        Assert.Equal(NotificationTypes.LoanCreated, pushed.Type);

        var unread = await client.GetFromJsonAsync<UnreadCountResponse>($"{Api}/notifications/unread-count", Token);
        Assert.True(unread!.Unread >= 1);

        await client.PostAsync($"{Api}/notifications/read-all", null, Token);
        Assert.Equal(0, (await client.GetFromJsonAsync<UnreadCountResponse>($"{Api}/notifications/unread-count", Token))!.Unread);
    }

    [Fact]
    public async Task Hub_rejects_anonymous_connections()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, NotificationsHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync(Token));
    }

    [Fact]
    public async Task Changes_are_audited_with_the_author()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);

        var audit = await admin.GetFromJsonAsync<PagedResult<AuditEntryResponse>>($"{Api}/audit?entityType=Book&entityId={book.Id}", Token);

        var created = Assert.Single(audit!.Items, entry => entry.Action == "Book.Created");
        Assert.Equal("Administrator", created.ActorName);
        Assert.Contains(book.Title, created.Changes);
    }

    [Fact]
    public async Task Admin_dashboard_and_csv_exports()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);
        var (member, _, client) = await RegisterAndSignInAsync();
        await BorrowOkAsync(client, book.Id, member.Id);

        var dashboard = await admin.GetFromJsonAsync<DashboardResponse>($"{Api}/reports/dashboard", Token);
        var csv = await admin.GetAsync($"{Api}/reports/loans/export", Token);
        var stats = await client.GetFromJsonAsync<ReadingStatsResponse>($"{Api}/members/{member.Id}/reading-stats", Token);

        Assert.True(dashboard!.ActiveLoans >= 1);
        Assert.True(dashboard.LoansToday >= 1);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        Assert.Equal("loans.csv", csv.Content.Headers.ContentDisposition?.FileName);
        Assert.StartsWith("Loan,Book,Author", (await csv.Content.ReadAsStringAsync(Token)).TrimStart('﻿'));
        Assert.Equal(1, stats!.ActiveLoans);
        Assert.Equal("Science Fiction", stats.FavoriteGenre);
        Assert.Equal(12, stats.LastTwelveMonths.Count);
    }

    [Fact]
    public async Task Api_version_is_reported_in_the_response()
    {
        var response = await Anonymous.GetAsync($"{Api}/books", Token);

        Assert.StartsWith("1", response.Headers.GetValues("api-supported-versions").Single());
    }

    private async Task<string> WaitForLinkAsync(string email, string subject)
    {
        var delivered = await Eventually.SatisfiesAsync(
            () => Factory.Emails.Messages.Any(message => message.To == email && message.Subject == subject),
            Token);

        Assert.True(delivered, $"The email '{subject}' was not delivered.");

        var body = Factory.Emails.Messages.Last(message => message.To == email && message.Subject == subject).Body;
        var start = body.IndexOf(LibraryApiFactory.FrontEndUrl, StringComparison.Ordinal);
        var end = body.IndexOfAny([' ', '.'], body.IndexOf("token=", start, StringComparison.Ordinal));

        return end < 0 ? body[start..] : body[start..end];
    }

    private static string TokenFrom(string link) => Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + 6)..]);
}
