using System.Net;
using System.Net.Http.Json;
using Library.Application.Books;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reports;
using Library.IntegrationTests.Infrastructure;

namespace Library.IntegrationTests;

public class PaginationAndReportTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Catalog_is_paginated_and_searchable()
    {
        var admin = await CreateAdminClientAsync();
        var prefix = $"Saga-{Guid.NewGuid():N}"[..13];

        foreach (var volume in new[] { "I", "II", "III" })
        {
            var created = await admin.PostAsJsonAsync("/api/books", new BookRequest($"{prefix} Volume {volume}", "Test Author", ""), Token);
            created.EnsureSuccessStatusCode();
        }

        var first = await Anonymous.GetFromJsonAsync<PagedResult<BookResponse>>($"/api/books?search={prefix}&page=1&pageSize=2", Token);
        var second = await Anonymous.GetFromJsonAsync<PagedResult<BookResponse>>($"/api/books?search={prefix}&page=2&pageSize=2", Token);

        Assert.Equal(3, first!.TotalItems);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, first.Items.Count);
        Assert.True(first.HasNextPage);
        Assert.Single(second!.Items);
        Assert.False(second.HasNextPage);
        Assert.True(second.HasPreviousPage);

        var titles = first.Items.Concat(second.Items).Select(book => book.Title).ToList();
        Assert.Equal(titles.Order().ToList(), titles);
        Assert.Equal(3, titles.Distinct().Count());
    }

    [Fact]
    public async Task Page_size_above_the_limit_returns_400()
    {
        var response = await Anonymous.GetAsync("/api/books?pageSize=500", Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Member_list_is_admin_only()
    {
        var (_, _, member) = await RegisterAndSignInAsync();
        var admin = await CreateAdminClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/members", Token)).StatusCode);

        var page = await admin.GetFromJsonAsync<PagedResult<MemberResponse>>($"/api/members?search={LibraryApiFactory.AdminEmail}", Token);
        Assert.Contains(page!.Items, item => item.Email == LibraryApiFactory.AdminEmail);
    }

    [Fact]
    public async Task Most_borrowed_books_ranking_with_dapper()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);

        for (var i = 0; i < 2; i++)
        {
            var (member, _, memberClient) = await RegisterAndSignInAsync();
            var created = await memberClient.PostAsJsonAsync("/api/loans", new CreateLoanRequest(book.Id, member.Id), Token);
            var loan = (await created.Content.ReadFromJsonAsync<LoanResponse>(Token))!;
            await admin.PostAsync($"/api/loans/{loan.Id}/return", null, Token);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ranking = await admin.GetFromJsonAsync<List<MostBorrowedBookResponse>>(
            $"/api/reports/most-borrowed-books?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}&top=50", Token);

        var item = Assert.Single(ranking!, entry => entry.BookId == book.Id);
        Assert.Equal(2, item.TotalLoans);
        Assert.Equal(book.Title, item.Title);
        Assert.True(item.Rank >= 1);
    }

    [Fact]
    public async Task Monthly_overdue_report_with_stored_procedure()
    {
        const int year = 2031;
        var admin = await CreateAdminClientAsync();
        var adminToken = await GetAccessTokenAsync(LibraryApiFactory.AdminEmail, LibraryApiFactory.AdminPassword);
        var onTimeBook = await CreateBookAsync(admin);
        var lateBook = await CreateBookAsync(admin);
        var (punctualMember, punctualToken, _) = await RegisterAndSignInAsync();
        var (lateMember, lateToken, _) = await RegisterAndSignInAsync();

        var punctual = WithToken(Factory.CreateClientAt(new DateTime(year, 3, 1, 12, 0, 0, DateTimeKind.Utc)), punctualToken);
        var onTimeLoan = (await (await punctual.PostAsJsonAsync("/api/loans", new CreateLoanRequest(onTimeBook.Id, punctualMember.Id), Token))
            .Content.ReadFromJsonAsync<LoanResponse>(Token))!;

        var adminOnMarch5 = WithToken(Factory.CreateClientAt(new DateTime(year, 3, 5, 12, 0, 0, DateTimeKind.Utc)), adminToken);
        (await adminOnMarch5.PostAsync($"/api/loans/{onTimeLoan.Id}/return", null, Token)).EnsureSuccessStatusCode();

        var late = WithToken(Factory.CreateClientAt(new DateTime(year, 3, 10, 12, 0, 0, DateTimeKind.Utc)), lateToken);
        (await late.PostAsJsonAsync("/api/loans", new CreateLoanRequest(lateBook.Id, lateMember.Id), Token)).EnsureSuccessStatusCode();

        var adminOnMarch27 = WithToken(Factory.CreateClientAt(new DateTime(year, 3, 27, 12, 0, 0, DateTimeKind.Utc)), adminToken);
        var report = await adminOnMarch27.GetFromJsonAsync<List<MonthlyOverdueResponse>>($"/api/reports/overdue-by-month?year={year}", Token);

        Assert.Equal(12, report!.Count);
        var march = report.Single(month => month.Month == 3);
        Assert.Equal(2, march.TotalLoans);
        Assert.Equal(1, march.OverdueLoans);
        Assert.Equal(50.00m, march.OverduePercentage);
        Assert.Equal(10.00m, march.AverageDaysOverdue);
        Assert.All(report.Where(month => month.Month != 3), month => Assert.Equal(0, month.TotalLoans));
    }

    [Fact]
    public async Task Reports_are_admin_only()
    {
        var (_, _, member) = await RegisterAndSignInAsync();

        var response = await member.GetAsync("/api/reports/overdue-by-month?year=2026", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
