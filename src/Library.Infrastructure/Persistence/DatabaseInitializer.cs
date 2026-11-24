using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<LibraryDbContext>();

        await context.Database.MigrateAsync();
        await SeedAdministratorAsync(context, provider, configuration);
    }

    private static async Task SeedAdministratorAsync(LibraryDbContext context, IServiceProvider provider, IConfiguration configuration)
    {
        var email = configuration["Seed:Admin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await context.Members.AnyAsync(member => member.Email == email))
        {
            return;
        }

        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var now = provider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var admin = new Member("Administrator", new DateTime(1990, 1, 1), "0000000000", email, passwordHasher.Hash(password), now, Role.Admin);

        context.Members.Add(admin);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await context.Members.AsNoTracking().AnyAsync(member => member.Email == email))
            {
                return;
            }

            throw;
        }

        provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer))
            .LogInformation("Initial administrator {Email} created.", email);
    }
}
