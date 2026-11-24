namespace Library.Api.Contracts;

public sealed class EbookUploadForm
{
    public required IFormFile File { get; init; }

    public int Licenses { get; init; } = 2;
}
