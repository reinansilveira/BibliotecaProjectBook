using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Library.Application.Abstractions;
using Library.Application.Books.Import;
using Library.Application.Books.Inventory;
using Library.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

[assembly: AssemblyFixture(typeof(LibraryApiFactory))]

namespace Library.IntegrationTests.Infrastructure;

public sealed class LibraryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "Admin@Tests123";
    public const string FrontEndUrl = "https://library.test";

    private const string RabbitMqUser = "library";
    private const string RabbitMqPassword = "library-tests";
    private const int RabbitMqPort = 5672;
    private const string DatabaseName = "LibraryTests";

    private readonly IFutureDockerImage _sqlServerImage = new ImageFromDockerfileBuilder()
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "docker/sqlserver")
        .WithName("library-sqlserver-fts:tests")
        .WithCleanUp(false)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-alpine")
        .WithUsername(RabbitMqUser)
        .WithPassword(RabbitMqPassword)
        .Build();

    private MsSqlContainer? _sqlServer;

    public CapturingEmailSender Emails { get; } = new();

    public FakeBookMetadataProvider Metadata { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _sqlServerImage.CreateAsync();
        _sqlServer = new MsSqlBuilder(_sqlServerImage).Build();

        await Task.WhenAll(_sqlServer.StartAsync(), _rabbitMq.StartAsync());

        await using var connection = new SqlConnection(_sqlServer.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{DatabaseName}]", connection);
        await command.ExecuteNonQueryAsync();
    }

    public HttpClient CreateClientAt(DateTime utcNow) => CreateClientWithClockOffset(utcNow - DateTime.UtcNow);

    public HttpClient CreateClientWithClockOffset(TimeSpan offset)
    {
        var clock = new OffsetTimeProvider(offset);

        return WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
        })).CreateClient();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync();
        }

        await _rabbitMq.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", new SqlConnectionStringBuilder(_sqlServer!.GetConnectionString()) { InitialCatalog = DatabaseName }.ConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("MaintenanceJob:Enabled", "false");
        builder.UseSetting("Caching:Enabled", "false");
        builder.UseSetting("Account:FrontEndUrl", FrontEndUrl);
        builder.UseSetting("Jwt:Issuer", "LibraryApi");
        builder.UseSetting("Jwt:Audience", "LibraryApi.Clients");
        builder.UseSetting("Jwt:Key", "key-used-only-by-the-integration-tests-0123456789");
        builder.UseSetting("Seed:Admin:Email", AdminEmail);
        builder.UseSetting("Seed:Admin:Password", AdminPassword);
        builder.UseSetting("Messaging:OutboxPollingIntervalSeconds", "1");
        builder.UseSetting("RateLimiting:AuthenticationPermitLimit", "100000");
        builder.UseSetting("RabbitMq:HostName", _rabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(RabbitMqPort).ToString());
        builder.UseSetting("RabbitMq:UserName", RabbitMqUser);
        builder.UseSetting("RabbitMq:Password", RabbitMqPassword);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Emails);
            services.AddSingleton<IBookMetadataProvider>(Metadata);
            services.AddSingleton<IEbookSource, FakeEbookSource>();
        });
    }

    private sealed class OffsetTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow().Add(offset);
    }
}
