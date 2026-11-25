using Library.Application.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Library.Infrastructure.Caching;

public sealed class HybridCatalogCache : ICatalogCache
{
    public const string Tag = "catalog";

    private static readonly string[] Tags = [Tag];

    private readonly HybridCache _cache;
    private readonly CachingSettings _settings;
    private readonly HybridCacheEntryOptions _entryOptions;

    public HybridCatalogCache(HybridCache cache, IOptions<CachingSettings> settings)
    {
        _cache = cache;
        _settings = settings.Value;

        var expiration = TimeSpan.FromSeconds(_settings.CatalogSeconds);
        _entryOptions = new HybridCacheEntryOptions { Expiration = expiration, LocalCacheExpiration = expiration };
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return await factory(cancellationToken);
        }

        return await _cache.GetOrCreateAsync(
            key,
            factory,
            static async (create, token) => await create(token),
            _entryOptions,
            Tags,
            cancellationToken);
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        await _cache.RemoveByTagAsync(Tag, cancellationToken);
    }
}
