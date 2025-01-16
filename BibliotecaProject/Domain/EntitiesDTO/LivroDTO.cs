using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaProject.Domain.EntitiesDTO;

public class LivroDTO 
{
    //public string NomeLivro { get; set; }

    public string Autor { get; set; }
    
    public string NomeLivro { get; set; }

    public string DescricaoLivro { get; set; }

    public DateTime DataCriacao { get; set; }

    public bool LivroDeletado {  get; set; }

    public static implicit operator LivroDTO(Livro livro)
    {
        return new LivroDTO()
        {
            Autor = livro.Autor,
            NomeLivro = livro.NomeLivro,
            DescricaoLivro = livro.DescricaoLivro,
            DataCriacao = livro.DataCriacao,
            LivroDeletado = livro.LivroDeletado
        };
    }


    }
