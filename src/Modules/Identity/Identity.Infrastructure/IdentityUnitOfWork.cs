using BB.Persistence;
using Identity.Application;

namespace Identity.Infrastructure;

public sealed class IdentityUnitOfWork : UnitOfWork<IdentityDbContext>, IIdentityUnitOfWork
{
    public IdentityUnitOfWork(IdentityDbContext ctx) : base(ctx) { }
}
