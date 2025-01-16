using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Factory;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Exceptions;
using BibliotecaProject.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Domain.Services
{
    public class EmprestimoServices : IEmprestimo
    {
        private readonly BibliotecaDbContext _context;

        public EmprestimoServices(BibliotecaDbContext context )
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
                throw new NotFoundException(400, "O membro contém um emprestimo existente.");
            }
            
            if (DateTime.Now > tempoLimite) 
               {
                   throw new NotFoundException(400, "Seu emprestimo já passou de 7 dias.");
               }
            
            var livro = await _context.Livros.FindAsync(emprestimo.LivroId);
            if (livro == null)
            {
                throw new NotFoundException(400, "O livro não existe.");
            }
           
            // Verifica se o membro existe
           
            var membro = await _context.Membros.FindAsync(emprestimo.MembroId);
            if (membro == null)
            {
                throw new NotFoundException(400, "O mesmo membro já fez um emprestimo.");
            }

            var criarEmprestimo = EmprestimoFactory.NovoEmprestimo(emprestimo, livro, membro);
            
            _context.Emprestimos.Add(criarEmprestimo);
            await _context.SaveChangesAsync();
            return emprestimo;
        }


        public async Task<ActionResult<Emprestimo>> GetById(string id)
        {
            var emprestimoId = Guid.Parse(id);
            var emprestimo = await _context.Emprestimos.SingleOrDefaultAsync(x => x.EmprestimoId == emprestimoId);

            if (emprestimo == null)
            {
                new NotFoundException(400, "Aqui foi um erro de código");
            }
            return emprestimo;
        }

        public async Task<ActionResult<Emprestimo>> Update( string id, Emprestimo input)
        {
            var emprestimoId = Guid.Parse(id);
            var emprestimo = await _context.Emprestimos.SingleOrDefaultAsync(x => x.EmprestimoId == emprestimoId);
            if (emprestimo == null)
            {
                throw new NotFoundException(400,"Aqui foi um erro de código");
                //new NotFoundException(400, "Aqui foi um erro de código");
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
                new NotFoundException(400, "Aqui foi um erro de código");
            }
            emprestimo.Emprestado();
            await _context.SaveChangesAsync();
            return new NoContentResult();
        }
        
    }
}
