using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Library.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAuthenticationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(RateLimitPolicies.Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.AuthenticationPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.AuthenticationWindowSeconds),
                        QueueLimit = 0
                    }));
        });

        if (settings.TrustForwardedHeaders)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        return services;
    }

    public static WebApplication UseAuthenticationRateLimiting(this WebApplication app)
    {
        var settings = app.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

        if (settings.TrustForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        app.UseRateLimiter();

        return app;
    }

    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Too many attempts. Please wait a moment and try again."
        };

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problemDetails
        });
    }
}
