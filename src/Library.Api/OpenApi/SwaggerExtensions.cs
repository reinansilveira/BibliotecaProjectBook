using System.Reflection;
using Microsoft.OpenApi;

namespace Library.Api.OpenApi;

public static class SwaggerExtensions
{
    private const string SchemeName = "Bearer";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Library Management API",
                Version = "v1",
                Description = "Catalog with printed copies and e-books, loans, waiting lists, fines, reviews and real-time notifications. " +
                              "Sign in at /api/v1/auth/login and click Authorize to send the JWT."
            });

            options.AddSecurityDefinition(SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste only the access token returned by the login endpoint."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
            });

            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            options.IncludeXmlComments(xmlFile);
        });

        return services;
    }
}
