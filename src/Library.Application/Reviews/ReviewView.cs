using Library.Application.Members;
using Library.Domain.Entities;

namespace Library.Application.Reviews;

public sealed record ReviewView(Review Review, string MemberName)
{
    public ReviewResponse ToResponse() => new(
        Review.Id,
        Review.BookId,
        Review.MemberId,
        MemberMappings.ToDisplayName(MemberName),
        Review.Rating,
        Review.Comment,
        Review.CreatedAt,
        Review.UpdatedAt);
}
