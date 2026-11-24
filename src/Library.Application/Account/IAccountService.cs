namespace Library.Application.Account;

public interface IAccountService
{
    Task SendEmailConfirmationAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid memberId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
