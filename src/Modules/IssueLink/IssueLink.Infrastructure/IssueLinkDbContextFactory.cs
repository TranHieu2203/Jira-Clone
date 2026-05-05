using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IssueLink.Infrastructure;

public sealed class IssueLinkDbContextFactory : IDesignTimeDbContextFactory<IssueLinkDbContext>
{
    public IssueLinkDbContext CreateDbContext(string[] args)
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

        DbContextOptionsBuilder<IssueLinkDbContext> builder = new();
        builder.UseConfiguredDatabase(cfg, migrationsAssembly: typeof(IssueLinkDbContextFactory).Assembly.GetName().Name);

        return new IssueLinkDbContext(builder.Options);
    }
}
