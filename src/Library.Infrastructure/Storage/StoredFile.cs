namespace Library.Infrastructure.Storage;

public sealed class StoredFile
{
    private StoredFile()
    {
    }

    public StoredFile(string key, string fileName, string contentType, byte[] content, DateTime createdAt)
    {
        Key = key;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        Size = content.LongLength;
        CreatedAt = createdAt;
    }

    public string Key { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public byte[] Content { get; private set; } = [];

    public long Size { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
