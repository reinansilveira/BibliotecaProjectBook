using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.UnitTests.Domain;

public class ReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_outside_one_to_five_is_rejected(int rating)
    {
        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), rating, "", DateTime.UtcNow));
    }

    [Fact]
    public void Changing_a_review_trims_the_comment_and_updates_the_date()
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), 3, "ok", createdAt);

        review.Change(5, "  Loved it  ", createdAt.AddDays(1));

        Assert.Equal(5, review.Rating);
        Assert.Equal("Loved it", review.Comment);
        Assert.Equal(createdAt.AddDays(1), review.UpdatedAt);
        Assert.Equal(createdAt, review.CreatedAt);
    }
}
