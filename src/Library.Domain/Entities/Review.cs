using Library.Domain.Exceptions;

namespace Library.Domain.Entities;

public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    private Review()
    {
    }

    public Review(Guid bookId, Guid memberId, int rating, string comment, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        MemberId = memberId;
        CreatedAt = createdAt;
        Change(rating, comment, createdAt);
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Change(int rating, string comment, DateTime now)
    {
        if (rating is < MinRating or > MaxRating)
        {
            throw new DomainException($"The rating must be between {MinRating} and {MaxRating}.");
        }

        Rating = rating;
        Comment = comment.Trim();
        UpdatedAt = now;
    }
}
