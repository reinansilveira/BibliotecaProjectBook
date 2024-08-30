using BibliotecaProject.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Domain.Interfaces
{
    public interface IEmprestimo
    {
        Task<ActionResult<Emprestimo>> Post(Emprestimo emprestimo);

        Task<ActionResult<Emprestimo>> GetById(string id);

        Task<ActionResult<Emprestimo>> Update(string id, Emprestimo input);

        Task<ActionResult> Emprestado(string id);
    }
}
