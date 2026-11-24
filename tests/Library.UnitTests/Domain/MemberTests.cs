using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Domain;

public class MemberTests
{
    [Fact]
    public void New_member_has_the_member_role_by_default()
    {
        var member = new Member("Ana", new DateTime(1990, 1, 1), "555-0100", "ana@email.com", "hash", DateTime.UtcNow);

        Assert.Equal(Role.Member, member.Role);
        Assert.False(member.IsDeleted);
    }

    [Fact]
    public void Deleting_a_member_marks_it_as_deleted()
    {
        var member = new Member("Ana", new DateTime(1990, 1, 1), "555-0100", "ana@email.com", "hash", DateTime.UtcNow);

        member.Delete();

        Assert.True(member.IsDeleted);
    }
}
