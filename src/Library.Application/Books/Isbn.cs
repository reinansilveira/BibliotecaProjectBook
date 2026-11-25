namespace Library.Application.Books;

public static class Isbn
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new string(value.Where(character => char.IsDigit(character) || character is 'x' or 'X').ToArray()).ToUpperInvariant();
    }

    public static bool IsValid(string? value)
    {
        var isbn = Normalize(value);

        return isbn?.Length switch
        {
            10 => IsValidIsbn10(isbn),
            13 => IsValidIsbn13(isbn),
            _ => false
        };
    }

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;

        for (var index = 0; index < 10; index++)
        {
            var character = isbn[index];

            if (character == 'X' && index != 9)
            {
                return false;
            }

            var digit = character == 'X' ? 10 : character - '0';
            sum += digit * (10 - index);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Contains('X'))
        {
            return false;
        }

        var sum = isbn.Select((character, index) => (character - '0') * (index % 2 == 0 ? 1 : 3)).Sum();

        return sum % 10 == 0;
    }
}
