using System.Net;
using System.Net.Http.Json;
using Library.Application.Books;
using Library.Application.Books.Import;
using Library.Application.Books.Inventory;
using Library.Application.Common.Pagination;
using Library.Application.Reviews;
using Library.IntegrationTests.Infrastructure;

namespace Library.IntegrationTests;

public class CatalogTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Full_text_search_finds_words_by_prefix_in_title_author_and_description()
    {
        var admin = await CreateAdminClientAsync();
        var word = $"Zyx{Guid.NewGuid():N}"[..12];
        var request = new CreateBookRequest { Title = "Quiet Harbor", Author = "Ada Moreno", Description = $"A story about {word}lighthouses", Genre = "Drama" };
        var created = await (await admin.PostAsJsonAsync($"{Api}/books", request, Token)).Content.ReadFromJsonAsync<BookResponse>(Token);

        var found = await Eventually.SatisfiesAsync(async () =>
        {
            var page = await Anonymous.GetFromJsonAsync<PagedResult<BookResponse>>($"{Api}/books?search={word}", Token);

            return page!.Items.Any(book => book.Id == created!.Id);
        }, Token);

        Assert.True(found, "The full-text index did not find the book by a word prefix in the description.");
    }

    [Fact]
    public async Task Filters_and_sorting_work_together()
    {
        var admin = await CreateAdminClientAsync();
        var genre = $"Genre{Guid.NewGuid():N}"[..14];
        var old = await CreateBookAsync(admin, copies: 1, genre: genre);
        var available = await CreateBookAsync(admin, copies: 2, genre: genre);
        var unavailable = await CreateBookAsync(admin, copies: 1, genre: genre);
        var (member, _, client) = await RegisterAndSignInAsync();
        await BorrowOkAsync(client, unavailable.Id, member.Id);
        await admin.PutAsJsonAsync($"{Api}/books/{old.Id}", new BookRequest { Title = old.Title, Author = old.Author, Genre = genre, PublishedYear = 1900 }, Token);

        var onlyAvailable = await Anonymous.GetFromJsonAsync<PagedResult<BookResponse>>($"{Api}/books?genre={genre}&availableOnly=true", Token);
        var byYear = await Anonymous.GetFromJsonAsync<PagedResult<BookResponse>>($"{Api}/books?genre={genre}&yearTo=1950", Token);
        var genres = await Anonymous.GetFromJsonAsync<List<GenreResponse>>($"{Api}/books/genres", Token);

        Assert.Equal(2, onlyAvailable!.TotalItems);
        Assert.DoesNotContain(onlyAvailable.Items, book => book.Id == unavailable.Id);
        Assert.Equal(old.Id, Assert.Single(byYear!.Items).Id);
        Assert.Equal(3, genres!.Single(item => item.Name == genre).Books);
        Assert.Contains(onlyAvailable.Items, book => book.Id == available.Id && book.AvailableCopies == 2);
    }

    [Fact]
    public async Task Suggestions_match_the_start_of_titles_and_authors()
    {
        var admin = await CreateAdminClientAsync();
        var title = $"Qwerty{Guid.NewGuid():N}"[..14];
        await CreateBookAsync(admin, title: title);

        var suggestions = await Anonymous.GetFromJsonAsync<List<BookSuggestion>>($"{Api}/books/suggestions?query={title[..8]}", Token);

        Assert.Contains(suggestions!, suggestion => suggestion.Title == title);
    }

    [Fact]
    public async Task Isbn_import_fills_the_book_from_open_library()
    {
        Factory.Metadata.Add(new BookMetadata("9780441013593", "Dune", "Frank Herbert", "A desert planet.", "Science Fiction", 1965, "https://covers.openlibrary.org/b/isbn/9780441013593-L.jpg"));
        var admin = await CreateAdminClientAsync();

        var lookup = await admin.GetFromJsonAsync<BookMetadata>($"{Api}/books/lookup?isbn=978-0-441-01359-3", Token);
        var imported = await admin.PostAsJsonAsync($"{Api}/books/import", new ImportBookRequest("9780441013593", 2), Token);
        var duplicate = await admin.PostAsJsonAsync($"{Api}/books/import", new ImportBookRequest("9780441013593", 1), Token);

        Assert.Equal("Dune", lookup!.Title);
        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        var book = (await imported.Content.ReadFromJsonAsync<BookResponse>(Token))!;
        Assert.Equal("9780441013593", book.Isbn);
        Assert.Equal(2, book.TotalCopies);
        Assert.NotNull(book.CoverUrl);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Api}/books/lookup?isbn=9780140449136", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"{Api}/books/lookup?isbn=123", Token)).StatusCode);
    }

    [Fact]
    public async Task Copies_can_be_added_and_withdrawn()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 1);

        var copies = await (await admin.PostAsJsonAsync($"{Api}/books/{book.Id}/copies", new AddCopiesRequest(2), Token))
            .Content.ReadFromJsonAsync<List<BookCopyResponse>>(Token);
        var withdrawn = await admin.DeleteAsync($"{Api}/books/{book.Id}/copies/{copies![0].Id}", Token);

        Assert.Equal(3, copies.Count);
        Assert.Equal(HttpStatusCode.NoContent, withdrawn.StatusCode);
        Assert.Equal(2, (await Anonymous.GetFromJsonAsync<BookResponse>($"{Api}/books/{book.Id}", Token))!.TotalCopies);
    }

    [Fact]
    public async Task Readers_review_books_and_the_average_is_updated()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 2);
        var (first, _, firstClient) = await RegisterAndSignInAsync("Maria Clara Souza");
        var (second, _, secondClient) = await RegisterAndSignInAsync();
        var (_, _, outsider) = await RegisterAndSignInAsync();
        await BorrowOkAsync(firstClient, book.Id, first.Id);
        await BorrowOkAsync(secondClient, book.Id, second.Id);

        Assert.Equal(HttpStatusCode.OK, (await firstClient.PutAsJsonAsync($"{Api}/books/{book.Id}/reviews/mine", new ReviewRequest(5, "Loved it"), Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await secondClient.PutAsJsonAsync($"{Api}/books/{book.Id}/reviews/mine", new ReviewRequest(4), Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await outsider.PutAsJsonAsync($"{Api}/books/{book.Id}/reviews/mine", new ReviewRequest(1), Token)).StatusCode);

        var rated = await Anonymous.GetFromJsonAsync<BookResponse>($"{Api}/books/{book.Id}", Token);
        var reviews = await Anonymous.GetFromJsonAsync<PagedResult<ReviewResponse>>($"{Api}/books/{book.Id}/reviews", Token);

        Assert.Equal(4.50m, rated!.AverageRating);
        Assert.Equal(2, rated.RatingsCount);
        Assert.Contains(reviews!.Items, review => review.ReviewerName == "Maria S.");

        await firstClient.DeleteAsync($"{Api}/books/{book.Id}/reviews/mine", Token);
        Assert.Equal(4.00m, (await Anonymous.GetFromJsonAsync<BookResponse>($"{Api}/books/{book.Id}", Token))!.AverageRating);
    }

    [Fact]
    public async Task Members_who_read_a_book_also_read_others_and_keep_a_wishlist()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin, copies: 2);
        var related = await CreateBookAsync(admin, copies: 2);
        var wished = await CreateBookAsync(admin, copies: 1);
        var (first, _, firstClient) = await RegisterAndSignInAsync();
        var (second, _, secondClient) = await RegisterAndSignInAsync();
        await BorrowOkAsync(firstClient, book.Id, first.Id);
        await BorrowOkAsync(firstClient, related.Id, first.Id);
        await BorrowOkAsync(secondClient, book.Id, second.Id);
        await BorrowOkAsync(secondClient, related.Id, second.Id);

        var alsoRead = await Anonymous.GetFromJsonAsync<List<BookResponse>>($"{Api}/books/{book.Id}/also-read", Token);
        Assert.Equal(related.Id, alsoRead![0].Id);

        Assert.Equal(HttpStatusCode.NoContent, (await firstClient.PutAsync($"{Api}/members/{first.Id}/wishlist/{wished.Id}", null, Token)).StatusCode);
        await firstClient.PutAsync($"{Api}/members/{first.Id}/wishlist/{wished.Id}", null, Token);
        var wishlist = await firstClient.GetFromJsonAsync<List<BookResponse>>($"{Api}/members/{first.Id}/wishlist", Token);
        Assert.Equal(wished.Id, Assert.Single(wishlist!).Id);

        await firstClient.DeleteAsync($"{Api}/members/{first.Id}/wishlist/{wished.Id}", Token);
        Assert.Empty((await firstClient.GetFromJsonAsync<List<BookResponse>>($"{Api}/members/{first.Id}/wishlist", Token))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await secondClient.GetAsync($"{Api}/members/{first.Id}/wishlist", Token)).StatusCode);
    }
}
