using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BB.Storage;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _opts;
    private readonly ILogger<S3FileStorage> _logger;

    public S3FileStorage(IAmazonS3 s3, IOptions<StorageOptions> opts, ILogger<S3FileStorage> logger)
    {
        _s3 = s3;
        _opts = opts.Value;
        _logger = logger;
    }

    private string Bucket => _opts.S3.Bucket;

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(Bucket, storageKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task PutAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default)
    {
        PutObjectRequest req = new()
        {
            BucketName = Bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            AutoCloseStream = false
        };
        await _s3.PutObjectAsync(req, ct);
        _logger.LogDebug("Stored S3 object {Key}", storageKey);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            GetObjectResponse resp = await _s3.GetObjectAsync(Bucket, storageKey, ct);
            return resp.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(Bucket, storageKey, ct);
    }
}
