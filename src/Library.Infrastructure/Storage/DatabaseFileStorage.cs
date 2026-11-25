using Library.Application.Abstractions;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Storage;

public sealed class DatabaseFileStorage : IFileStorage
{
    private readonly LibraryDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DatabaseFileStorage(LibraryDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public Task SaveAsync(string key, FileContent file, CancellationToken cancellationToken = default)
    {
        _context.StoredFiles.Add(new StoredFile(key, file.FileName, file.ContentType, file.Content, _timeProvider.GetUtcNow().UtcDateTime));

        return Task.CompletedTask;
    }

    public Task<FileContent?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        _context.StoredFiles
            .AsNoTracking()
            .Where(file => file.Key == key)
            .Select(file => new FileContent(file.FileName, file.ContentType, file.Content))
            .FirstOrDefaultAsync(cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        _context.StoredFiles.Where(file => file.Key == key).ExecuteDeleteAsync(cancellationToken);
}
