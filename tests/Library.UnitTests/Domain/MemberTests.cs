using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Domain;

public class MemberTests
{
    private static Member NewMember() => new("Ana", new DateTime(1990, 1, 1), "555-0100", "ana@email.com", "hash", DateTime.UtcNow);

    [Fact]
    public void New_member_has_the_member_role_and_an_unconfirmed_email()
    {
        var member = NewMember();

        Assert.Equal(Role.Member, member.Role);
        Assert.False(member.IsDeleted);
        Assert.False(member.IsEmailConfirmed);
    }

    [Fact]
    public void Deleting_a_member_marks_it_as_deleted()
    {
        var member = NewMember();

        member.Delete();

        Assert.True(member.IsDeleted);
    }

    [Fact]
    public void Changing_the_email_asks_for_a_new_confirmation()
    {
        var member = NewMember();
        member.ConfirmEmail(DateTime.UtcNow);

        member.Update("Ana", new DateTime(1990, 1, 1), "555-0100", "ana@email.com");
        Assert.True(member.IsEmailConfirmed);

        member.Update("Ana", new DateTime(1990, 1, 1), "555-0100", "ana.new@email.com");
        Assert.False(member.IsEmailConfirmed);
    }

    [Fact]
    public void Changing_the_password_replaces_the_hash()
    {
        var member = NewMember();

        member.ChangePassword("new-hash");

        Assert.Equal("new-hash", member.PasswordHash);
    }
}
