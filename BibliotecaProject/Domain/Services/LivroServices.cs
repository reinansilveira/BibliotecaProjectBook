
using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.EntitiesDTO;
using BibliotecaProject.Domain.Factory;
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


        public async Task<ActionResult<LivroDTO>> Post(LivroDTO livroDto)
        {
            var criarLivro = 
                new Livro
                {
                    Id = Guid.NewGuid(),
                    NomeLivro = livroDto.NomeLivro,
                    Autor = livroDto.Autor,
                    DescricaoLivro = livroDto.DescricaoLivro,
                    DataCriacao = livroDto.DataCriacao,
                    LivroDeletado = false
                };
            _context.Livros.Add(criarLivro); 
            await _context.SaveChangesAsync();
            
            var livroDTO = 
                new LivroDTO
                {
                    Autor = criarLivro.Autor, 
                    NomeLivro = criarLivro.NomeLivro,
                    DescricaoLivro = criarLivro.DescricaoLivro,
                    DataCriacao = criarLivro.DataCriacao, 
                    LivroDeletado = criarLivro.LivroDeletado
                }; 
            return livroDTO;
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
