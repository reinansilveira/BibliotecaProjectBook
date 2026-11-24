using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Blacklist;
using Library.Application.Books;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Events;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Loans;

public sealed class LoanService : ILoanService
{
    private readonly ILoanRepository _loans;
    private readonly IBookRepository _books;
    private readonly IMemberRepository _members;
    private readonly IBlacklistRepository _blacklist;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _events;
    private readonly IValidator<CreateLoanRequest> _validator;
    private readonly TimeProvider _timeProvider;

    public LoanService(
        ILoanRepository loans,
        IBookRepository books,
        IMemberRepository members,
        IBlacklistRepository blacklist,
        IUnitOfWork unitOfWork,
        IEventPublisher events,
        IValidator<CreateLoanRequest> validator,
        TimeProvider timeProvider)
    {
        _loans = loans;
        _books = books;
        _members = members;
        _blacklist = blacklist;
        _unitOfWork = unitOfWork;
        _events = events;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<LoanResponse> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var member = await GetActiveMemberAsync(request.MemberId, cancellationToken);
        var book = await GetActiveBookAsync(request.BookId, cancellationToken);

        if (await _blacklist.IsBlacklistedAsync(member.Id, cancellationToken))
        {
            throw new DomainException("The member is blacklisted due to an overdue return and cannot borrow books.");
        }

        if (await _loans.HasActiveLoanAsync(member.Id, cancellationToken))
        {
            throw new DomainException("The member already has an active loan.");
        }

        if (await _loans.IsBookOnLoanAsync(book.Id, cancellationToken))
        {
            throw new DomainException("The book is already on loan.");
        }

        var loan = new Loan(book.Id, member.Id, Now);

        await _loans.AddAsync(loan, cancellationToken);
        _events.Publish(new LoanCreatedEvent(loan.Id, member.Id, member.Name, member.Email, book.Title, loan.DueDate));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return loan.ToResponse(Now);
    }

    public async Task<LoanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await GetLoanAsync(id, cancellationToken);

        return loan.ToResponse(Now);
    }

    public async Task<PagedResult<LoanResponse>> ListByMemberAsync(Guid memberId, PageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Page.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _loans.ListByMemberAsync(memberId, request, cancellationToken);
        var now = Now;

        return page.Map(loan => loan.ToResponse(now));
    }

    public async Task<LoanResponse> ReturnAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await GetLoanAsync(id, cancellationToken);
        var member = await _members.GetByIdAsync(loan.MemberId, cancellationToken) ?? throw new NotFoundException("Member not found.");
        var book = await _books.GetByIdAsync(loan.BookId, cancellationToken) ?? throw new NotFoundException("Book not found.");
        var now = Now;
        var wasOverdue = loan.IsOverdue(now);

        loan.Return(now);
        _events.Publish(new LoanReturnedEvent(loan.Id, member.Id, member.Name, member.Email, book.Title, now, wasOverdue));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return loan.ToResponse(now);
    }

    private async Task<Loan> GetLoanAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _loans.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Loan not found.");
    }

    private async Task<Member> GetActiveMemberAsync(Guid id, CancellationToken cancellationToken)
    {
        var member = await _members.GetByIdAsync(id, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            throw new NotFoundException("Member not found.");
        }

        return member;
    }

    private async Task<Book> GetActiveBookAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(id, cancellationToken);

        if (book is null || book.IsDeleted)
        {
            throw new NotFoundException("Book not found.");
        }

        return book;
    }
}
