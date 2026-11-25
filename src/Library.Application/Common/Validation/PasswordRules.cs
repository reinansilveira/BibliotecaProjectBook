using FluentValidation;

namespace Library.Application.Common.Validation;

public static class PasswordRules
{
    public const int MinimumLength = 6;
    public const int MaximumLength = 100;

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(MinimumLength).MaximumLength(MaximumLength);
}
