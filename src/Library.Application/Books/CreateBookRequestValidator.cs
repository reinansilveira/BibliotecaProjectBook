using FluentValidation;
using Library.Application.Books.Inventory;

namespace Library.Application.Books;

public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        Include(new BookRequestValidator());
        RuleFor(book => book.Copies).InclusiveBetween(0, AddCopiesRequestValidator.MaxCopiesPerRequest);
    }
}
