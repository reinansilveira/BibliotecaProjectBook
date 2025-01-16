namespace BibliotecaProject.Domain.Services;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using SendGrid;
public class NovoTeste
{
    
        private void Main()
        {
            Execute().Wait();
        }

        public async Task Execute()
        {
            var apiKey = "REMOVED";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("reinanlanz@gmail.com", "..");
            var subject = "";
            var to = new EmailAddress("reinanlanz@gmail.com", "....");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = "";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }

