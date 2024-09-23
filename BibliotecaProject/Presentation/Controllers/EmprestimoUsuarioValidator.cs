using BibliotecaProject.Domain.Entities;
using FluentValidation;

namespace BibliotecaProject.Presentation.Controllers;

public class EmprestimoUsuarioValidator : AbstractValidator<Emprestimo>
{
    public EmprestimoUsuarioValidator()
    {
        RuleFor(user=> user.EmprestimoId).NotEmpty().WithMessage("Emprestimo não é existente");

    }
    
}

   
