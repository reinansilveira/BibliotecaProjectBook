using System.Collections;
using System.Runtime.InteropServices.JavaScript;
using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Factory;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Domain.RegistroUsuarioValidator;
using BibliotecaProject.Exceptions;
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
            
                var validator = new RegisteUserValidator();
                var result = validator.Validate(membro);
            
                if (result.IsValid == false)
                {
                    var errorMessages = result.Errors.Select(e => e.ErrorMessage);
                    throw new NotFoundException(400, "Nome Vazio ou e-mail invalido.");
                }
       
            
           var criarMembros = MembroFactory.CriarMembro(membro);
            
            _context.Membros.Add(criarMembros);
            await _context.SaveChangesAsync();
            return membro;
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

        private void Validate(Membro membro)
        {
            
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

