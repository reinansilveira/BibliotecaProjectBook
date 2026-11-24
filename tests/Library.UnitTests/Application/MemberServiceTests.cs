using Library.Application.Abstractions;
using Library.Application.Common.Exceptions;
using Library.Application.Events;
using Library.Application.Members;
using Library.Domain.Entities;
using Library.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Library.UnitTests.Application;

public class MemberServiceTests
{
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly RecordingEventPublisher _events = new();
    private readonly MemberService _service;

    public MemberServiceTests()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _passwordHasher.Hash(Arg.Any<string>()).Returns("secure-hash");

        _service = new MemberService(
            _members,
            _unitOfWork,
            _passwordHasher,
            _events,
            new RegisterMemberRequestValidator(clock),
            new UpdateMemberRequestValidator(clock),
            clock);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Stores_only_the_password_hash_and_normalizes_the_email()
    {
        var request = TestData.RegisterMemberRequest() with { Email = "  Ana.Smith@Email.COM " };

        var response = await _service.RegisterAsync(request, Token);

        Assert.Equal("ana.smith@email.com", response.Email);
        await _members.Received(1).AddAsync(
            Arg.Is<Member>(member => member.PasswordHash == "secure-hash" && member.PasswordHash != request.Password),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Registration_publishes_a_member_registered_event()
    {
        var response = await _service.RegisterAsync(TestData.RegisterMemberRequest(), Token);

        var registered = Assert.IsType<MemberRegisteredEvent>(Assert.Single(_events.Events));
        Assert.Equal(response.Id, registered.MemberId);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        _members.IsEmailInUseAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(TestData.RegisterMemberRequest(), Token));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Deleted_member_is_not_found()
    {
        var member = TestData.Member();
        member.Delete();
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(member.Id, Token));
    }
}
