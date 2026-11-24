namespace Library.Application.Abstractions;

public sealed record FileContent(string FileName, string ContentType, byte[] Content);
