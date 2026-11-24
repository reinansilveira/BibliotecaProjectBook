using FluentValidation;
using Library.Application.Common.Pagination;

namespace Library.Application.Books;

public sealed class BookSearchRequestValidator : AbstractValidator<BookSearchRequest>
{
    public BookSearchRequestValidator()
    {
        Include(new SearchPageRequestValidator());
        RuleFor(request => request.Genre).MaximumLength(50);
        RuleFor(request => request.Author).MaximumLength(150);
        RuleFor(request => request.Format).IsInEnum();
        RuleFor(request => request.Sort).IsInEnum();
        RuleFor(request => request.YearTo)
            .GreaterThanOrEqualTo(request => request.YearFrom)
            .When(request => request.YearFrom.HasValue && request.YearTo.HasValue)
            .WithMessage("The end year must be on or after the start year.");
    }
}
