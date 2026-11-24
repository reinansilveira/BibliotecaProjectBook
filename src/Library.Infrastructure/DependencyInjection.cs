using Library.Application.Abstractions;
using Library.Application.Account;
using Library.Application.Audit;
using Library.Application.Auth;
using Library.Application.Books;
using Library.Application.Books.Import;
using Library.Application.Books.Inventory;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Loans.Reading;
using Library.Application.Members;
using Library.Application.Reports;
using Library.Application.Reservations;
using Library.Application.Reviews;
using Library.Application.Wishlist;
using Library.Infrastructure.Auditing;
using Library.Infrastructure.Caching;
using Library.Infrastructure.External;
using Library.Infrastructure.Jobs;
using Library.Infrastructure.Messaging;
using Library.Infrastructure.Notifications;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories;
using Library.Infrastructure.Reports;
using Library.Infrastructure.Search;
using Library.Infrastructure.Security;
using Library.Infrastructure.Seeding;
using Library.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    private const string UserAgent = "LibraryApi/1.0 (+https://github.com/reinansilveira/BibliotecaProjectBook)";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSecurity(configuration);
        services.AddCatalogServices(configuration);
        services.AddExternalServices();

        services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
        services.AddScoped<IEmailSender, SendGridEmailSender>();
        services.TryAddScoped<IRealtimeNotifier, NullRealtimeNotifier>();

        var healthChecks = services.AddHealthChecks().AddDbContextCheck<LibraryDbContext>("sqlserver");

        services.AddMessaging(configuration, healthChecks);
        services.AddMaintenanceJob(configuration);

        if (configuration.GetValue<bool>("Seed:DemoEbooks"))
        {
            services.AddHostedService<DemoEbookImporter>();
        }

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("The 'Database' connection string is not configured.");

        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<NotificationDispatchInterceptor>();

        services.AddDbContext<LibraryDbContext>((provider, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
            .AddInterceptors(provider.GetRequiredService<AuditInterceptor>(), provider.GetRequiredService<NotificationDispatchInterceptor>()));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LibraryDbContext>());

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookCopyRepository, BookCopyRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IFineRepository, FineRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IOneTimeTokenRepository, OneTimeTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IFileStorage, DatabaseFileStorage>();
    }

    private static void AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IEbookLinkSigner, EbookLinkSigner>();
    }

    private static void AddCatalogServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SearchSettings>(configuration.GetSection(SearchSettings.SectionName));
        services.AddSingleton<FullTextSearch>();

        services.Configure<CachingSettings>(configuration.GetSection(CachingSettings.SectionName));
        services.AddHybridCache();
        services.AddSingleton<ICatalogCache, HybridCatalogCache>();
    }

    private static void AddExternalServices(this IServiceCollection services)
    {
        services.AddHttpClient<IBookMetadataProvider, OpenLibraryMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }).AddStandardResilienceHandler();

        services.AddHttpClient<IEbookSource, GutenbergEbookSource>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }).AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 1;
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
        });
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration, IHealthChecksBuilder healthChecks)
    {
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        var section = configuration.GetSection(MessagingSettings.SectionName);
        services.Configure<MessagingSettings>(section);
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

        if (!section.GetValue(nameof(MessagingSettings.Enabled), true))
        {
            return;
        }

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<NotificationConsumer>();
        healthChecks.AddCheck<RabbitMqHealthCheck>("rabbitmq");
    }

    private static void AddMaintenanceJob(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("MaintenanceJob");

        if (!section.GetValue("Enabled", true))
        {
            return;
        }

        var cron = section.GetValue<string>("Cron") ?? "0 0/15 * * * ?";

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(MaintenanceJob));
            quartz.AddJob<MaintenanceJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(MaintenanceJob)}-trigger")
                .WithCronSchedule(cron));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
