namespace BibliotecaProject.Domain.Services;

public class HistoricoEmprestimos
{
    [Key]
    public Guid EmprestimoId { get; set; }
    
    public DateTime? DataEmprestimo { get; set; } = DateTime.Now;
    
    public DateTime DataDevolucao { get; set; }
}