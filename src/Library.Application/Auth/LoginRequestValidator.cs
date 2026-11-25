using FluentValidation;

namespace Library.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(login => login.Email).NotEmpty();
        RuleFor(login => login.Password).NotEmpty();
    }
}
