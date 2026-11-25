using Library.Application.Books.Inventory;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Seeding;

public sealed class DemoEbookImporter : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoEbookImporter> _logger;

    public DemoEbookImporter(IServiceScopeFactory scopeFactory, ILogger<DemoEbookImporter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartDelay, stoppingToken);

        foreach (var demo in DemoCatalog.Books)
        {
            await ImportAsync(demo, stoppingToken);
        }
    }

    private async Task ImportAsync(DemoBook demo, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            var book = await context.Books.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Isbn == demo.Isbn && !item.IsDeleted && item.EbookFileKey == null, cancellationToken);

            if (book is null)
            {
                return;
            }

            var inventory = scope.ServiceProvider.GetRequiredService<IBookInventoryService>();
            await inventory.ImportGutenbergEbookAsync(book.Id, new GutenbergImportRequest(demo.GutenbergId, DemoCatalog.EbookLicenses), cancellationToken);
            _logger.LogInformation("Imported the e-book of {Title} from Project Gutenberg.", demo.Title);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Could not import the e-book of {Title}.", demo.Title);
        }
    }
}
