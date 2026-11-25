using FluentValidation;

namespace Library.Application.Books;

public sealed class BookRequestValidator : AbstractValidator<BookRequest>
{
    public const int MinimumYear = -3000;
    public const int MaximumYear = 2100;

    public BookRequestValidator()
    {
        RuleFor(book => book.Title).NotEmpty().MaximumLength(200);
        RuleFor(book => book.Author).NotEmpty().MaximumLength(150);
        RuleFor(book => book.Description).MaximumLength(2000);
        RuleFor(book => book.Genre).NotEmpty().MaximumLength(50);
        RuleFor(book => book.Isbn)
            .Must(Isbn.IsValid)
            .When(book => !string.IsNullOrWhiteSpace(book.Isbn))
            .WithMessage("The ISBN is not valid. Use 10 or 13 digits with a correct check digit.");
        RuleFor(book => book.PublishedYear).InclusiveBetween(MinimumYear, MaximumYear);
        RuleFor(book => book.CoverUrl)
            .MaximumLength(500)
            .Must(BeAnHttpUrl)
            .When(book => !string.IsNullOrWhiteSpace(book.CoverUrl))
            .WithMessage("The cover must be an absolute http or https address.");
    }

    private static bool BeAnHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
