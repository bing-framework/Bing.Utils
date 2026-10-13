namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitProperties` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.Properties")]
public class TypeVisitPropertiesTest
{
    /// <summary>
    /// 测试用例：验证 `GetProperties` 在 `WithAccessOptions` 场景下，结果为 `ShouldFilterExpected`。
    /// </summary>
    [Fact]
    public void GetProperties_WithAccessOptions_ShouldFilterExpected()
    {
        var getterProps = TypeVisit.GetProperties(typeof(TypeVisitSample), PropertyAccessOptions.Getters)
            .Select(x => x.Name)
            .ToList();
        var setterProps = TypeVisit.GetProperties(typeof(TypeVisitSample), PropertyAccessOptions.Setters)
            .Select(x => x.Name)
            .ToList();
        getterProps.ShouldContain(nameof(TypeVisitSample.Normal));
        getterProps.ShouldContain(nameof(TypeVisitSample.ReadOnly));
        getterProps.ShouldNotContain(nameof(TypeVisitSample.WriteOnly));
        setterProps.ShouldContain(nameof(TypeVisitSample.Normal));
        setterProps.ShouldContain(nameof(TypeVisitSample.WriteOnly));
        setterProps.ShouldNotContain(nameof(TypeVisitSample.ReadOnly));
    }
    /// <summary>
    /// 测试用例：验证 `GetProperty` 在 `WithInvalidExpression` 场景下，结果为 `ShouldThrow`。
    /// </summary>
    [Fact]
    public void GetProperty_WithInvalidExpression_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<TypeVisitSample, string>(x => x.ToString()));
    }
    /// <summary>
    /// 测试用例：验证 `GetProperty` 在 `WithMismatchedAccess` 场景下，结果为 `ShouldThrow`。
    /// </summary>
    [Fact]
    public void GetProperty_WithMismatchedAccess_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<TypeVisitSample, int>(x => x.ReadOnly, PropertyAccessOptions.Setters));
    }
    /// <summary>
    /// 测试用例：验证 `Exclude` 在 `ShouldReturnRemainingProperties` 场景下的行为。
    /// </summary>
    [Fact]
    public void Exclude_ShouldReturnRemainingProperties()
    {
        var properties = TypeVisit.GetProperties(typeof(TypeVisitSample), PropertyAccessOptions.Getters).ToList();
        var filtered = TypeVisit.Exclude<TypeVisitSample>(properties, x => x.Normal).Select(x => x.Name).ToList();
        filtered.ShouldNotContain(nameof(TypeVisitSample.Normal));
        filtered.ShouldContain(nameof(TypeVisitSample.ReadOnly));
    }
    private class TypeVisitSample
    {
        public int Normal { get; set; }
        public int ReadOnly { get; } = 1;
        public int WriteOnly
        {
            set { _ = value; }
        }
    }
}

