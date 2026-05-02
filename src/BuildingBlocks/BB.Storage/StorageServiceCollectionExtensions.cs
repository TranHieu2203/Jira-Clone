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
                    ServiceURL = s3.ServiceUrl,
                    ForcePathStyle = s3.ForcePathStyle
                };
                if (!string.IsNullOrWhiteSpace(s3.Region))
                    cfg.RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region);

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
