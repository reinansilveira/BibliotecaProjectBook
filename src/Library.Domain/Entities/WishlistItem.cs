namespace Library.Domain.Entities;

public class WishlistItem
{
    private WishlistItem()
    {
    }

    public WishlistItem(Guid memberId, Guid bookId, DateTime addedAt)
    {
        MemberId = memberId;
        BookId = bookId;
        AddedAt = addedAt;
    }

    public Guid MemberId { get; private set; }

    public Guid BookId { get; private set; }

    public DateTime AddedAt { get; private set; }
}
