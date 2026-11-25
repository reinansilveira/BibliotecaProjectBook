using Library.Domain.Exceptions;

namespace Library.Application.Books.Inventory;

public static class EbookFiles
{
    public const long MaxSizeInBytes = 25 * 1024 * 1024;
    public const string EpubContentType = "application/epub+zip";
    public const string PdfContentType = "application/pdf";

    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46];

    public static string DetectContentType(string fileName, byte[] content)
    {
        if (content.LongLength == 0 || content.LongLength > MaxSizeInBytes)
        {
            throw new DomainException($"The e-book must have between 1 byte and {MaxSizeInBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension == ".epub" && content.AsSpan().StartsWith(ZipSignature))
        {
            return EpubContentType;
        }

        if (extension == ".pdf" && content.AsSpan().StartsWith(PdfSignature))
        {
            return PdfContentType;
        }

        throw new DomainException("Only EPUB and PDF files are accepted, and the content must match the extension.");
    }

    public static string ExtensionFor(string contentType) => contentType == PdfContentType ? ".pdf" : ".epub";
}
