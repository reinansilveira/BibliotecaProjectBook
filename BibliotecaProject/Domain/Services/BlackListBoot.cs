using BibliotecaProject.Domain.Entities;
using BibliotecaProject.Infrastructure.Data;


namespace BibliotecaProject.Domain.Services;

public class BlackListBoot
{

    private readonly BibliotecaDbContext _context;



    public BlackListBoot(BibliotecaDbContext context)
    {
        _context = context;

    }

    public async Task VerificarLogin()
    {
        var dataLimite = DateTime.Now.AddDays(-7);
        //var  emprestimoSemelhante = emprestimo.EmprestimoId == membro.IdMembro;


        List<Emprestimo> listaEmprestimoVencidos =
            _context.Emprestimos.Where(e => e.DataDevolucao < DateTime.Now).ToList();

        var compararListaVencida = listaEmprestimoVencidos.Select(e => e.MembroId).ToList();

        List<Membro> resultadoSemelhante =
            _context.Membros.Where(e => compararListaVencida.Contains(e.IdMembro)).ToList();
        Console.WriteLine($"Número de membros encontrados: {resultadoSemelhante.Count}");

            foreach (var empres in resultadoSemelhante)
           {
                  Console.WriteLine($"Membro: {empres.IdMembro}, {empres.Nome}");
             var blacklist = new BlackList()
               {
                   IdMembro = empres.IdMembro,
                   Nome = empres.Nome

               };
               _context.BlackLists.Add(blacklist);
             
           }
           await _context.SaveChangesAsync();
       }
       
    }
    