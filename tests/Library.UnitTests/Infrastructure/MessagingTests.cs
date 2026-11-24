using System.Text.Json;
using Library.Application.Events;
using Library.Infrastructure.Messaging;

namespace Library.UnitTests.Infrastructure;

public class MessagingTests
{
    [Theory]
    [InlineData(typeof(MemberRegisteredEvent), "member.registered")]
    [InlineData(typeof(EmailConfirmationRequestedEvent), "member.email-confirmation-requested")]
    [InlineData(typeof(PasswordResetRequestedEvent), "member.password-reset-requested")]
    [InlineData(typeof(LoanCreatedEvent), "loan.created")]
    [InlineData(typeof(LoanReturnedEvent), "loan.returned")]
    [InlineData(typeof(LoanDueSoonEvent), "loan.due-soon")]
    [InlineData(typeof(LoanOverdueEvent), "loan.overdue")]
    [InlineData(typeof(ReservationReadyEvent), "reservation.ready")]
    [InlineData(typeof(FineIssuedEvent), "fine.issued")]
    public void Every_event_has_a_routing_key_that_maps_back_to_its_type(Type eventType, string routingKey)
    {
        Assert.Equal(routingKey, IntegrationEventRegistry.GetName(eventType));
        Assert.Equal(eventType, IntegrationEventRegistry.FindType(routingKey));
    }

    [Fact]
    public void Every_integration_event_is_registered()
    {
        var eventTypes = typeof(IIntegrationEvent).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IIntegrationEvent).IsAssignableFrom(type));

        Assert.All(eventTypes, type => Assert.NotNull(IntegrationEventRegistry.GetName(type)));
    }

    [Fact]
    public void Events_survive_a_serialization_round_trip()
    {
        var original = new LoanCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Ana", "ana@email.com", "Dune", new DateTime(2026, 3, 8), true);

        var json = JsonSerializer.Serialize(original, IntegrationEventRegistry.SerializerOptions);
        var copy = JsonSerializer.Deserialize<LoanCreatedEvent>(json, IntegrationEventRegistry.SerializerOptions);

        Assert.Equal(original, copy);
    }

    [Fact]
    public void Unknown_routing_key_has_no_type()
    {
        Assert.Null(IntegrationEventRegistry.FindType("unknown.event"));
    }

    [Fact]
    public void Failed_publish_increments_attempts_and_truncates_the_error()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "loan.created", "{}", DateTime.UtcNow);

        message.RegisterFailure(new string('x', 5000));

        Assert.Equal(1, message.Attempts);
        Assert.Equal(OutboxMessage.MaxErrorLength, message.LastError!.Length);
    }

    [Fact]
    public void Processed_message_clears_the_last_error()
    {
        var message = new OutboxMessage(Guid.NewGuid(), "loan.created", "{}", DateTime.UtcNow);
        message.RegisterFailure("broker unavailable");

        message.MarkAsProcessed(DateTime.UtcNow);

        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.LastError);
    }
}
