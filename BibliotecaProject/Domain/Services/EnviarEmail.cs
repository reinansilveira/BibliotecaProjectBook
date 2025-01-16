using BibliotecaProject.Domain.Entities;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using System;
using SendGrid;

namespace BibliotecaProject.Domain.Services;

public class EnviarEmail
{
    private readonly IConfiguration _configuration;

    public EnviarEmail(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task Enviar(Email email)
    {
        try
        {
            var apiKey = _configuration["SendGridSettings:ApiKey"];
            
            var enviarMensagemEmail = new SendGridMessage
            {
                From = new EmailAddress(email.origem),
                Subject = email.assunto,
                PlainTextContent = email.mensagem,
                HtmlContent = $"<p>{email.mensagem}</p>"
            };
            
            enviarMensagemEmail.AddTo(new EmailAddress(email.destino));
            var client = new SendGridClient(apiKey);
            
            var response =  await client.SendEmailAsync(enviarMensagemEmail);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Email enviado com sucesso!");
            }
            Console.WriteLine("Erro ao enviar o e-mail");
        }
        catch (Exception)
        {
            throw;
        }
        
        
    }
}