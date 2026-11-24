using FluentValidation;
using Library.Application.Account;
using Library.Application.Audit;
using Library.Application.Auth;
using Library.Application.Books;
using Library.Application.Books.Import;
using Library.Application.Books.Inventory;
using Library.Application.Fines;
using Library.Application.Inbox;
using Library.Application.Loans;
using Library.Application.Loans.Reading;
using Library.Application.Maintenance;
using Library.Application.Members;
using Library.Application.Notifications;
using Library.Application.Reports;
using Library.Application.Reservations;
using Library.Application.Reviews;
using Library.Application.Wishlist;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<FineSettings>(configuration.GetSection(FineSettings.SectionName));
        services.Configure<AccountSettings>(configuration.GetSection(AccountSettings.SectionName));

        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookInventoryService, BookInventoryService>();
        services.AddScoped<IBookImportService, BookImportService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IEbookReadingService, EbookReadingService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ReservationQueue>();
        services.AddScoped<IFineService, FineService>();
        services.AddScoped<IMemberStandingService, MemberStandingService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IInbox, Inbox.Inbox>();
        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
