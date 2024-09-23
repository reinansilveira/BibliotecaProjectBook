using BibliotecaProject.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Domain.Interfaces
{
    public interface IMembro
    {
        Task<ActionResult<Membro>> Post(Membro membro);

        Task<ActionResult<Membro>> GetById(string id);

        Task<ActionResult<Membro>> Update(string id, Membro input);
        Task<ActionResult> Delete(string id);
    }
}
