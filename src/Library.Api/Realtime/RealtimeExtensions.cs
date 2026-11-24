using Library.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Realtime;

public static class RealtimeExtensions
{
    public static IServiceCollection AddRealtimeNotifications(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, MemberIdUserIdProvider>();
        services.AddScoped<IRealtimeNotifier, SignalRNotifier>();

        return services;
    }

    public static WebApplication MapRealtimeNotifications(this WebApplication app)
    {
        app.MapHub<NotificationsHub>(NotificationsHub.Path);

        return app;
    }
}
