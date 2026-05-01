using BB.Persistence;
using Sample.Application;

namespace Sample.Infrastructure;

public sealed class SampleUnitOfWork : UnitOfWork<SampleDbContext>, ISampleUnitOfWork
{
    public SampleUnitOfWork(SampleDbContext ctx) : base(ctx) { }
}
