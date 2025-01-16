using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace BibliotecaProject.Domain.Entities
{
    public class Membro
    {
        [Key]
        public Guid IdMembro { get; set; }

        public string Nome { get; set; } = string.Empty;

        public DateTime DataNascimento { get; set; }

        public string Telefone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        
        public string Senha { get; set; } = string.Empty;
        
        public bool Deletado { get; set; }

        //public List<HistoricoEmprestimos> HistoricoEmprestimos { get; set; }

        public void Update(Membro input)
        {
            Nome = input.Nome;
            DataNascimento = input.DataNascimento;
            Telefone = input.Telefone;
            Email = input.Email;
        }

        internal void Update(DateTime dataNascimento, string nome, string telefone, string email, string senha)
        {
           Nome = nome;
           DataNascimento = dataNascimento;
           Telefone = telefone;
           Email = email;
           Senha = senha;

        }

        public void Delete()
        {
            Deletado = false;

        }
    }
}
