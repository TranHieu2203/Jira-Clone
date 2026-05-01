using BB.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Issue.Infrastructure;

public sealed class IssueDbContextFactory : IDesignTimeDbContextFactory<IssueDbContext>
{
    public IssueDbContext CreateDbContext(string[] args)
    {
        var provider = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "Postgres";
        var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION") ??
                      (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase)
                          ? "User Id=jira_clone;Password=jira_clone;Data Source=localhost:1521/FREEPDB1"
                          : "Host=localhost;Port=5432;Database=jira_clone;Username=jira_clone;Password=jira_clone");

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["Database:ConnectionString"] = connStr,
                ["Database:CommandTimeout"] = "30"
            }).Build();

        var builder = new DbContextOptionsBuilder<IssueDbContext>();
        builder.UseConfiguredDatabase(cfg, migrationsAssembly: typeof(IssueDbContextFactory).Assembly.GetName().Name);

        return new IssueDbContext(builder.Options);
    }
}
