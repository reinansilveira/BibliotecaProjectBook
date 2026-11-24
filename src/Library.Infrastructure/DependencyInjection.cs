using Library.Application.Abstractions;
using Library.Application.Auth;
using Library.Application.Blacklist;
using Library.Application.Books;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Reports;
using Library.Infrastructure.Jobs;
using Library.Infrastructure.Messaging;
using Library.Infrastructure.Notifications;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories;
using Library.Infrastructure.Reports;
using Library.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Library.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("The 'Database' connection string is not configured.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LibraryDbContext>());

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IBlacklistRepository, BlacklistRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
        services.AddScoped<IEmailSender, SendGridEmailSender>();

        var healthChecks = services.AddHealthChecks().AddDbContextCheck<LibraryDbContext>("sqlserver");

        services.AddMessaging(configuration, healthChecks);
        services.AddBlacklistJob(configuration);

        return services;
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

    private static void AddBlacklistJob(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("BlacklistJob");

        if (!section.GetValue("Enabled", true))
        {
            return;
        }

        var cron = section.GetValue<string>("Cron") ?? "0 0 * * * ?";

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(BlacklistJob));
            quartz.AddJob<BlacklistJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(BlacklistJob)}-trigger")
                .WithCronSchedule(cron));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
