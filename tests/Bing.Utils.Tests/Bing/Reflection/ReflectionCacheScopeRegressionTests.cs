using Bing.Reflection;
using Shouldly;

namespace Bing.Utils.Tests.Bing.Reflection;

public class ReflectionCacheScopeRegressionTests
{
    [Fact]
    public void GetPublicInstanceProperties_RecursiveGenericConstraint_Terminates()
    {
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(RecursiveConstraint<>));

        properties.Select(property => property.Name).ShouldContain(nameof(RecursiveConstraint<int>.Value));
    }

    private sealed class RecursiveConstraint<T> where T : IComparable<T>
    {
        public T Value { get; set; }
    }
}
