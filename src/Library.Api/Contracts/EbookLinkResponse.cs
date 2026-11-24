namespace Library.Api.Contracts;

public sealed record EbookLinkResponse(string Url, DateTime ExpiresAt, string FileFormat);
