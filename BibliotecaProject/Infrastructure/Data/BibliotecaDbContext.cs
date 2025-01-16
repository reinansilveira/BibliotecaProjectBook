using BibliotecaProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaProject.Infrastructure.Data
{
    public class BibliotecaDbContext : DbContext
    {
        public  BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options) :base(options)
        { }


        public DbSet<Emprestimo> Emprestimos { get; set; }
        public DbSet<Livro> Livros { get; set; }
        public DbSet<Membro> Membros { get; set; }
        
        public DbSet<BlackList> BlackLists { get; set; }

    }
}
