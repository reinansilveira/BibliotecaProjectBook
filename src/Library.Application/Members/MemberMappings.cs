using Library.Domain.Entities;

namespace Library.Application.Members;

internal static class MemberMappings
{
    public static MemberResponse ToResponse(this Member member) =>
        new(member.Id, member.Name, member.BirthDate, member.Phone, member.Email, member.Role.ToString(), member.CreatedAt);
}
