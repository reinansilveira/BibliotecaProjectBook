using FluentValidation;

namespace Library.Application.Reports;

public sealed class MostBorrowedBooksRequestValidator : AbstractValidator<MostBorrowedBooksRequest>
{
    public MostBorrowedBooksRequestValidator()
    {
        RuleFor(request => request.Top).InclusiveBetween(1, 50);
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .When(request => request.From.HasValue && request.To.HasValue)
            .WithMessage("The end date must be on or after the start date.");
    }
}
