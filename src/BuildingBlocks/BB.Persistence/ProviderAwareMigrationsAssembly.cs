using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BB.Persistence;

/// <summary>
/// Chỉ nạp migration có suffix tên class khớp provider (_Postgres / _Oracle) để hai chuỗi migration song song trên cùng DbContext.
/// </summary>
public sealed class ProviderAwareMigrationsAssembly : IMigrationsAssembly
{
    private readonly Type _contextType;
    private readonly Assembly _assembly;
    private readonly IMigrationsIdGenerator _idGenerator;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Migrations> _logger;
    private readonly DbProvider _provider;
    private IReadOnlyDictionary<string, TypeInfo>? _migrations;
    private ModelSnapshot? _modelSnapshot;

    public ProviderAwareMigrationsAssembly(
        ICurrentDbContext currentContext,
        IDbContextOptions options,
        IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
    {
        _contextType = currentContext.Context.GetType();

        string? migrationsAssemblyName = RelationalOptionsExtension.Extract(options).MigrationsAssembly;
        _assembly = migrationsAssemblyName is null
            ? _contextType.Assembly
            : Assembly.Load(new AssemblyName(migrationsAssemblyName));

        _idGenerator = idGenerator;
        _logger = logger;
        _provider = ResolveProvider(options);
    }

    public IReadOnlyDictionary<string, TypeInfo> Migrations =>
        _migrations ??= DiscoverMigrations();

    public ModelSnapshot? ModelSnapshot =>
        _modelSnapshot ??= DiscoverSnapshot();

    public Assembly Assembly => _assembly;

    public string? FindMigrationId(string nameOrId) =>
        Migrations.Keys
            .Where(
                _idGenerator.IsValidId(nameOrId)
                    ? id => string.Equals(id, nameOrId, StringComparison.OrdinalIgnoreCase)
                    : id => string.Equals(_idGenerator.GetName(id), nameOrId, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        Migration migration = (Migration)Activator.CreateInstance(migrationClass.AsType())!;
        migration.ActiveProvider = activeProvider;
        return migration;
    }

    private static DbProvider ResolveProvider(IDbContextOptions options)
    {
        foreach (IDbContextOptionsExtension ext in options.Extensions)
        {
            if (ext is DatabaseProviderOptionsExtension d)
                return d.Provider;
        }

        return DbProvider.Postgres;
    }

    private IReadOnlyDictionary<string, TypeInfo> DiscoverMigrations()
    {
        string suffix = _provider == DbProvider.Oracle ? "_Oracle" : "_Postgres";
        var result = new SortedList<string, TypeInfo>();

        foreach (TypeInfo typeInfo in EnumerateLoadableTypes(_assembly))
        {
            if (!typeInfo.IsSubclassOf(typeof(Migration)))
                continue;

            DbContextAttribute? ctxAttr = typeInfo.GetCustomAttribute<DbContextAttribute>();
            if (ctxAttr?.ContextType != _contextType)
                continue;

            MigrationAttribute? migAttr = typeInfo.GetCustomAttribute<MigrationAttribute>();
            if (migAttr?.Id is null)
            {
                _logger.MigrationAttributeMissingWarning(typeInfo);
                continue;
            }

            if (!typeInfo.Name.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            result.Add(migAttr.Id, typeInfo);
        }

        return result;
    }

    private ModelSnapshot? DiscoverSnapshot()
    {
        foreach (TypeInfo typeInfo in EnumerateLoadableTypes(_assembly))
        {
            if (!typeInfo.IsSubclassOf(typeof(ModelSnapshot)))
                continue;

            DbContextAttribute? ctxAttr = typeInfo.GetCustomAttribute<DbContextAttribute>();
            if (ctxAttr?.ContextType != _contextType)
                continue;

            return (ModelSnapshot)Activator.CreateInstance(typeInfo.AsType())!;
        }

        return null;
    }

    private static IEnumerable<TypeInfo> EnumerateLoadableTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        foreach (Type type in types)
        {
            TypeInfo info = type.GetTypeInfo();
            if (info.IsAbstract || info.IsGenericTypeDefinition)
                continue;

            if (info.DeclaredConstructors.All(c => c.GetParameters().Length != 0))
                continue;

            yield return info;
        }
    }
}
