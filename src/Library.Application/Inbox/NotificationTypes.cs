namespace Library.Application.Inbox;

public static class NotificationTypes
{
    public const string LoanCreated = "loan.created";
    public const string LoanReturned = "loan.returned";
    public const string LoanRenewed = "loan.renewed";
    public const string LoanDueSoon = "loan.due-soon";
    public const string LoanOverdue = "loan.overdue";
    public const string EbookExpired = "loan.ebook-expired";
    public const string ReservationReady = "reservation.ready";
    public const string ReservationExpired = "reservation.expired";
    public const string FineIssued = "fine.issued";
    public const string FineSettled = "fine.settled";
    public const string PasswordChanged = "account.password-changed";
}
