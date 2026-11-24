using System.Net;
using System.Text;
using Library.Infrastructure.Caching;
using Library.Infrastructure.External;
using Library.Infrastructure.Search;
using Library.Infrastructure.Security;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Library.UnitTests.Infrastructure;

public class CatalogInfrastructureTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("dune", "\"dune*\"")]
    [InlineData("  jane   eyre! ", "\"jane*\" AND \"eyre*\"")]
    [InlineData("Dom \"Casmurro\" OR", "\"Dom*\" AND \"Casmurro*\" AND \"OR*\"")]
    public void Full_text_condition_uses_prefix_terms_without_user_operators(string term, string expected)
    {
        Assert.Equal(expected, FullTextSearch.BuildPrefixCondition(term));
    }

    [Fact]
    public void Full_text_condition_is_null_without_words()
    {
        Assert.Null(FullTextSearch.BuildPrefixCondition("!!! ***"));
    }

    [Fact]
    public void Ebook_link_signature_is_bound_to_the_loan_and_the_expiry()
    {
        var signer = new EbookLinkSigner(Options.Create(new JwtSettings { Key = new string('k', 40) }));
        var loanId = Guid.NewGuid();
        var signature = signer.Sign(loanId, 1000);

        Assert.True(signer.Verify(loanId, 1000, signature));
        Assert.False(signer.Verify(loanId, 1001, signature));
        Assert.False(signer.Verify(Guid.NewGuid(), 1000, signature));
        Assert.False(signer.Verify(loanId, 1000, signature + "x"));
    }

    [Theory]
    [InlineData("June 1997", 1997)]
    [InlineData("1813", 1813)]
    [InlineData("unknown", null)]
    [InlineData(null, null)]
    public void Publication_year_is_read_from_free_text(string? text, int? expected)
    {
        Assert.Equal(expected, OpenLibraryMetadataProvider.ParseYear(text));
    }

    [Theory]
    [InlineData("SCIENCE FICTION, AMERICAN", "Science Fiction")]
    [InlineData("", "General")]
    [InlineData(null, "General")]
    public void Subject_becomes_a_short_genre(string? subject, string expected)
    {
        Assert.Equal(expected, OpenLibraryMetadataProvider.ToGenre(subject));
    }

    [Fact]
    public async Task Open_library_answer_is_mapped_to_metadata()
    {
        var handler = new StubHandler(path => path switch
        {
            "/api/books" => """{"ISBN:9780441013593":{"title":"Dune","authors":[{"name":"Frank Herbert"}],"publish_date":"1990","subjects":[{"name":"Science fiction"}],"cover":{"large":"https://covers/dune-L.jpg"}}}""",
            "/isbn/9780441013593.json" => """{"works":[{"key":"/works/OL893415W"}]}""",
            "/works/OL893415W.json" => """{"description":{"value":"A desert planet."}}""",
            _ => null
        });
        var provider = new OpenLibraryMetadataProvider(
            new HttpClient(handler) { BaseAddress = new Uri("https://openlibrary.org/") },
            NullLogger<OpenLibraryMetadataProvider>.Instance);

        var metadata = await provider.FindByIsbnAsync("9780441013593", Token);

        Assert.NotNull(metadata);
        Assert.Equal("Dune", metadata.Title);
        Assert.Equal("Frank Herbert", metadata.Author);
        Assert.Equal("A desert planet.", metadata.Description);
        Assert.Equal("Science Fiction", metadata.Genre);
        Assert.Equal(1990, metadata.PublishedYear);
        Assert.Equal("https://covers/dune-L.jpg", metadata.CoverUrl);
    }

    [Fact]
    public async Task Unknown_isbn_has_no_metadata()
    {
        var provider = new OpenLibraryMetadataProvider(
            new HttpClient(new StubHandler(_ => "{}")) { BaseAddress = new Uri("https://openlibrary.org/") },
            NullLogger<OpenLibraryMetadataProvider>.Instance);

        Assert.Null(await provider.FindByIsbnAsync("9780441013593", Token));
    }

    [Fact]
    public async Task Catalog_cache_serves_cached_values_until_invalidated()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        await using var provider = services.BuildServiceProvider();
        var cache = new HybridCatalogCache(provider.GetRequiredService<HybridCache>(), Options.Create(new CachingSettings()));
        var calls = 0;

        Task<int> Factory(CancellationToken _) => Task.FromResult(++calls);

        Assert.Equal(1, await cache.GetOrCreateAsync("key", Factory, Token));
        Assert.Equal(1, await cache.GetOrCreateAsync("key", Factory, Token));

        await cache.InvalidateAsync(Token);

        Assert.Equal(2, await cache.GetOrCreateAsync("key", Factory, Token));
    }

    [Fact]
    public async Task Disabled_cache_always_calls_the_factory()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        await using var provider = services.BuildServiceProvider();
        var cache = new HybridCatalogCache(provider.GetRequiredService<HybridCache>(), Options.Create(new CachingSettings { Enabled = false }));
        var calls = 0;

        await cache.GetOrCreateAsync("key", _ => Task.FromResult(++calls), Token);
        await cache.GetOrCreateAsync("key", _ => Task.FromResult(++calls), Token);

        Assert.Equal(2, calls);
    }

    private sealed class StubHandler(Func<string, string?> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = respond(request.RequestUri!.AbsolutePath);

            if (body is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
