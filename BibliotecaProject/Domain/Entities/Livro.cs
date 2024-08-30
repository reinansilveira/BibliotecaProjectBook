using BibliotecaProject.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaProject.Domain.Entities
{
    public class Livro 
    {
        [Key]
        public Guid Id { get; set; }    

        public string NomeLivro { get; set; }

        public string Autor { get; set; }

        public string DescricaoLivro { get; set; }

        public DateTime DataCriacao { get; set; }

        public bool LivroDeletado {  get; set; }

        public void Update(Livro input) 
        {
            NomeLivro = input.NomeLivro;
            Autor = input.Autor;
            DescricaoLivro = input.DescricaoLivro;
            DataCriacao = input.DataCriacao;
        }

        internal void Update (string nomeLivro, string autor, string descricaoLivro, DateTime dataCriacao ) 
        {
            NomeLivro = nomeLivro;
            Autor = autor;
            DescricaoLivro = descricaoLivro;
            DataCriacao = dataCriacao;
        }
        public void Delete () 
        {
            LivroDeletado = true;
        }
    }
}
