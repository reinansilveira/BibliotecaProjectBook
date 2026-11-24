using System.Text;
using Library.Application.Books;
using Library.Application.Books.Inventory;
using Library.Application.Common.Csv;
using Library.Application.Members;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Application;

public class CommonTests
{
    [Theory]
    [InlineData("978-0-14-143951-8", true)]
    [InlineData("0-14-143951-3", true)]
    [InlineData("080442957X", true)]
    [InlineData("9780141439519", false)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    public void Isbn_check_digit_is_validated(string isbn, bool expected)
    {
        Assert.Equal(expected, Isbn.IsValid(isbn));
    }

    [Fact]
    public void Isbn_is_normalized_to_digits()
    {
        Assert.Equal("080442957X", Isbn.Normalize(" 0-8044-2957-x "));
        Assert.Null(Isbn.Normalize("  "));
    }

    [Fact]
    public void Csv_escapes_quotes_commas_and_formulas()
    {
        var bytes = CsvWriter.Write(
            [new { Title = "Hello, \"world\"", Note = "=HYPERLINK(\"x\")", Value = -3.5m }],
            ("Title", row => row.Title),
            ("Note", row => row.Note),
            ("Value", row => row.Value));

        var text = Encoding.UTF8.GetString(bytes).TrimStart('﻿');
        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Title,Note,Value", lines[0]);
        Assert.Equal("\"Hello, \"\"world\"\"\",\"'=HYPERLINK(\"\"x\"\")\",-3.5", lines[1]);
    }

    [Fact]
    public void Csv_formats_dates_in_a_stable_way()
    {
        Assert.Equal("2026-03-01 10:30:00", CsvWriter.Escape(new DateTime(2026, 3, 1, 10, 30, 0).ToString("yyyy-MM-dd HH:mm:ss")));
    }

    [Theory]
    [InlineData("book.epub", new byte[] { 0x50, 0x4B, 0x03, 0x04 }, EbookFiles.EpubContentType)]
    [InlineData("book.PDF", new byte[] { 0x25, 0x50, 0x44, 0x46 }, EbookFiles.PdfContentType)]
    public void Ebook_type_comes_from_extension_and_content(string fileName, byte[] content, string expected)
    {
        Assert.Equal(expected, EbookFiles.DetectContentType(fileName, content));
    }

    [Theory]
    [InlineData("book.pdf", new byte[] { 0x50, 0x4B, 0x03, 0x04 })]
    [InlineData("book.exe", new byte[] { 0x4D, 0x5A })]
    [InlineData("book.epub", new byte[0])]
    public void Mismatched_or_empty_ebooks_are_rejected(string fileName, byte[] content)
    {
        Assert.Throws<DomainException>(() => EbookFiles.DetectContentType(fileName, content));
    }

    [Theory]
    [InlineData("Maria Clara Souza", "Maria S.")]
    [InlineData("Ana", "Ana")]
    [InlineData("  ", "")]
    public void Reviewers_are_shown_by_first_name_and_initial(string name, string expected)
    {
        Assert.Equal(expected, MemberMappings.ToDisplayName(name));
    }
}
