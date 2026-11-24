namespace Library.Application.Books.Inventory;

public sealed record GutenbergImportRequest(int GutenbergId, int Licenses = 2);
