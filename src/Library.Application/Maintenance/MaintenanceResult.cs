namespace Library.Application.Maintenance;

public sealed record MaintenanceResult(int ExpiredEbookLoans, int ExpiredHolds, int PromotedReservations, int DueSoonReminders, int OverdueNotices);
