namespace Library.Domain.Entities;

public class BookCopy
{
    private BookCopy()
    {
    }

    public BookCopy(Guid bookId, string barcode, DateTime addedAt)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        Barcode = barcode;
        AddedAt = addedAt;
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public DateTime AddedAt { get; private set; }

    public DateTime? WithdrawnAt { get; private set; }

    public bool IsWithdrawn => WithdrawnAt.HasValue;

    public void Withdraw(DateTime now)
    {
        WithdrawnAt ??= now;
    }
}
