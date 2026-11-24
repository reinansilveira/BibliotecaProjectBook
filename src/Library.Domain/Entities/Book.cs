namespace Library.Domain.Entities;

public class Book
{
    private Book()
    {
    }

    public Book(string title, string author, string description, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        Title = title;
        Author = author;
        Description = description;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public void Update(string title, string author, string description)
    {
        Title = title;
        Author = author;
        Description = description;
    }

    public void Delete()
    {
        IsDeleted = true;
    }
}
