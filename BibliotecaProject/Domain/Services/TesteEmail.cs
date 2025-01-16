using BibliotecaProject.Domain.Entities;

namespace BibliotecaProject.Domain.Services;

public class TesteEmail
{
    private readonly EnviarEmail _enviarEmailService;

    public TesteEmail(EnviarEmail enviarEmailService)
    {
        _enviarEmailService = enviarEmailService;
    }

    public async Task TestarEnvioEmail()
    {
        
        var email = new Email
        {
            origem = "reinanlanz@gmail.com",   
            destino = "reinanlanz@gmail.com",   
            assunto = "Emprestimo Atrasado seu safado", 
            mensagem = "Devolva meu livro" 
        };

        // Enviando o e-mail
        await _enviarEmailService.Enviar(email);
    }
}