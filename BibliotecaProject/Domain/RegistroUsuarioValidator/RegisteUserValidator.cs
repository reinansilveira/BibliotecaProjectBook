using System.Data;
using BibliotecaProject.Domain.Entities;
using FluentValidation;

namespace BibliotecaProject.Domain.RegistroUsuarioValidator;

public class RegisteUserValidator : AbstractValidator<Membro>
{
    public RegisteUserValidator()
    {
        RuleFor(user => user.Nome).NotEmpty();
        RuleFor(user => user.Email).NotEmpty();
        RuleFor(user => user.Email).EmailAddress();
        RuleFor(user => user.Telefone).NotEmpty();
        RuleFor(user => user.Senha).NotEmpty();
        RuleFor(user => user.Senha).MinimumLength(6);
    }
}