using System.Net;
using System.Net.Http.Json;
using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Application.Members;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Library.IntegrationTests;

public class LoanFlowTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_check_is_healthy_with_database_and_broker_connected()
    {
        var response = await Anonymous.GetAsync("/health", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task Full_loan_and_return_flow()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);
        var (member, _, memberClient) = await RegisterAndSignInAsync();
        var (otherMember, _, otherClient) = await RegisterAndSignInAsync();

        var create = await memberClient.PostAsJsonAsync("/api/v1/loans", new CreateLoanRequest(book.Id, member.Id), Token);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var loan = (await create.Content.ReadFromJsonAsync<LoanResponse>(Token))!;
        Assert.Equal(LoanStatus.Active, loan.Status);

        var duplicate = await otherClient.PostAsJsonAsync("/api/v1/loans", new CreateLoanRequest(book.Id, otherMember.Id), Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>(Token);
        Assert.Equal("No copies are available right now. Join the waiting list.", problem!.Detail);

        var returnByMember = await memberClient.PostAsync($"/api/v1/loans/{loan.Id}/return", null, Token);
        Assert.Equal(HttpStatusCode.Forbidden, returnByMember.StatusCode);

        var returned = await admin.PostAsync($"/api/v1/loans/{loan.Id}/return", null, Token);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal(LoanStatus.Returned, (await returned.Content.ReadFromJsonAsync<LoanResponse>(Token))!.Status);

        var history = await memberClient.GetFromJsonAsync<PagedResult<LoanResponse>>($"/api/v1/members/{member.Id}/loans", Token);
        Assert.Single(history!.Items);
    }

    [Fact]
    public async Task Invalid_data_returns_400_with_errors_per_field()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/v1/auth/register", new RegisterMemberRequest("", new DateTime(1990, 1, 1), "", "not-an-email", "123"), Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Token);
        Assert.Contains("Email", problem!.Errors.Keys);
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Duplicate_email_returns_409()
    {
        var (member, _, _) = await RegisterAndSignInAsync();
        var request = NewMemberRequest() with { Email = member.Email.ToUpperInvariant() };

        var response = await Anonymous.PostAsJsonAsync("/api/v1/auth/register", request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_book_returns_404_as_problem_details()
    {
        var response = await Anonymous.GetAsync($"/api/v1/books/{Guid.NewGuid()}", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Token);
        Assert.Equal("Book not found.", problem!.Detail);
    }

    [Fact]
    public async Task Password_is_never_returned()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/v1/auth/register", NewMemberRequest(), Token);
        var json = await response.Content.ReadAsStringAsync(Token);

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }
}
