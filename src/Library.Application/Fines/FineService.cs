using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Inbox;
using Library.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Library.Application.Fines;

public sealed class FineService : IFineService
{
    private readonly IFineRepository _fines;
    private readonly IInbox _inbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FineSettings _settings;
    private readonly TimeProvider _timeProvider;

    public FineService(IFineRepository fines, IInbox inbox, IUnitOfWork unitOfWork, IOptions<FineSettings> settings, TimeProvider timeProvider)
    {
        _fines = fines;
        _inbox = inbox;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<FineResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var view = await _fines.GetViewAsync(id, cancellationToken) ?? throw new NotFoundException("Fine not found.");

        return view.ToResponse(_settings.Currency);
    }

    public async Task<PagedResult<FineResponse>> ListAsync(Guid? memberId, FineListRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _fines.ListAsync(memberId, request, cancellationToken);

        return page.Map(view => view.ToResponse(_settings.Currency));
    }

    public Task<FineResponse> PayAsync(Guid id, CancellationToken cancellationToken = default) =>
        SettleAsync(id, fine => fine.Pay($"SIM-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Now), "Payment received", cancellationToken);

    public Task<FineResponse> WaiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        SettleAsync(id, fine => fine.Waive(Now), "Fine waived", cancellationToken);

    private async Task<FineResponse> SettleAsync(Guid id, Action<Fine> settle, string title, CancellationToken cancellationToken)
    {
        var fine = await _fines.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Fine not found.");

        settle(fine);
        _inbox.Add(fine.MemberId, NotificationTypes.FineSettled, title, $"Your fine of {_settings.Currency} {fine.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} is settled.", "/account/fines");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }
}
