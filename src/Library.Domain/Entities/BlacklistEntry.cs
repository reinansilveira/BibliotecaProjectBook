namespace Library.Domain.Entities;

public class BlacklistEntry
{
    private BlacklistEntry()
    {
    }

    public BlacklistEntry(Guid memberId, string memberName, DateTime addedAt)
    {
        Id = Guid.NewGuid();
        MemberId = memberId;
        MemberName = memberName;
        AddedAt = addedAt;
    }

    public Guid Id { get; private set; }

    public Guid MemberId { get; private set; }

    public string MemberName { get; private set; } = string.Empty;

    public DateTime AddedAt { get; private set; }
}
