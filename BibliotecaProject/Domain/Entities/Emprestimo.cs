using System.ComponentModel.DataAnnotations;
using BibliotecaProject.Domain.Services;
using BibliotecaProject.Presentation.Controllers;
using FluentValidation;

namespace BibliotecaProject.Domain.Entities
{
    public class Emprestimo
    {
        [Key]
        public Guid EmprestimoId { get; set; }

        public Guid MembroId { get; set; }

        public Guid LivroId { get; set; }

        public DateTime? DataEmprestimo { get; set; } = DateTime.Now;

        public DateTime DataDevolucao { get; set; }  //7 dias para fazer a devolução.

        public bool StatusEmprestimo { get; set; }
       


        public void Update (Emprestimo input) 
        {
            DataEmprestimo = input.DataEmprestimo;
            DataDevolucao = input.DataDevolucao;
        }
      
        internal void Update (DateTime dataEmprestimo, DateTime dataDevolução) 
        {
            DataEmprestimo = dataEmprestimo;
            DataDevolucao = dataDevolução;
        }

        public void Emprestado() 
        {
            StatusEmprestimo = true;
        }

    }
    
}
