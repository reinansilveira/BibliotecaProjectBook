namespace Library.Application.Blacklist;

public sealed record BlacklistEntryResponse(Guid MemberId, string MemberName, DateTime AddedAt);
