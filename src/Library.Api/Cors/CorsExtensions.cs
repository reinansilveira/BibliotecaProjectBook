namespace Library.Api.Cors;

public static class CorsExtensions
{
    public const string FrontEndPolicy = "FrontEnd";

    private const string SectionName = "Cors:AllowedOrigins";

    public static IServiceCollection AddFrontEndCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = AllowedOrigins(configuration);

        if (origins.Length == 0)
        {
            return services;
        }

        services.AddCors(options => options.AddPolicy(FrontEndPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Retry-After")));

        return services;
    }

    public static WebApplication UseFrontEndCors(this WebApplication app)
    {
        if (AllowedOrigins(app.Configuration).Length == 0)
        {
            return app;
        }

        app.UseCors(FrontEndPolicy);

        return app;
    }

    private static string[] AllowedOrigins(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.TrimEnd('/'))
            .ToArray() ?? [];
}
