namespace Library.Application.Abstractions;

public interface IFileStorage
{
    Task SaveAsync(string key, FileContent file, CancellationToken cancellationToken = default);

    Task<FileContent?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
