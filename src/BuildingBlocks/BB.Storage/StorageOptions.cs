namespace BB.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Local | S3</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Thư mục gốc (relative tới BaseDirectory hoặc absolute).</summary>
    public string LocalRoot { get; set; } = Path.Combine("App_Data", "storage");

    public S3StorageOptions S3 { get; set; } = new();

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed class S3StorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
    public string? Region { get; set; }
}
