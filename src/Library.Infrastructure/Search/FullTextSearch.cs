using System.Text.RegularExpressions;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Library.Infrastructure.Search;

public sealed partial class FullTextSearch
{
    private const int MaxWords = 5;

    private readonly bool _enabled;
    private bool? _available;

    public FullTextSearch(IOptions<SearchSettings> settings)
    {
        _enabled = settings.Value.UseFullText;
    }

    public async Task<bool> IsAvailableAsync(LibraryDbContext context, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
        {
            return false;
        }

        if (_available.HasValue)
        {
            return _available.Value;
        }

        var hasIndex = await context.Database
            .SqlQueryRaw<int>("SELECT CAST(COALESCE(OBJECTPROPERTYEX(OBJECT_ID('dbo.Books'), 'TableHasActiveFulltextIndex'), 0) AS INT) AS [Value]")
            .SingleAsync(cancellationToken);

        _available = hasIndex == 1;

        return _available.Value;
    }

    public static string? BuildPrefixCondition(string term)
    {
        var words = WordPattern().Matches(term).Select(match => match.Value).Take(MaxWords).ToList();

        if (words.Count == 0)
        {
            return null;
        }

        return string.Join(" AND ", words.Select(word => $"\"{word}*\""));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordPattern();
}
