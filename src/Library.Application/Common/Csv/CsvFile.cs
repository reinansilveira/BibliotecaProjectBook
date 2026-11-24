namespace Library.Application.Common.Csv;

public sealed record CsvFile(string FileName, byte[] Content)
{
    public const string ContentType = "text/csv";
}
