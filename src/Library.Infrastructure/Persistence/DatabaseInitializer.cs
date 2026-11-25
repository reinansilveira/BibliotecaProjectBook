using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Seeding;
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

        if (!configuration.GetValue<bool>("Seed:DemoCatalog"))
        {
            return;
        }

        await SeedDemoCatalogAsync(context, provider);
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
        admin.ConfirmEmail(now);

        context.Members.Add(admin);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (ConflictException)
        {
            if (await context.Members.AsNoTracking().AnyAsync(member => member.Email == email))
            {
                return;
            }

            throw;
        }

        Logger(provider).LogInformation("Initial administrator {Email} created.", email);
    }

    private static async Task SeedDemoCatalogAsync(LibraryDbContext context, IServiceProvider provider)
    {
        if (await context.Books.AnyAsync())
        {
            return;
        }

        var now = provider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        foreach (var demo in DemoCatalog.Books)
        {
            var book = new Book(
                new BookDetails(demo.Title, demo.Author, demo.Description, demo.Genre, demo.Isbn, demo.PublishedYear, DemoCatalog.CoverUrl(demo.Isbn)),
                now);

            context.Books.Add(book);
            context.BookCopies.AddRange(Enumerable.Range(1, demo.Copies)
                .Select(number => new BookCopy(book.Id, $"DM-{demo.Isbn[^6..]}-{number:00}", now)));
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (ConflictException)
        {
            return;
        }

        Logger(provider).LogInformation("Demo catalog with {Count} books created.", DemoCatalog.Books.Count);
    }

    private static ILogger Logger(IServiceProvider provider) =>
        provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
}
