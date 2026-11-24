using Library.Domain.Exceptions;
using Library.UnitTests.Fakes;

namespace Library.UnitTests.Domain;

public class BookTests
{
    [Fact]
    public void Book_has_an_ebook_only_with_a_file_and_licenses()
    {
        var book = TestData.Book();

        Assert.False(book.HasEbook);

        book.AttachEbook("ebooks/file.epub", 3);

        Assert.True(book.HasEbook);
        Assert.Equal(3, book.EbookLicenses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Licenses_must_be_between_1_and_100(int licenses)
    {
        Assert.Throws<DomainException>(() => TestData.Book().AttachEbook("ebooks/file.epub", licenses));
    }

    [Fact]
    public void Licenses_cannot_change_without_an_ebook()
    {
        Assert.Throws<DomainException>(() => TestData.Book().ChangeEbookLicenses(2));
    }

    [Fact]
    public void Update_replaces_the_details()
    {
        var book = TestData.Book();
        var details = TestData.BookDetails() with { Isbn = "9780141439518", PublishedYear = 1813 };

        book.Update(details);

        Assert.Equal(details.Title, book.Title);
        Assert.Equal("9780141439518", book.Isbn);
        Assert.Equal(1813, book.PublishedYear);
    }
}
