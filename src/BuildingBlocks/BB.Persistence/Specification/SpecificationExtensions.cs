using System.Linq.Expressions;

namespace BB.Persistence.Specification;

public static class SpecificationExtensions
{
    /// <summary>Kết hợp AND; thay parameter của <paramref name="right"/> bằng parameter của <paramref name="left"/> (không dùng <see cref="Expression.Invoke"/>).</summary>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        ParameterExpression pLeft = left.Criteria.Parameters[0];
        var replacer = new ParameterReplacer(right.Criteria.Parameters[0], pLeft);
        Expression rightBody = replacer.Visit(right.Criteria.Body)
            ?? throw new InvalidOperationException("Specification And: right body is null");

        BinaryExpression combined = Expression.AndAlso(left.Criteria.Body, rightBody);
        Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(combined, pLeft);
        return new Specification<T>(lambda);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
