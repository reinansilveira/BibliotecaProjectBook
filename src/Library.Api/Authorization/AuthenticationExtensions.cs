using Library.Application.Abstractions;
using Library.Domain.Enums;
using Library.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Library.Api.Authorization;

public static class AuthenticationExtensions
{
    public const string HubsPath = "/hubs";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwt) =>
            {
                var settings = jwt.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.GetSigningKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role
                };
                options.Events = new JwtBearerEvents { OnMessageReceived = ReadHubAccessToken };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IAuthorizationHandler, ResourceOwnerOrAdminHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(nameof(Role.Admin)))
            .AddPolicy(Policies.MemberOrAdmin, policy => policy.RequireRole(nameof(Role.Member), nameof(Role.Admin)))
            .AddPolicy(Policies.ResourceOwnerOrAdmin, policy => policy.AddRequirements(new ResourceOwnerOrAdminRequirement()));

        return services;
    }

    private static Task ReadHubAccessToken(MessageReceivedContext context)
    {
        var token = context.Request.Query["access_token"].ToString();

        if (string.IsNullOrEmpty(token) || !context.HttpContext.Request.Path.StartsWithSegments(HubsPath))
        {
            return Task.CompletedTask;
        }

        context.Token = token;

        return Task.CompletedTask;
    }
}
