using Library.Application.Common.Pagination;
using Library.Application.Fines;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class FineRepository : IFineRepository
{
    private readonly LibraryDbContext _context;

    public FineRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Fine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Fines.FirstOrDefaultAsync(fine => fine.Id == id, cancellationToken);

    public Task<FineView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default) =>
        ToViews(_context.Fines.AsNoTracking().Where(fine => fine.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<FineView>> ListAsync(Guid? memberId, FineListRequest request, CancellationToken cancellationToken = default)
    {
        var fines = _context.Fines.AsNoTracking();

        if (memberId.HasValue)
        {
            fines = fines.Where(fine => fine.MemberId == memberId);
        }

        if (request.Status.HasValue)
        {
            fines = fines.Where(fine => fine.Status == request.Status);
        }

        return fines
            .OrderBy(fine => fine.Status != FineStatus.Unpaid)
            .ThenByDescending(fine => fine.IssuedAt)
            .ThenBy(fine => fine.Id)
            .ToPagedResultAsync(ToViews, request, cancellationToken);
    }

    public async Task<decimal> SumUnpaidAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await _context.Fines
            .Where(fine => fine.MemberId == memberId && fine.Status == FineStatus.Unpaid)
            .SumAsync(fine => (decimal?)fine.Amount, cancellationToken) ?? 0m;

    public async Task AddAsync(Fine fine, CancellationToken cancellationToken = default) =>
        await _context.Fines.AddAsync(fine, cancellationToken);

    private IQueryable<FineView> ToViews(IQueryable<Fine> fines) =>
        from fine in fines
        join member in _context.Members on fine.MemberId equals member.Id
        join loan in _context.Loans on fine.LoanId equals loan.Id
        join book in _context.Books on loan.BookId equals book.Id
        select new FineView(fine, member.Name, book.Title);
}
