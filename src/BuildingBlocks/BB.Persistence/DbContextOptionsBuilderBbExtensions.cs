using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BB.Persistence;

public static class DbContextOptionsBuilderBbExtensions
{
    public static DbContextOptionsBuilder AddBbDatabaseProvider(
        this DbContextOptionsBuilder builder,
        DbProvider provider)
    {
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
            new DatabaseProviderOptionsExtension(provider));
        return builder;
    }
}
