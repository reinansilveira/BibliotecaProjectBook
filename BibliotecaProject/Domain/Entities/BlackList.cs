using System.ComponentModel.DataAnnotations;

namespace BibliotecaProject.Domain.Entities;

public class BlackList
{
    [Key]
    public Guid IdBlackList { get; set; }
    public Guid IdMembro { get; set; }
    public string Nome { get; set; } 

}