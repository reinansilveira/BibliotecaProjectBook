using System.Text.Json;
using Library.Application.Abstractions;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Library.Infrastructure.Auditing;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private const string Masked = "***";

    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Book), typeof(BookCopy), typeof(Member), typeof(Loan), typeof(Reservation), typeof(Fine), typeof(Review)
    ];

    private static readonly HashSet<string> MaskedProperties = [nameof(Member.PasswordHash)];

    private static readonly HashSet<string> IgnoredProperties =
    [
        nameof(Loan.ReadingLocation), nameof(Loan.ReadingProgress), nameof(Loan.ReminderSentAt), nameof(Loan.OverdueNoticeSentAt)
    ];

    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            Capture(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            Capture(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void Capture(DbContext context)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var entries = context.ChangeTracker.Entries()
            .Where(entry => AuditedTypes.Contains(entry.Metadata.ClrType) && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => Describe(entry, now))
            .OfType<AuditEntry>()
            .ToList();

        context.Set<AuditEntry>().AddRange(entries);
    }

    private AuditEntry? Describe(EntityEntry entry, DateTime now)
    {
        var changes = Changes(entry);

        if (entry.State == EntityState.Modified && changes.Count == 0)
        {
            return null;
        }

        var entityType = entry.Metadata.ClrType.Name;

        return new AuditEntry(
            now,
            _currentUser.MemberId,
            _currentUser.Name,
            $"{entityType}.{Verb(entry)}",
            entityType,
            string.Join(',', entry.Properties.Where(property => property.Metadata.IsPrimaryKey()).Select(property => property.CurrentValue)),
            JsonSerializer.Serialize(changes));
    }

    private static Dictionary<string, object?> Changes(EntityEntry entry)
    {
        var properties = entry.Properties.Where(property => !IgnoredProperties.Contains(property.Metadata.Name));

        if (entry.State == EntityState.Added)
        {
            return properties.ToDictionary(property => property.Metadata.Name, property => Value(property, property.CurrentValue));
        }

        if (entry.State == EntityState.Modified)
        {
            return properties
                .Where(property => property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                .ToDictionary(
                    property => property.Metadata.Name,
                    property => (object?)new Dictionary<string, object?>
                    {
                        ["from"] = Value(property, property.OriginalValue),
                        ["to"] = Value(property, property.CurrentValue)
                    });
        }

        return new Dictionary<string, object?>();
    }

    private static object? Value(PropertyEntry property, object? value) =>
        MaskedProperties.Contains(property.Metadata.Name) ? Masked : value?.ToString();

    private static string Verb(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return "Created";
        }

        if (entry.State == EntityState.Deleted || BecameTrue(entry, "IsDeleted"))
        {
            return "Deleted";
        }

        if (BecameSet(entry, nameof(Loan.ReturnedAt)))
        {
            return "Returned";
        }

        if (BecameSet(entry, nameof(BookCopy.WithdrawnAt)))
        {
            return "Withdrawn";
        }

        var status = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Status" && property.IsModified);

        return status?.CurrentValue?.ToString() ?? "Updated";
    }

    private static bool BecameTrue(EntityEntry entry, string propertyName) =>
        entry.Properties.Any(property => property.Metadata.Name == propertyName && property.IsModified && Equals(property.CurrentValue, true));

    private static bool BecameSet(EntityEntry entry, string propertyName) =>
        entry.Properties.Any(property => property.Metadata.Name == propertyName && property.IsModified && property.OriginalValue is null && property.CurrentValue is not null);
}
