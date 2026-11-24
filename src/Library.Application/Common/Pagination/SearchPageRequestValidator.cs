using FluentValidation;

namespace Library.Application.Common.Pagination;

public sealed class SearchPageRequestValidator : AbstractValidator<SearchPageRequest>
{
    public SearchPageRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
