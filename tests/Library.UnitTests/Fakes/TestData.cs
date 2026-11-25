using Bogus;
using Library.Application.Members;
using Library.Domain.Entities;

namespace Library.UnitTests.Fakes;

public static class TestData
{
    private static readonly Faker Faker = new("en");

    public static BookDetails BookDetails() =>
        new(Faker.Lorem.Sentence(3), Faker.Name.FullName(), Faker.Lorem.Paragraph(), "Fiction", null, 1990, null);

    public static Book Book() => new(BookDetails(), DateTime.UtcNow);

    public static Book EbookBook(int licenses = 2)
    {
        var book = Book();
        book.AttachEbook($"ebooks/{book.Id:N}/file.epub", licenses);

        return book;
    }

    public static Member Member() =>
        new(Faker.Name.FullName(), new DateTime(1995, 5, 10), Faker.Phone.PhoneNumber("###-###-####"), Faker.Internet.Email(), "hash", DateTime.UtcNow);

    public static RegisterMemberRequest RegisterMemberRequest() =>
        new(Faker.Name.FullName(), new DateTime(1995, 5, 10), Faker.Phone.PhoneNumber("###-###-####"), Faker.Internet.Email(), Faker.Internet.Password(10));
}
