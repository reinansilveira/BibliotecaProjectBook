using BibliotecaProject.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Interfaces
{
    public interface Ilivro 
    {
        Task<ActionResult<Livro>> Post(Livro livro);

        Task<ActionResult<Livro>> GetById(string id);

        Task<ActionResult<Livro>> Update(string id, Livro input);

        Task<ActionResult> Delete(string id);
    }

   
    }

