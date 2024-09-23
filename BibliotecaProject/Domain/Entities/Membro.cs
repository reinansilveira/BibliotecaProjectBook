using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace BibliotecaProject.Domain.Entities
{
    public class Membro
    {
        [Key]
        public Guid IdMembro { get; set; }

        public string Nome { get; set; }

        public DateTime DataNascimento { get; set; }

        public string Telefone { get; set; }

        public string Email { get; set; }
        
        public bool Deletado { get; set; }

        //public List<HistoricoEmprestimos> HistoricoEmprestimos { get; set; }

        public void Update(Membro input)
        {
            Nome = input.Nome;
            DataNascimento = input.DataNascimento;
            Telefone = input.Telefone;
            Email = input.Email;
        }

        internal void Update(DateTime dataNascimento, string nome, string telefone, string email)
        {
           Nome = nome;
           DataNascimento = dataNascimento;
           Telefone = telefone;
           Email = email;

        }

        public void Delete()
        {
            Deletado = false;

        }
    }
}
