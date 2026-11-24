using FluentValidation;

namespace Library.Application.Books.Inventory;

public sealed class AddCopiesRequestValidator : AbstractValidator<AddCopiesRequest>
{
    public const int MaxCopiesPerRequest = 50;

    public AddCopiesRequestValidator()
    {
        RuleFor(request => request.Quantity).InclusiveBetween(1, MaxCopiesPerRequest);
    }
}
