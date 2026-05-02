using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BB.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly StorageOptions _opts;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<StorageOptions> opts, ILogger<LocalFileStorage> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    private string ResolveRoot()
    {
        string root = Path.IsPathRooted(_opts.LocalRoot)
            ? _opts.LocalRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _opts.LocalRoot));
        return root;
    }

    private string PhysicalPath(string storageKey)
    {
        string safeKey = storageKey.Replace('\\', '/').TrimStart('/');
        string combined = Path.Combine(ResolveRoot(), safeKey.Replace('/', Path.DirectorySeparatorChar));
        string full = Path.GetFullPath(combined);
        string root = Path.GetFullPath(ResolveRoot()) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key path.");
        return full;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(PhysicalPath(storageKey)));
    }

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
    {
        string path = PhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fs, ct);
        _logger.LogDebug("Stored local object {Key}", storageKey);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string path = PhysicalPath(storageKey);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        string path = PhysicalPath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}
