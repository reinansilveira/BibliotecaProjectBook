namespace Library.Application.Account;

public sealed class AccountSettings
{
    public const string SectionName = "Account";

    public string FrontEndUrl { get; set; } = "http://localhost:4200";

    public int EmailConfirmationHours { get; set; } = 24;

    public int PasswordResetMinutes { get; set; } = 60;
}
