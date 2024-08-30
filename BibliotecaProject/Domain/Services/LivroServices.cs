using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Services
{
    public class LivroServices : Ilivro
    {
        private readonly BibliotecaDbContext _context;

        public LivroServices(BibliotecaDbContext context)
        {
            _context = context;
        }


        public async Task<ActionResult<Livro>> Post(Livro livro)
        {

            var livros = new Livro()
            {
                Id = Guid.NewGuid(),
                NomeLivro = livro.NomeLivro,
                Autor = livro.Autor,
                DescricaoLivro = livro.DescricaoLivro,
                DataCriacao = livro.DataCriacao
            };

            _context.Livros.Add(livros);
            await _context.SaveChangesAsync();
            return livros;
        }

         public async Task<ActionResult<Livro>> GetById(string id)
         {
             var livroId = Guid.Parse(id);
             var livro = await _context.Livros.SingleOrDefaultAsync(x => x.Id == livroId);

             if (livro == null)
             {
                return new NotFoundResult();
            }
               return livro;
        }

        public async Task<ActionResult<Livro>> Update(string id, Livro input)
        {
            var livroId = Guid.Parse(id);
            var livro = await _context.Livros.SingleOrDefaultAsync(x => x.Id == livroId);
            if (livro == null)
            {
                return new NotFoundResult();
            }
            input.Id = livroId;
            livro.Update(input);
            await _context.SaveChangesAsync();
            return livro;
        }

        public async Task<ActionResult> Delete(string id)
        {
            var livroId = Guid.Parse(id);
            var livro = await _context.Livros.SingleOrDefaultAsync(x => x.Id == livroId);
            if (livro == null)
            {
                return new NotFoundResult();
            }
            livro.Delete();
            await _context.SaveChangesAsync();
            return new NoContentResult();
        }
    }
}
