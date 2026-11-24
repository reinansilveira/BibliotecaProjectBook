using System.Text;

namespace Library.IntegrationTests.Infrastructure;

public static class TestFiles
{
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    public static byte[] Epub(string content) => [.. ZipSignature, .. Encoding.UTF8.GetBytes(content)];
}
