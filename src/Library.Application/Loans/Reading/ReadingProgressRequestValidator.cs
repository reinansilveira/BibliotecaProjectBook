using FluentValidation;

namespace Library.Application.Loans.Reading;

public sealed class ReadingProgressRequestValidator : AbstractValidator<ReadingProgressRequest>
{
    public ReadingProgressRequestValidator()
    {
        RuleFor(request => request.Location).NotEmpty().MaximumLength(500);
        RuleFor(request => request.Percentage).InclusiveBetween(0, 100);
    }
}
