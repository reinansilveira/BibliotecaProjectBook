using Library.Domain.Entities;

namespace Library.Application.Blacklist;

internal static class BlacklistMappings
{
    public static BlacklistEntryResponse ToResponse(this BlacklistEntry entry) =>
        new(entry.MemberId, entry.MemberName, entry.AddedAt);
}
