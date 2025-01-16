namespace BibliotecaProject.Domain.Entities;

public class Email
{
    public string origem { get; set; }
    public string destino { get; set; }
    public string assunto { get; set; }
    public string mensagem { get; set; }
    public string usuario { get; set; }
    public string senha { get; set; }
}