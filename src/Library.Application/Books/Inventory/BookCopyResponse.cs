namespace Library.Application.Books.Inventory;

public sealed record BookCopyResponse(Guid Id, string Barcode, DateTime AddedAt, DateTime? WithdrawnAt, bool IsOnLoan);
