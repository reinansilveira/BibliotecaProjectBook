using System.Text.Json;
using Library.Application.Events;

namespace Library.Infrastructure.Messaging;

public static class IntegrationEventRegistry
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<Type, string> NamesByType = new()
    {
        [typeof(MemberRegisteredEvent)] = "member.registered",
        [typeof(EmailConfirmationRequestedEvent)] = "member.email-confirmation-requested",
        [typeof(PasswordResetRequestedEvent)] = "member.password-reset-requested",
        [typeof(LoanCreatedEvent)] = "loan.created",
        [typeof(LoanReturnedEvent)] = "loan.returned",
        [typeof(LoanDueSoonEvent)] = "loan.due-soon",
        [typeof(LoanOverdueEvent)] = "loan.overdue",
        [typeof(ReservationReadyEvent)] = "reservation.ready",
        [typeof(FineIssuedEvent)] = "fine.issued"
    };

    private static readonly Dictionary<string, Type> TypesByName = NamesByType.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static IReadOnlyCollection<string> RoutingKeys => TypesByName.Keys;

    public static string GetName(Type eventType)
    {
        if (NamesByType.TryGetValue(eventType, out var name))
        {
            return name;
        }

        throw new InvalidOperationException($"The event {eventType.Name} is not registered.");
    }

    public static Type? FindType(string name) => TypesByName.GetValueOrDefault(name);
}
