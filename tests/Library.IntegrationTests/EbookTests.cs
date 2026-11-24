using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Library.Api.Contracts;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Loans;
using Library.Application.Loans.Reading;
using Library.Application.Reservations;
using Library.Domain.Enums;
using Library.IntegrationTests.Infrastructure;

namespace Library.IntegrationTests;

[Collection(SharedClockCollection.Name)]
public class EbookTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Ebook_loan_reading_and_early_return_flow()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 0);
        var content = TestFiles.Epub("chapter one");
        var withEbook = await UploadAsync(admin, book.Id, content, licenses: 1);
        Assert.True(withEbook.HasEbook);
        Assert.Equal(1, withEbook.AvailableEbookLicenses);

        var (reader, _, readerClient) = await RegisterAndSignInAsync();
        var (waiting, _, waitingClient) = await RegisterAndSignInAsync();
        var loan = await BorrowOkAsync(readerClient, book.Id, reader.Id, BookFormat.Ebook);
        Assert.Equal(loan.LoanDate.AddDays(14), loan.DueDate);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await BorrowAsync(waitingClient, book.Id, waiting.Id, BookFormat.Ebook)).StatusCode);
        var reservation = await (await waitingClient.PostAsJsonAsync($"{Api}/reservations", new ReservationRequest(book.Id, waiting.Id, BookFormat.Ebook), Token))
            .Content.ReadFromJsonAsync<ReservationResponse>(Token);

        var link = await (await readerClient.PostAsync($"{Api}/loans/{loan.Id}/reading-link", null, Token)).Content.ReadFromJsonAsync<EbookLinkResponse>(Token);
        Assert.Equal("epub", link!.FileFormat);

        var download = await Anonymous.GetAsync(link.Url, Token);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(EbookFiles.EpubContentType, download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(content, await download.Content.ReadAsByteArrayAsync(Token));

        var partial = new HttpRequestMessage(HttpMethod.Get, link.Url);
        partial.Headers.Range = new RangeHeaderValue(0, 3);
        Assert.Equal(HttpStatusCode.PartialContent, (await Anonymous.SendAsync(partial, Token)).StatusCode);

        var tampered = link.Url.Replace("signature=", "signature=x", StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync(tampered, Token)).StatusCode);

        var progress = await readerClient.PutAsJsonAsync($"{Api}/loans/{loan.Id}/progress", new ReadingProgressRequest("epubcfi(/6/8)", 42.5m), Token);
        Assert.Equal(42.5m, (await progress.Content.ReadFromJsonAsync<LoanResponse>(Token))!.ReadingProgress);

        var returned = await readerClient.PostAsync($"{Api}/loans/{loan.Id}/return", null, Token);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous.GetAsync(link.Url, Token)).StatusCode);

        var ready = await waitingClient.GetFromJsonAsync<ReservationResponse>($"{Api}/reservations/{reservation!.Id}", Token);
        Assert.Equal(nameof(ReservationStatus.Ready), ready!.Status);
    }

    [Fact]
    public async Task Only_the_reader_gets_a_reading_link()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 0);
        await UploadAsync(admin, book.Id, TestFiles.Epub("x"), licenses: 2);
        var (reader, _, readerClient) = await RegisterAndSignInAsync();
        var (_, _, otherClient) = await RegisterAndSignInAsync();
        var loan = await BorrowOkAsync(readerClient, book.Id, reader.Id, BookFormat.Ebook);

        Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.PostAsync($"{Api}/loans/{loan.Id}/reading-link", null, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync($"{Api}/loans/{loan.Id}/reading-link", null, Token)).StatusCode);
    }

    [Fact]
    public async Task Expired_ebook_loan_frees_the_license()
    {
        var admin = await CreateAdminClientAsync();
        var adminToken = await GetAdminTokenAsync();
        var book = await CreateBookAsync(admin, copies: 0);
        await UploadAsync(admin, book.Id, TestFiles.Epub("x"), licenses: 1);
        var (reader, _, readerClient) = await RegisterAndSignInAsync();
        var loan = await BorrowOkAsync(readerClient, book.Id, reader.Id, BookFormat.Ebook);

        var adminLater = WithToken(Factory.CreateClientWithClockOffset(TimeSpan.FromDays(15)), adminToken);
        (await adminLater.PostAsync($"{Api}/maintenance/run", null, Token)).EnsureSuccessStatusCode();

        var expired = await readerClient.GetFromJsonAsync<LoanResponse>($"{Api}/loans/{loan.Id}", Token);
        Assert.Equal(LoanStatus.Expired, expired!.Status);
        Assert.Equal(1, (await Anonymous.GetFromJsonAsync<BookResponse>($"{Api}/books/{book.Id}", Token))!.AvailableEbookLicenses);
    }

    [Fact]
    public async Task Gutenberg_import_attaches_a_public_domain_epub()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);

        var response = await admin.PostAsJsonAsync($"{Api}/books/{book.Id}/ebook/gutenberg", new GutenbergImportRequest(84, 3), Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<BookResponse>(Token))!;
        Assert.True(updated.HasEbook);
        Assert.Equal(3, updated.EbookLicenses);
    }

    [Fact]
    public async Task Fake_ebook_file_is_rejected()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 0);
        using var form = Form([0x4D, 0x5A, 0x90], "book.epub", 1);

        var response = await admin.PostAsync($"{Api}/books/{book.Id}/ebook", form, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task<BookResponse> UploadAsync(HttpClient admin, Guid bookId, byte[] content, int licenses)
    {
        using var form = Form(content, "book.epub", licenses);
        var response = await admin.PostAsync($"{Api}/books/{bookId}/ebook", form, Token);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BookResponse>(Token))!;
    }

    private static MultipartFormDataContent Form(byte[] content, string fileName, int licenses)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/epub+zip");

        return new MultipartFormDataContent
        {
            { file, "File", fileName },
            { new StringContent(licenses.ToString()), "Licenses" }
        };
    }
}
