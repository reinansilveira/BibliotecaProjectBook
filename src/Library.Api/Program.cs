using System.Text.Json.Serialization;
using Library.Api.Authorization;
using Library.Api.Cors;
using Library.Api.ErrorHandling;
using Library.Api.Observability;
using Library.Api.OpenApi;
using Library.Api.RateLimiting;
using Library.Application;
using Library.Infrastructure;
using Library.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddAuthenticationRateLimiting(builder.Configuration);
builder.Services.AddFrontEndCors(builder.Configuration);

var app = builder.Build();

app.UseStructuredRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseFrontEndCors();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.InitializeDatabaseAsync(app.Configuration);
}

app.UseAuthenticationRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

public partial class Program;
