using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Services
{
    public class MembroServices : IMembro
    {
        private readonly BibliotecaDbContext _context;

        public MembroServices(BibliotecaDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult<Membro>> Post(Membro membro)
        {
            //Tenho que colocar primeiramente os livros emprestado para os membros
            var membros = new Membro()
            {
                IdMembro = Guid.NewGuid(),
                Nome = membro.Nome,
                DataNascimento = membro.DataNascimento,
                Telefone = membro.Telefone,
                Email = membro.Email
            };
            _context.Membros.Add(membros);
            await _context.SaveChangesAsync();
            return membros;
        }

        public async Task<ActionResult<Membro>> GetById(string id)
        {
            var membroId = Guid.Parse(id);
            var membro = await _context.Membros.SingleOrDefaultAsync(x => x.IdMembro == membroId);

            if (membro == null)
            {
                return new NotFoundResult();
            }

            return membro;
        }

        public async Task<ActionResult<Membro>> Update(string id, Membro input)
        {
            var membroId = Guid.Parse(id);
            var membro = await _context.Membros.SingleOrDefaultAsync(x => x.IdMembro == membroId);
            if (membro == null)
            {
                return new NotFoundResult();
            }

            input.IdMembro = membroId;
            membro.Update(input);
            await _context.SaveChangesAsync();
            return membro;
        }

        public async Task<ActionResult> Delete(string id)
        {
            var membroId = Guid.Parse(id);
            var membro = await _context.Membros.SingleOrDefaultAsync(x => x.IdMembro == membroId);
            if (membro == null)
            {
                return new NotFoundResult();
            }

            membro.Delete();
            _context.Membros.Remove(membro);
            await _context.SaveChangesAsync();
            return new NoContentResult();
        }
    }
}

