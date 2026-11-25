using Library.Application.Abstractions;

namespace Library.UnitTests.Fakes;

public sealed class PassThroughCatalogCache : ICatalogCache
{
    public int Invalidations { get; private set; }

    public List<string> Keys { get; } = [];

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        Keys.Add(key);

        return factory(cancellationToken);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        Invalidations++;

        return Task.CompletedTask;
    }
}
