using FluentValidation;
using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Events;
using Library.Domain.Entities;

namespace Library.Application.Members;

public sealed class MemberService : IMemberService
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEventPublisher _events;
    private readonly IValidator<RegisterMemberRequest> _registerValidator;
    private readonly IValidator<UpdateMemberRequest> _updateValidator;
    private readonly TimeProvider _timeProvider;

    public MemberService(
        IMemberRepository members,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEventPublisher events,
        IValidator<RegisterMemberRequest> registerValidator,
        IValidator<UpdateMemberRequest> updateValidator,
        TimeProvider timeProvider)
    {
        _members = members;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _events = events;
        _registerValidator = registerValidator;
        _updateValidator = updateValidator;
        _timeProvider = timeProvider;
    }

    public async Task<MemberResponse> RegisterAsync(RegisterMemberRequest request, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        await EnsureEmailIsAvailableAsync(email, null, cancellationToken);

        var member = new Member(
            request.Name,
            request.BirthDate,
            request.Phone,
            email,
            _passwordHasher.Hash(request.Password),
            _timeProvider.GetUtcNow().UtcDateTime);

        await _members.AddAsync(member, cancellationToken);
        _events.Publish(new MemberRegisteredEvent(member.Id, member.Name, member.Email));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToResponse();
    }

    public async Task<MemberResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await GetActiveMemberAsync(id, cancellationToken);

        return member.ToResponse();
    }

    public async Task<PagedResult<MemberResponse>> ListAsync(SearchPageRequest request, CancellationToken cancellationToken = default)
    {
        await PaginationValidators.Search.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _members.ListAsync(request, cancellationToken);

        return page.Map(member => member.ToResponse());
    }

    public async Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var member = await GetActiveMemberAsync(id, cancellationToken);
        var email = NormalizeEmail(request.Email);
        await EnsureEmailIsAvailableAsync(email, id, cancellationToken);

        member.Update(request.Name, request.BirthDate, request.Phone, email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await GetActiveMemberAsync(id, cancellationToken);

        member.Delete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<Member> GetActiveMemberAsync(Guid id, CancellationToken cancellationToken)
    {
        var member = await _members.GetByIdAsync(id, cancellationToken);

        if (member is null || member.IsDeleted)
        {
            throw new NotFoundException("Member not found.");
        }

        return member;
    }

    private async Task EnsureEmailIsAvailableAsync(string email, Guid? memberId, CancellationToken cancellationToken)
    {
        if (!await _members.IsEmailInUseAsync(email, memberId, cancellationToken))
        {
            return;
        }

        throw new ConflictException("A member with this email already exists.");
    }
}
