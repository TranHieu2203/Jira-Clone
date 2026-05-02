using BB.Persistence.Specification;
using FluentAssertions;

namespace Issue.UnitTests;

public sealed class SpecificationCompositionTests
{
    [Fact]
    public void And_Combines_without_invoke_trashing_ef_translation()
    {
        ISpecification<int> left = new Specification<int>(x => x > 1);
        ISpecification<int> right = new Specification<int>(x => x < 10);
        Func<int, bool> combined = left.And(right).Criteria.Compile();

        combined(5).Should().BeTrue();
        combined(1).Should().BeFalse();
        combined(11).Should().BeFalse();
    }
}
