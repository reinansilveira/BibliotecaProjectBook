using System.Data.Common;
using Library.Application.Abstractions;
using Library.Application.Inbox;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Notifications;

public sealed class NotificationDispatchInterceptor : ISaveChangesInterceptor, IDbTransactionInterceptor
{
    private readonly List<Notification> _saving = [];
    private readonly List<Notification> _pending = [];
    private readonly IRealtimeNotifier _notifier;
    private readonly ILogger<NotificationDispatchInterceptor> _logger;

    public NotificationDispatchInterceptor(IRealtimeNotifier notifier, ILogger<NotificationDispatchInterceptor> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _saving.Clear();

        if (eventData.Context is not null)
        {
            _saving.AddRange(eventData.Context.ChangeTracker.Entries<Notification>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity));
        }

        return ValueTask.FromResult(result);
    }

    public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _pending.AddRange(_saving);
        _saving.Clear();

        if (eventData.Context?.Database.CurrentTransaction is null)
        {
            await FlushAsync(cancellationToken);
        }

        return result;
    }

    public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _saving.Clear();

        return Task.CompletedTask;
    }

    public async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await FlushAsync(cancellationToken);
    }

    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _pending.Clear();

        return Task.CompletedTask;
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var batch = _pending.ToList();
        _pending.Clear();

        foreach (var notification in batch)
        {
            try
            {
                await _notifier.SendAsync(notification.MemberId, notification.ToResponse(), cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Could not push notification {NotificationId} in real time.", notification.Id);
            }
        }
    }
}
