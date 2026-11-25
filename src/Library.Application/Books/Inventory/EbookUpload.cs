namespace Library.Application.Books.Inventory;

public sealed record EbookUpload(string FileName, byte[] Content, int Licenses);
