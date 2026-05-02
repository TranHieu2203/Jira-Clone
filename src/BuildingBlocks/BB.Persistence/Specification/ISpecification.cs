using System.Linq.Expressions;

namespace BB.Persistence.Specification;

/// <summary>Bộ lọc có thể kết hợp cho EF <see cref="IQueryable{T}.Where"/>.</summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
}
