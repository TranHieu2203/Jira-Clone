using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBbStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        StorageOptions snapshot = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
                                  ?? new StorageOptions();

        if (string.Equals(snapshot.Provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                S3StorageOptions s3 = snapshot.S3;
                AmazonS3Config cfg = new()
                {
                    ForcePathStyle = s3.ForcePathStyle,
                    // AuthenticationRegion bắt buộc cho SDK ký request, nhưng ServiceURL override
                    // endpoint resolution → request đi tới LocalStack chứ không AWS public.
                    AuthenticationRegion = string.IsNullOrWhiteSpace(s3.Region) ? "us-east-1" : s3.Region
                };
                // Set ServiceURL SAU AuthenticationRegion để override default region endpoint
                // (nếu set RegionEndpoint trước, SDK pre-resolve URL → redirect 301).
                if (!string.IsNullOrWhiteSpace(s3.ServiceUrl))
                {
                    cfg.ServiceURL = s3.ServiceUrl;
                    // Custom endpoint (LocalStack / MinIO) thường http, không https.
                    cfg.UseHttp = s3.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                }
                else if (!string.IsNullOrWhiteSpace(s3.Region))
                {
                    // Real AWS path: dùng RegionEndpoint.
                    cfg.RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region);
                }

                BasicAWSCredentials creds = new(s3.AccessKey, s3.SecretKey);
                return new AmazonS3Client(creds, cfg);
            });
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        return services;
    }
}
