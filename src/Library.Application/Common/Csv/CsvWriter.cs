using System.Globalization;
using System.Text;

namespace Library.Application.Common.Csv;

public static class CsvWriter
{
    private static readonly char[] CharactersThatNeedQuotes = [',', '"', '\n', '\r'];
    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@', '\t', '\r'];

    public static byte[] Write<T>(IEnumerable<T> rows, params (string Header, Func<T, object?> Value)[] columns)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', columns.Select(column => Escape(column.Header))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', columns.Select(column => Escape(Format(column.Value(row))))));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    public static string Escape(string value)
    {
        var safe = value.Length > 0 && FormulaPrefixes.Contains(value[0]) && !IsNumber(value) ? $"'{value}" : value;

        if (safe.IndexOfAny(CharactersThatNeedQuotes) < 0)
        {
            return safe;
        }

        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static bool IsNumber(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
}
