namespace Bing.Reflection;
/// <summary>
/// 验证属性按访问器筛选、表达式匹配及排除。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.Properties")]
public class TypeVisitPropertiesTest
{
    /// <summary>
    /// 验证可读与可写属性筛选分别匹配对应访问器。
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
    /// 验证属性查找拒绝方法调用表达式。
    /// </summary>
    [Fact]
    public void GetProperty_WithInvalidExpression_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<TypeVisitSample, string>(x => x.ToString()));
    }
    /// <summary>
    /// 验证只读属性不满足可写访问要求。
    /// </summary>
    [Fact]
    public void GetProperty_WithMismatchedAccess_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<TypeVisitSample, int>(x => x.ReadOnly, PropertyAccessOptions.Setters));
    }
    /// <summary>
    /// 验证排除指定属性后保留其余可读属性。
    /// </summary>
    [Fact]
    public void Exclude_ShouldReturnRemainingProperties()
    {
        var properties = TypeVisit.GetProperties(typeof(TypeVisitSample), PropertyAccessOptions.Getters).ToList();
        var filtered = TypeVisit.Exclude<TypeVisitSample>(properties, x => x.Normal).Select(x => x.Name).ToList();
        filtered.ShouldNotContain(nameof(TypeVisitSample.Normal));
        filtered.ShouldContain(nameof(TypeVisitSample.ReadOnly));
    }
    /// <summary>
    /// 提供可读写、只读与只写属性的反射样例。
    /// </summary>
    private class TypeVisitSample
    {
        /// <summary>
        /// 获取或设置用于验证访问器筛选的整数。
        /// </summary>
        public int Normal { get; set; }
        /// <summary>
        /// 获取固定的只读测试值。
        /// </summary>
        public int ReadOnly { get; } = 1;
        /// <summary>
        /// 设置用于验证只写访问器的整数。
        /// </summary>
        /// <remarks>设置操作不保存传入值。</remarks>
        public int WriteOnly
        {
            set { _ = value; }
        }
    }
}

