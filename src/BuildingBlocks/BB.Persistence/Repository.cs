using System.Linq.Expressions;
using BB.Common;
using Microsoft.EntityFrameworkCore;

namespace BB.Persistence;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly DbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(DbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<T> q = Set.AsNoTracking();
        if (predicate is not null) q = q.Where(predicate);
        return await q.ToListAsync(ct);
    }

    public async Task<PagedList<T>> PagedAsync(PagedRequest request, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<T> q = Set.AsNoTracking();
        if (predicate is not null) q = q.Where(predicate);
        var total = await q.LongCountAsync(ct);
        var skip = (Math.Max(request.PageIndex, 1) - 1) * Math.Max(request.PageSize, 1);
        var items = await q.Skip(skip).Take(request.PageSize).ToListAsync(ct);
        return new PagedList<T>(items, total, request.PageIndex, request.PageSize);
    }

    public Task AddAsync(T entity, CancellationToken ct = default) => Set.AddAsync(entity, ct).AsTask();
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);
}

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _ctx;
    public UnitOfWork(TContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        var strategy = _ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            try
            {
                await work(ct);
                await _ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public void DiscardChanges() => _ctx.ChangeTracker.Clear();
}
