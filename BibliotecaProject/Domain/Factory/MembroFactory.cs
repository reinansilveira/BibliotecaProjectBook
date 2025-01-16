using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Domain.Services.Criptografia;

namespace BibliotecaProject.Domain.Factory;

public class MembroFactory
{
    public static Membro CriarMembro(Membro membro)
    {
        var criptografiaDeSenha = new SenhaCriptografia();
        
        var membros = new Membro()
        {
            IdMembro = Guid.NewGuid(),
            Nome = membro.Nome,
            DataNascimento = membro.DataNascimento,
            Telefone = membro.Telefone,
            Email = membro.Email,
            Senha = criptografiaDeSenha.Criptografar(membro.Senha)
        };

        return membros;
    }
}