using FluentValidation;
using Library.Application.Auth;
using Library.Application.Blacklist;
using Library.Application.Books;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Application.Notifications;
using Library.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
