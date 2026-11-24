using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Book
{
    public const int MaxEbookLicenses = 100;

    private Book()
    {
    }

    public Book(BookDetails details, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        CreatedAt = createdAt;
        Apply(details);
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Genre { get; private set; } = string.Empty;

    public string? Isbn { get; private set; }

    public int? PublishedYear { get; private set; }

    public string? CoverUrl { get; private set; }

    public string? EbookFileKey { get; private set; }

    public int EbookLicenses { get; private set; }

    public decimal AverageRating { get; private set; }

    public int RatingsCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public bool HasEbook => EbookFileKey is not null && EbookLicenses > 0;

    public void Update(BookDetails details)
    {
        Apply(details);
    }

    public void AttachEbook(string fileKey, int licenses)
    {
        EnsureValidLicenses(licenses);
        EbookFileKey = fileKey;
        EbookLicenses = licenses;
    }

    public void ChangeEbookLicenses(int licenses)
    {
        if (EbookFileKey is null)
        {
            throw new DomainException("This book has no e-book.");
        }

        EnsureValidLicenses(licenses);
        EbookLicenses = licenses;
    }

    public void Delete()
    {
        IsDeleted = true;
    }

    private static void EnsureValidLicenses(int licenses)
    {
        if (licenses is >= 1 and <= MaxEbookLicenses)
        {
            return;
        }

        throw new DomainException($"An e-book must have between 1 and {MaxEbookLicenses} licenses.");
    }

    private void Apply(BookDetails details)
    {
        Title = details.Title;
        Author = details.Author;
        Description = details.Description;
        Genre = details.Genre;
        Isbn = details.Isbn;
        PublishedYear = details.PublishedYear;
        CoverUrl = details.CoverUrl;
    }
}
