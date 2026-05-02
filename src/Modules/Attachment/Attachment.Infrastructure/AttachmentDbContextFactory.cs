using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Attachment.Infrastructure;

public sealed class AttachmentDbContextFactory : IDesignTimeDbContextFactory<AttachmentDbContext>
{
    public AttachmentDbContext CreateDbContext(string[] args)
    {
        string provider = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "Postgres";
        string connStr = Environment.GetEnvironmentVariable("DB_CONNECTION") ??
                         (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase)
                             ? "User Id=jira_clone;Password=jira_clone;Data Source=localhost:1521/FREEPDB1"
                             : "Host=localhost;Port=5432;Database=jira_clone;Username=jira_clone;Password=jira_clone");

        IConfigurationRoot cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["Database:ConnectionString"] = connStr,
                ["Database:CommandTimeout"] = "30"
            }).Build();

        DbContextOptionsBuilder<AttachmentDbContext> builder = new();
        builder.UseConfiguredDatabase(cfg, migrationsAssembly: typeof(AttachmentDbContextFactory).Assembly.GetName().Name);

        return new AttachmentDbContext(builder.Options);
    }
}
