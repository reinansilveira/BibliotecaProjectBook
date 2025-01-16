using BibliotecaProject.Domain.Entities;

namespace BibliotecaProject.Domain.Factory;

public class EmprestimoFactory 
{
    public static Emprestimo NovoEmprestimo( Emprestimo emprestimo, Livro livro , Membro membro)
    {
        
        var emprestimos = new Emprestimo()
        {
            EmprestimoId = Guid.NewGuid(),
            DataEmprestimo = emprestimo.DataEmprestimo,
            DataDevolucao = emprestimo.DataDevolucao,
            LivroId = livro.Id,
            MembroId = membro.IdMembro
            
        };
        
        return emprestimos;
    }
    
}