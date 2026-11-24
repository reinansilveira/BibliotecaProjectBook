using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

public sealed class LibraryDbContext : DbContext, IUnitOfWork
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<BlacklistEntry> BlacklistEntries => Set<BlacklistEntry>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
