using Library.Domain.Entities;

namespace Library.Application.Members;

public static class MemberMappings
{
    public static MemberResponse ToResponse(this Member member) =>
        new(member.Id, member.Name, member.BirthDate, member.Phone, member.Email, member.Role.ToString(), member.IsEmailConfirmed, member.CreatedAt);

    public static string ToDisplayName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            return parts.FirstOrDefault() ?? string.Empty;
        }

        return $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}.";
    }
}
