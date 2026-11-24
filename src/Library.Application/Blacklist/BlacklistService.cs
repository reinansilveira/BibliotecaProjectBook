using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Events;
using Library.Application.Loans;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Library.Application.Blacklist;

public sealed class BlacklistService : IBlacklistService
{
    private readonly ILoanRepository _loans;
    private readonly IMemberRepository _members;
    private readonly IBlacklistRepository _blacklist;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BlacklistService> _logger;

    public BlacklistService(
        ILoanRepository loans,
        IMemberRepository members,
        IBlacklistRepository blacklist,
        IUnitOfWork unitOfWork,
        IEventPublisher events,
        TimeProvider timeProvider,
        ILogger<BlacklistService> logger)
    {
        _loans = loans;
        _members = members;
        _blacklist = blacklist;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var overdueLoans = await _loans.ListOverdueAsync(now, cancellationToken);
        var memberIds = overdueLoans.Select(loan => loan.MemberId).Distinct().ToList();
        var members = await _members.ListByIdsAsync(memberIds, cancellationToken);
        var added = 0;

        foreach (var member in members)
        {
            if (await _blacklist.IsBlacklistedAsync(member.Id, cancellationToken))
            {
                continue;
            }

            await _blacklist.AddAsync(new BlacklistEntry(member.Id, member.Name, now), cancellationToken);
            _events.Publish(new MemberBlacklistedEvent(member.Id, member.Name, member.Email));
            added++;
        }

        if (added == 0)
        {
            return 0;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} member(s) added to the blacklist due to overdue returns.", added);

        return added;
    }

    public async Task<PagedResult<BlacklistEntryResponse>> ListAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _blacklist.ListAsync(request, cancellationToken);

        return page.Map(entry => entry.ToResponse());
    }

    public async Task RemoveAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var entry = await _blacklist.GetByMemberIdAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Member is not on the blacklist.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (await _loans.HasOverdueLoanAsync(memberId, now, cancellationToken))
        {
            throw new DomainException("The member still has an overdue loan. Register the return before removing them from the blacklist.");
        }

        _blacklist.Remove(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
