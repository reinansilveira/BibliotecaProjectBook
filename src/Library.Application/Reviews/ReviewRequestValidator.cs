using FluentValidation;
using Library.Domain.Entities;

namespace Library.Application.Reviews;

public sealed class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(review => review.Rating).InclusiveBetween(Review.MinRating, Review.MaxRating);
        RuleFor(review => review.Comment).MaximumLength(2000);
    }
}
