using System.Net;
using System.Net.Http.Json;
using Library.Application.Fines;
using Library.Application.Loans;
using Library.Application.Maintenance;
using Library.Application.Reservations;
using Library.Domain.Enums;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Library.IntegrationTests;

[Collection(SharedClockCollection.Name)]
public class CirculationTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Copies_define_availability()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 2);
        var (first, _, firstClient) = await RegisterAndSignInAsync();
        var (second, _, secondClient) = await RegisterAndSignInAsync();
        var (third, _, thirdClient) = await RegisterAndSignInAsync();

        await BorrowOkAsync(firstClient, book.Id, first.Id);
        await BorrowOkAsync(secondClient, book.Id, second.Id);
        var blocked = await BorrowAsync(thirdClient, book.Id, third.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);
        var view = await Anonymous.GetFromJsonAsync<Application.Books.BookResponse>($"{Api}/books/{book.Id}", Token);
        Assert.Equal(2, view!.TotalCopies);
        Assert.Equal(0, view.AvailableCopies);
    }

    [Fact]
    public async Task Two_members_racing_for_the_last_copy_get_one_loan()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var (first, _, firstClient) = await RegisterAndSignInAsync();
        var (second, _, secondClient) = await RegisterAndSignInAsync();

        var results = await Task.WhenAll(BorrowAsync(firstClient, book.Id, first.Id), BorrowAsync(secondClient, book.Id, second.Id));

        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Waiting_list_late_fine_and_hold_flow()
    {
        var admin = await CreateAdminClientAsync();
        var adminToken = await GetAdminTokenAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var otherBook = await CreateBookAsync(admin, copies: 1);
        var (borrower, borrowerToken, borrowerClient) = await RegisterAndSignInAsync();
        var (next, nextToken, nextClient) = await RegisterAndSignInAsync();
        var (last, _, lastClient) = await RegisterAndSignInAsync();

        var loan = await BorrowOkAsync(borrowerClient, book.Id, borrower.Id);

        var nextReservation = await ReserveAsync(nextClient, book.Id, next.Id);
        var lastReservation = await ReserveAsync(lastClient, book.Id, last.Id);
        Assert.Equal(1, nextReservation.Position);
        Assert.Equal(2, lastReservation.Position);

        var duplicate = await nextClient.PostAsJsonAsync($"{Api}/reservations", new ReservationRequest(book.Id, next.Id), Token);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var adminLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromDays(10) - TimeSpan.FromHours(1)), adminToken);
        var returned = await adminLater.PostAsync($"{Api}/loans/{loan.Id}/return", null, Token);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        var fines = await borrowerClient.GetFromJsonAsync<Application.Common.Pagination.PagedResult<FineResponse>>($"{Api}/members/{borrower.Id}/fines", Token);
        var fine = Assert.Single(fines!.Items);
        Assert.Equal(3, fine.DaysOverdue);
        Assert.Equal(1.50m, fine.Amount);

        var borrowerLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromDays(10)), borrowerToken);
        var standing = await borrowerLater.GetFromJsonAsync<StandingResponse>($"{Api}/members/{borrower.Id}/standing", Token);
        Assert.False(standing!.CanBorrow);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await BorrowAsync(borrowerLater, otherBook.Id, borrower.Id)).StatusCode);

        var paid = await borrowerLater.PostAsync($"{Api}/fines/{fine.Id}/pay", null, Token);
        Assert.Equal(nameof(FineStatus.Paid), (await paid.Content.ReadFromJsonAsync<FineResponse>(Token))!.Status);
        Assert.True((await borrowerLater.GetFromJsonAsync<StandingResponse>($"{Api}/members/{borrower.Id}/standing", Token))!.CanBorrow);

        var held = await nextClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{nextReservation.Id}", Token);
        Assert.Equal(nameof(ReservationStatus.Ready), held!.Status);
        var stillWaiting = await lastClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{lastReservation.Id}", Token);
        Assert.Equal(1, stillWaiting!.Position);

        var lastLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromDays(10)), (await GetMemberTokenAsync(lastClient)));
        var notForLast = await BorrowAsync(lastLater, book.Id, last.Id);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notForLast.StatusCode);

        var nextLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromDays(10)), nextToken);
        Assert.Equal(HttpStatusCode.Created, (await BorrowAsync(nextLater, book.Id, next.Id)).StatusCode);
        var fulfilled = await nextClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{nextReservation.Id}", Token);
        Assert.Equal(nameof(ReservationStatus.Fulfilled), fulfilled!.Status);
    }

    [Fact]
    public async Task Hold_not_picked_up_goes_to_the_next_member()
    {
        var admin = await CreateAdminClientAsync();
        var adminToken = await GetAdminTokenAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var (borrower, _, borrowerClient) = await RegisterAndSignInAsync();
        var (first, _, firstClient) = await RegisterAndSignInAsync();
        var (second, _, secondClient) = await RegisterAndSignInAsync();

        var loan = await BorrowOkAsync(borrowerClient, book.Id, borrower.Id);
        var firstReservation = await ReserveAsync(firstClient, book.Id, first.Id);
        var secondReservation = await ReserveAsync(secondClient, book.Id, second.Id);
        (await admin.PostAsync($"{Api}/loans/{loan.Id}/return", null, Token)).EnsureSuccessStatusCode();

        var adminLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromHours(49)), adminToken);
        var result = await (await adminLater.PostAsync($"{Api}/maintenance/run", null, Token)).Content.ReadFromJsonAsync<MaintenanceResult>(Token);

        Assert.True(result!.ExpiredHolds >= 1);
        Assert.Equal(nameof(ReservationStatus.Expired), (await firstClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{firstReservation.Id}", Token))!.Status);
        Assert.Equal(nameof(ReservationStatus.Ready), (await secondClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{secondReservation.Id}", Token))!.Status);
    }

    [Fact]
    public async Task Loan_can_be_renewed_once_when_nobody_is_waiting()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var (member, _, client) = await RegisterAndSignInAsync();
        var loan = await BorrowOkAsync(client, book.Id, member.Id);

        var renewed = await client.PostAsync($"{Api}/loans/{loan.Id}/renew", null, Token);
        var again = await client.PostAsync($"{Api}/loans/{loan.Id}/renew", null, Token);

        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.Equal(loan.DueDate.AddDays(7), (await renewed.Content.ReadFromJsonAsync<LoanResponse>(Token))!.DueDate);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    [Fact]
    public async Task Renewal_is_refused_when_someone_is_waiting()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var (member, _, client) = await RegisterAndSignInAsync();
        var (waiting, _, waitingClient) = await RegisterAndSignInAsync();
        var loan = await BorrowOkAsync(client, book.Id, member.Id);
        await ReserveAsync(waitingClient, book.Id, waiting.Id);

        var renewed = await client.PostAsync($"{Api}/loans/{loan.Id}/renew", null, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, renewed.StatusCode);
        Assert.Contains("waiting", (await renewed.Content.ReadFromJsonAsync<ProblemDetails>(Token))!.Detail);
    }

    [Fact]
    public async Task Members_have_at_most_three_active_loans()
    {
        var admin = await CreateAdminClientAsync();
        var (member, _, client) = await RegisterAndSignInAsync();

        for (var i = 0; i < LoanPolicy.MaxActiveLoans; i++)
        {
            await BorrowOkAsync(client, (await CreateBookAsync(admin)).Id, member.Id);
        }

        var fourth = await BorrowAsync(client, (await CreateBookAsync(admin)).Id, member.Id);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fourth.StatusCode);
    }

    [Fact]
    public async Task Available_book_cannot_be_reserved()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);
        var (member, _, client) = await RegisterAndSignInAsync();

        var response = await client.PostAsJsonAsync($"{Api}/reservations", new ReservationRequest(book.Id, member.Id), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<ReservationResponse> ReserveAsync(HttpClient client, Guid bookId, Guid memberId)
    {
        var response = await client.PostAsJsonAsync($"{Api}/reservations", new ReservationRequest(bookId, memberId), Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<ReservationResponse>(Token))!;
    }

    private static Task<string> GetMemberTokenAsync(HttpClient client) =>
        Task.FromResult(client.DefaultRequestHeaders.Authorization!.Parameter!);
}
