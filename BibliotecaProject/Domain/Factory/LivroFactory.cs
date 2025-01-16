
using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.EntitiesDTO;


namespace BibliotecaProject.Domain.Factory;

public class LivroFactory
{

    public static Livro NovoLivro(LivroDTO livroDto)
    {
        
      var livros = new Livro()
        {
            Id = Guid.NewGuid(),
            NomeLivro = livroDto.NomeLivro,
            Autor = livroDto.Autor,
            DescricaoLivro = livroDto.DescricaoLivro,
            DataCriacao = livroDto.DataCriacao,
            LivroDeletado = false
        };

        return livros;
        //   _mapper.Map<LivroDTO>(livros);
    }
    

  
}