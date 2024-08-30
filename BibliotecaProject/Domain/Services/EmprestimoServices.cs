using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Services
{
    public class EmprestimoServices : IEmprestimo
    {
        private readonly BibliotecaDbContext _context;

        public EmprestimoServices(BibliotecaDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult<Emprestimo>> Post(Emprestimo emprestimo)
        {
            // Verifica se o membro tem um empréstimo não devolvido há mais de 7 dias
            var emprestimoExistente = await _context.Emprestimos.FirstOrDefaultAsync(e => e.MembroId == emprestimo.MembroId);
            var tempoLimite = emprestimo.DataDevolucao.AddDays(7);

            if (emprestimoExistente != null)
            {
                return new BadRequestResult();
            }
            if (emprestimo.DataEmprestimo > tempoLimite) 
               {
                 return new NotFoundResult();
            
               }
            var livro = await _context.Livros.FindAsync(emprestimo.LivroId);
            if (livro == null)
            {
                return new NotFoundResult();
            }

            // Verifica se o membro existe
            var membro = await _context.Membros.FindAsync(emprestimo.MembroId);
            if (membro == null)
            {
                return new NotFoundResult();
            }

            var emprestimos = new Emprestimo()
            {
                EmprestimoId = Guid.NewGuid(),
                DataEmprestimo = emprestimo.DataEmprestimo,
                DataDevolucao = emprestimo.DataDevolucao,
                LivroId = livro.Id,
                MembroId = membro.IdMembro
            };

            _context.Emprestimos.Add(emprestimos);
            await _context.SaveChangesAsync();
            return emprestimo;
        }


        public async Task<ActionResult<Emprestimo>> GetById(string id)
        {
            var emprestimoId = Guid.Parse(id);
            var emprestimo = await _context.Emprestimos.SingleOrDefaultAsync(x => x.EmprestimoId == emprestimoId);

            if (emprestimo == null)
            {
                return new NotFoundResult();
            }
            return emprestimo;
        }

        public async Task<ActionResult<Emprestimo>> Update( string id, Emprestimo input)
        {
            var emprestimoId = Guid.Parse(id);
            var emprestimo = await _context.Emprestimos.SingleOrDefaultAsync(x => x.EmprestimoId == emprestimoId);
            if (emprestimo == null)
            {
                return new NotFoundResult();
            }
            input.EmprestimoId = emprestimoId;
            emprestimo.Update(input);
            await _context.SaveChangesAsync();
            return emprestimo;
        }
        public async Task<ActionResult> Emprestado(string id)
        {
            var emprestimoId = Guid.Parse(id);
            var emprestimo = await _context.Emprestimos.SingleOrDefaultAsync(x => x.EmprestimoId == emprestimoId);
            if (emprestimo == null)
            {
                return new NotFoundResult();
            }
            emprestimo.Emprestado();
            await _context.SaveChangesAsync();
            return new NoContentResult();
        }
        
    }
}
