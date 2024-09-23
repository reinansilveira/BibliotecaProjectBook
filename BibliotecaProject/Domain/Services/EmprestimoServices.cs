using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;
using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using BibliotecaProject.Exceptions;
using BibliotecaProject.Infrastructure.Data;
using BibliotecaProject.Presentation.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BibliotecaProject.Domain.Services
{
    public class EmprestimoServices : IEmprestimo
    {
        private readonly BibliotecaDbContext _context;

        public EmprestimoServices(BibliotecaDbContext context)
        {
            _context = context;
        }
        


        public async Task<ActionResult<Emprestimo>> Post(Emprestimo emprestimo,IValidator<Emprestimo> validator)
        {
            // Verifica se o membro tem um empréstimo não devolvido há mais de 7 dias
            var emprestimoExistente = await _context.Emprestimos.FirstOrDefaultAsync(e => e.MembroId == emprestimo.MembroId);
             var tempoLimite = emprestimo.DataDevolucao.AddDays(7);

            var validatorUsuario = new EmprestimoUsuarioValidator();
            var result = validatorUsuario.Validate(emprestimo);
            var error = result.Errors.Select(e=> e.ErrorMessage);
            

            if (!result.IsValid)
            {
                return new BadRequestObjectResult(error);
            }
            
            
            if (emprestimoExistente != null)
            {
                return new NotFoundResult();
            }
            if (emprestimo.DataEmprestimo > tempoLimite) 
               {
                   new NotFoundException(400, "Seu emprestimo já passou de 7 dias.");
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
                new NotFoundException(400, "O mesmo membro já fez um emprestimo.");
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
                new NotFoundException(400, "Aqui foi um erro de código");
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
