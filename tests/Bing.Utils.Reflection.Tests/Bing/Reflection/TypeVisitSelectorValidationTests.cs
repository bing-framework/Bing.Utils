using System.Collections.Generic;
using System.Linq.Expressions;

namespace Bing.Reflection;

[Trait("Bing.Reflection", "TypeVisit.SelectorValidation")]
public class TypeVisitSelectorValidationTests
{
    [Fact]
    public void GetFields_InvalidSelector_ThrowsBeforeResultEnumeration()
    {
        IEnumerable<Expression<Func<SelectorSample, object>>> selectors = new Expression<Func<SelectorSample, object>>[]
        {
            sample => sample.Field,
            sample => sample.Property
        };

        Should.Throw<ArgumentException>(() => TypeVisit.GetFields<SelectorSample>(selectors));
    }

    [Fact]
    public void GetFields_NullSelector_ThrowsBeforeResultEnumeration()
    {
        IEnumerable<Expression<Func<SelectorSample, object>>> selectors = new Expression<Func<SelectorSample, object>>[]
        {
            null!
        };

        Should.Throw<ArgumentNullException>(() => TypeVisit.GetFields<SelectorSample>(selectors));
    }

    [Fact]
    public void GetProperties_InvalidSelector_ThrowsBeforeResultEnumeration()
    {
        IEnumerable<Expression<Func<SelectorSample, object>>> selectors = new Expression<Func<SelectorSample, object>>[]
        {
            sample => sample.Property,
            sample => sample.Field
        };

        Should.Throw<ArgumentException>(() => TypeVisit.GetProperties<SelectorSample>(selectors));
    }

    [Fact]
    public void GetProperties_NullSelector_ThrowsBeforeResultEnumeration()
    {
        IEnumerable<Expression<Func<SelectorSample, object>>> selectors = new Expression<Func<SelectorSample, object>>[]
        {
            null!
        };

        Should.Throw<ArgumentNullException>(() => TypeVisit.GetProperties<SelectorSample>(selectors));
    }

    private sealed class SelectorSample
    {
        public int Field = 1;

        public int Property { get; set; }
    }
}
