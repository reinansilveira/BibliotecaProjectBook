using Library.Application.Common.Pagination;
using Library.Application.Loans;
using Library.Domain.Entities;
using Library.Domain.Enums;
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

    public Task<LoanView?> GetViewAsync(Guid id, CancellationToken cancellationToken = default) =>
        ToViews(_context.Loans.AsNoTracking().Where(loan => loan.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<LoanView>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default) =>
        _context.Loans
            .AsNoTracking()
            .Where(loan => loan.MemberId == memberId)
            .OrderBy(loan => loan.ReturnedAt != null)
            .ThenByDescending(loan => loan.LoanDate)
            .ThenBy(loan => loan.Id)
            .ToPagedResultAsync(ToViews, request, cancellationToken);

    public Task<int> CountActiveByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        Active(now).CountAsync(loan => loan.MemberId == memberId, cancellationToken);

    public Task<int> CountOverdueByMemberAsync(Guid memberId, DateTime now, CancellationToken cancellationToken = default) =>
        Overdue(now).CountAsync(loan => loan.MemberId == memberId, cancellationToken);

    public Task<bool> HasActiveLoanAsync(Guid memberId, Guid bookId, DateTime now, CancellationToken cancellationToken = default) =>
        Active(now).AnyAsync(loan => loan.MemberId == memberId && loan.BookId == bookId, cancellationToken);

    public Task<bool> HasActiveLoanForBookAsync(Guid bookId, DateTime now, CancellationToken cancellationToken = default) =>
        Active(now).AnyAsync(loan => loan.BookId == bookId, cancellationToken);

    public Task<bool> HasBorrowedAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        _context.Loans.AnyAsync(loan => loan.MemberId == memberId && loan.BookId == bookId, cancellationToken);

    public Task<int> CountActivePrintLoansAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        _context.Loans.CountAsync(loan => loan.BookId == bookId && loan.Format == BookFormat.Print && loan.ReturnedAt == null, cancellationToken);

    public Task<int> CountActiveEbookLoansAsync(Guid bookId, DateTime now, CancellationToken cancellationToken = default) =>
        _context.Loans.CountAsync(
            loan => loan.BookId == bookId && loan.Format == BookFormat.Ebook && loan.ReturnedAt == null && loan.DueDate > now,
            cancellationToken);

    public async Task<IReadOnlyList<Loan>> ListExpiredEbookLoansAsync(DateTime now, CancellationToken cancellationToken = default) =>
        await _context.Loans
            .Where(loan => loan.Format == BookFormat.Ebook && loan.ReturnedAt == null && loan.DueDate <= now)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LoanView>> ListDueSoonWithoutReminderAsync(DateTime now, DateTime until, CancellationToken cancellationToken = default) =>
        await ToViews(Active(now).Where(loan => loan.ReminderSentAt == null && loan.DueDate > now && loan.DueDate <= until))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LoanView>> ListOverdueWithoutNoticeAsync(DateTime now, CancellationToken cancellationToken = default) =>
        await ToViews(Overdue(now).Where(loan => loan.OverdueNoticeSentAt == null)).ToListAsync(cancellationToken);

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default) =>
        await _context.Loans.AddAsync(loan, cancellationToken);

    private IQueryable<Loan> Active(DateTime now) =>
        _context.Loans.AsNoTracking().Where(loan => loan.ReturnedAt == null && (loan.Format == BookFormat.Print || loan.DueDate > now));

    private IQueryable<Loan> Overdue(DateTime now) =>
        _context.Loans.AsNoTracking().Where(loan => loan.Format == BookFormat.Print && loan.ReturnedAt == null && loan.DueDate < now);

    private IQueryable<LoanView> ToViews(IQueryable<Loan> loans) =>
        from loan in loans
        join book in _context.Books on loan.BookId equals book.Id
        join member in _context.Members on loan.MemberId equals member.Id
        select new LoanView(loan, book.Title, book.Author, book.CoverUrl, member.Name, member.Email);
}
