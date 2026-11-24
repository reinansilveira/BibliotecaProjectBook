using FluentValidation;

namespace Library.Application.Members;

public sealed class RegisterMemberRequestValidator : AbstractValidator<RegisterMemberRequest>
{
    public RegisterMemberRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(member => member.Name).NotEmpty().MaximumLength(150);
        RuleFor(member => member.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(member => member.Phone).NotEmpty().MaximumLength(20);
        RuleFor(member => member.BirthDate)
            .LessThan(_ => timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("Birth date must be in the past.");
        RuleFor(member => member.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}
