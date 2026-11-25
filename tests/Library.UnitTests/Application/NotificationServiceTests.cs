using Library.Application.Abstractions;
using Library.Application.Events;
using Library.Application.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Library.UnitTests.Application;

public class NotificationServiceTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_emailSender, NullLogger<NotificationService>.Instance);
    }

    public static TheoryData<IIntegrationEvent, string, string> Events => new()
    {
        { new MemberRegisteredEvent(Guid.NewGuid(), "Ana", "ana@email.com"), "ana@email.com", "Welcome to the library" },
        { new EmailConfirmationRequestedEvent(Guid.NewGuid(), "Ana", "ana@email.com", "https://x/confirm"), "ana@email.com", "Confirm your email" },
        { new PasswordResetRequestedEvent(Guid.NewGuid(), "Ana", "ana@email.com", "https://x/reset"), "ana@email.com", "Reset your password" },
        { new LoanCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 8)), "ana@email.com", "Loan confirmed" },
        { new LoanReturnedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 5), false), "ana@email.com", "Book returned" },
        { new LoanDueSoonEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 8), false), "ana@email.com", "Your loan is due tomorrow" },
        { new LoanOverdueEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 8), 0.5m, "USD"), "ana@email.com", "Overdue loan" },
        { new ReservationReadyEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 10)), "ana@email.com", "Your reserved book is available" },
        { new FineIssuedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", 3, 1.5m, "USD"), "ana@email.com", "Late return fine" }
    };

    [Theory]
    [MemberData(nameof(Events))]
    public async Task Each_event_sends_an_email_to_the_member(IIntegrationEvent integrationEvent, string expectedTo, string expectedSubject)
    {
        await _service.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(message => message.To == expectedTo && message.Subject == expectedSubject),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Loan_confirmation_contains_the_book_and_the_due_date()
    {
        var message = NotificationService.CreateMessage(
            new LoanCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 8)));

        Assert.NotNull(message);
        Assert.Contains("Dune", message.Body);
        Assert.Contains("2026-03-08", message.Body);
    }

    [Fact]
    public void Fine_email_shows_the_amount_with_the_currency()
    {
        var message = NotificationService.CreateMessage(new FineIssuedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", 3, 1.5m, "USD"));

        Assert.Contains("USD 1.50", message!.Body);
    }
}
