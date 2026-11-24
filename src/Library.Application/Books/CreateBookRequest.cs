namespace Library.Application.Books;

public sealed record CreateBookRequest : BookRequest
{
    public int Copies { get; init; } = 1;
}
