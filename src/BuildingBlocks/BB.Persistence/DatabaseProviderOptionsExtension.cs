using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BB.Persistence;

/// <summary>
/// Gắn provider đang chạy vào DbContextOptions để ProviderAwareMigrationsAssembly lọc migration theo suffix _Postgres / _Oracle.
/// </summary>
internal sealed class DatabaseProviderOptionsExtension : IDbContextOptionsExtension
{
    public DatabaseProviderOptionsExtension(DbProvider provider) => Provider = provider;

    public DbProvider Provider { get; }

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Không đăng ký service ở đây — dùng ReplaceService trên DbContextOptionsBuilder.
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(DatabaseProviderOptionsExtension extension)
            : base(extension)
        {
        }

        private DatabaseProviderOptionsExtension Ext => (DatabaseProviderOptionsExtension)Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => $"BB DatabaseProvider={Ext.Provider} ";

        public override int GetServiceProviderHashCode() => (int)Ext.Provider;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo o && o.Ext.Provider == Ext.Provider;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) =>
            debugInfo["BB:DatabaseProvider"] = Ext.Provider.ToString();
    }
}
