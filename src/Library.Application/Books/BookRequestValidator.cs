using FluentValidation;

namespace Library.Application.Books;

public sealed class BookRequestValidator : AbstractValidator<BookRequest>
{
    public BookRequestValidator()
    {
        RuleFor(book => book.Title).NotEmpty().MaximumLength(200);
        RuleFor(book => book.Author).NotEmpty().MaximumLength(150);
        RuleFor(book => book.Description).MaximumLength(1000);
    }
}
