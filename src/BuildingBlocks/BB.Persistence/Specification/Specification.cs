using System.Linq.Expressions;

namespace BB.Persistence.Specification;

public sealed class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; }

    public Specification(Expression<Func<T, bool>> criteria) =>
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
}
