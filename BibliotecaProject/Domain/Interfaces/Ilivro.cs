using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.EntitiesDTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Interfaces
{
    public interface Ilivro 
    {
        Task<ActionResult<LivroDTO>> Post (LivroDTO livroDto);

        Task<ActionResult<Livro>> GetById(string id);

        Task<ActionResult<Livro>> Update(string id, Livro input);

        Task<ActionResult> Delete(string id);

      
    }

   
    }

