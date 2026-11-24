using System.Net.Http.Json;
using Library.Application.Loans;
using Library.Infrastructure.Persistence;
using Library.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Library.IntegrationTests;

public class MessagingTests(LibraryApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Registration_sends_a_welcome_email_through_rabbitmq()
    {
        var (member, _, _) = await RegisterAndSignInAsync();

        var delivered = await Eventually.SatisfiesAsync(
            () => Factory.Emails.Messages.Any(message => message.To == member.Email && message.Subject == "Welcome to the library"),
            Token);

        Assert.True(delivered, "The welcome email was not delivered through the message broker.");
    }

    [Fact]
    public async Task Borrowing_a_book_sends_a_loan_confirmation()
    {
        var admin = await CreateAdminClientAsync();
        var book = await CreateBookAsync(admin);
        var (member, _, client) = await RegisterAndSignInAsync();

        var response = await client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(book.Id, member.Id), Token);
        response.EnsureSuccessStatusCode();

        var delivered = await Eventually.SatisfiesAsync(
            () => Factory.Emails.Messages.Any(message => message.To == member.Email && message.Subject == "Loan confirmed" && message.Body.Contains(book.Title)),
            Token);

        Assert.True(delivered, "The loan confirmation was not delivered through the message broker.");
    }

    [Fact]
    public async Task Outbox_message_is_published_and_processed_exactly_once()
    {
        var (member, _, _) = await RegisterAndSignInAsync();

        var processed = await Eventually.SatisfiesAsync(async () =>
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

            var outbox = await context.OutboxMessages.AsNoTracking()
                .SingleOrDefaultAsync(message => message.Type == "member.registered" && message.Payload.Contains(member.Email), Token);

            return outbox?.ProcessedAt is not null
                && await context.ProcessedMessages.AnyAsync(message => message.MessageId == outbox.Id, Token);
        }, Token);

        Assert.True(processed, "The outbox message was not published and consumed.");
        Assert.Single(Factory.Emails.Messages, message => message.To == member.Email && message.Subject == "Welcome to the library");
    }
}
