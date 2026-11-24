using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories;

public sealed class LoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _context;

    public LoanRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Loans.FirstOrDefaultAsync(loan => loan.Id == id, cancellationToken);

    public Task<PagedResult<Loan>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default) =>
        _context.Loans
            .AsNoTracking()
            .Where(loan => loan.MemberId == memberId)
            .OrderByDescending(loan => loan.LoanDate)
            .ThenBy(loan => loan.Id)
            .ToPagedResultAsync(request, cancellationToken);

    public async Task<IReadOnlyList<Loan>> ListOverdueAsync(DateTime now, CancellationToken cancellationToken = default) =>
        await _context.Loans
            .AsNoTracking()
            .Where(loan => loan.ReturnedAt == null && loan.DueDate < now)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveLoanAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        _context.Loans.AnyAsync(loan => loan.MemberId == memberId && loan.ReturnedAt == null, cancellationToken);

    public Task<bool> HasOverdueLoanAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        _context.Loans.AnyAsync(
            loan => loan.MemberId == memberId && loan.ReturnedAt == null && loan.DueDate < now,
            cancellationToken);

    public Task<bool> IsBookOnLoanAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        _context.Loans.AnyAsync(loan => loan.BookId == bookId && loan.ReturnedAt == null, cancellationToken);

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default) =>
        await _context.Loans.AddAsync(loan, cancellationToken);
}
