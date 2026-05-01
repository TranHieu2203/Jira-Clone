using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BB.Persistence;

public static class DbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseConfiguredDatabase(
        this DbContextOptionsBuilder builder,
        IConfiguration configuration,
        string? migrationsAssembly = null)
    {
        var opts = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                   ?? throw new InvalidOperationException("Missing Database configuration section");
        return builder.UseConfiguredDatabase(opts, migrationsAssembly);
    }

    public static DbContextOptionsBuilder UseConfiguredDatabase(
        this DbContextOptionsBuilder builder,
        DatabaseOptions opts,
        string? migrationsAssembly = null)
    {
        if (string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString is empty");
        }

        if (!Enum.TryParse<DbProvider>(opts.Provider, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException($"Unsupported Database:Provider '{opts.Provider}'. Use Postgres or Oracle.");
        }

        builder.UseSnakeCaseNamingConvention();

        if (opts.EnableSensitiveDataLogging) builder.EnableSensitiveDataLogging();
        if (opts.EnableDetailedErrors) builder.EnableDetailedErrors();

        switch (provider)
        {
            case DbProvider.Postgres:
                builder.UseNpgsql(opts.ConnectionString, npg =>
                {
                    npg.CommandTimeout(opts.CommandTimeout);
                    if (migrationsAssembly is not null) npg.MigrationsAssembly(migrationsAssembly);
                    npg.MigrationsHistoryTable("__ef_migrations_history");
                });
                break;

            case DbProvider.Oracle:
                builder.UseOracle(opts.ConnectionString, ora =>
                {
                    ora.CommandTimeout(opts.CommandTimeout);
                    if (migrationsAssembly is not null) ora.MigrationsAssembly(migrationsAssembly);
                });
                break;
        }

        return builder;
    }

    public static DbProvider GetProvider(IConfiguration configuration)
    {
        var raw = configuration[$"{DatabaseOptions.SectionName}:Provider"] ?? "Postgres";
        return Enum.Parse<DbProvider>(raw, ignoreCase: true);
    }
}
