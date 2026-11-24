using Library.Api.Authorization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Library.Api.Observability;

public static class LoggingExtensions
{
    private const string DevelopmentTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Library.Api")
            .WriteTo.Console(CreateFormatter(builder.Environment)));

        return builder;
    }

    public static WebApplication UseStructuredRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.EnrichDiagnosticContext = (context, http) =>
                context.Set("UserId", http.User.GetMemberId()?.ToString() ?? "anonymous");
            options.GetLevel = (http, _, exception) => GetLevel(http, exception);
        });

        return app;
    }

    private static ITextFormatter CreateFormatter(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return new MessageTemplateTextFormatter(DevelopmentTemplate);
        }

        return new CompactJsonFormatter();
    }

    private static LogEventLevel GetLevel(HttpContext http, Exception? exception)
    {
        if (exception is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (http.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Verbose;
        }

        return LogEventLevel.Information;
    }
}
