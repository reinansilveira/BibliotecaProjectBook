using Library.Application.Account;
using Library.Application.Books;
using Library.Application.Members;
using Library.Application.Reports;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Library.UnitTests.Application;

public class ValidatorTests
{
    private readonly RegisterMemberRequestValidator _memberValidator =
        new(new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Valid_member_passes()
    {
        var result = _memberValidator.Validate(TestData.RegisterMemberRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "ana@email.com", "secret123", "Name")]
    [InlineData("Ana", "not-an-email", "secret123", "Email")]
    [InlineData("Ana", "ana@email.com", "123", "Password")]
    public void Invalid_member_points_to_the_field(string name, string email, string password, string field)
    {
        var request = new RegisterMemberRequest(name, new DateTime(1990, 1, 1), "555-0100", email, password);

        var result = _memberValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == field);
    }

    [Fact]
    public void Birth_date_in_the_future_is_invalid()
    {
        var request = TestData.RegisterMemberRequest() with { BirthDate = new DateTime(2030, 1, 1) };

        var result = _memberValidator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterMemberRequest.BirthDate));
    }

    [Fact]
    public void Book_without_title_author_or_genre_is_invalid()
    {
        var result = new BookRequestValidator().Validate(new BookRequest { Description = "Description" });

        Assert.Equal(3, result.Errors.Count);
    }

    [Theory]
    [InlineData("9780141439519", "Isbn")]
    [InlineData("ftp://covers/book.jpg", "CoverUrl")]
    public void Book_with_a_bad_isbn_or_cover_is_invalid(string value, string field)
    {
        var request = new BookRequest
        {
            Title = "Emma",
            Author = "Jane Austen",
            Genre = "Romance",
            Isbn = field == "Isbn" ? value : null,
            CoverUrl = field == "CoverUrl" ? value : null
        };

        var result = new BookRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == field);
    }

    [Fact]
    public void New_book_copies_are_limited()
    {
        var result = new CreateBookRequestValidator().Validate(new CreateBookRequest { Title = "Emma", Author = "Austen", Genre = "Romance", Copies = 51 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookRequest.Copies));
    }

    [Fact]
    public void Search_with_years_out_of_order_is_invalid()
    {
        var result = new BookSearchRequestValidator().Validate(new BookSearchRequest { YearFrom = 1900, YearTo = 1800 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void New_password_must_differ_from_the_current_one()
    {
        var result = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest("Same@123", "Same@123"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Report_period_ending_before_it_starts_is_invalid()
    {
        var request = new MostBorrowedBooksRequest { From = new DateOnly(2026, 5, 10), To = new DateOnly(2026, 5, 1) };

        var result = new MostBorrowedBooksRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }
}
