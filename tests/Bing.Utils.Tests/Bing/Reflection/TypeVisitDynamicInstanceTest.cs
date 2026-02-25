namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitDynamicInstance` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.DynamicInstance")]
public class TypeVisitDynamicInstanceTest
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `WithPropertyDictionary` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void CreateInstance_WithPropertyDictionary_CurrentlyThrowsNullReferenceException()
    {
        var values = new Dictionary<string, object>
        {
            ["Name"] = "Alice",
            ["Age"] = 18
        };
        Should.Throw<NullReferenceException>(() => TypeVisit.CreateInstance(values));
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `WithPropertyDictionary` 场景下，结果为 `ReturnsTypeContainingProperties`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithPropertyDictionary_ReturnsTypeContainingProperties()
    {
        var properties = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Enabled"] = typeof(bool)
        };
        var type = TypeVisit.CreateDynamicType(properties);
        type.ShouldNotBeNull();
        type.GetProperty("Name")!.PropertyType.ShouldBe(typeof(string));
        type.GetProperty("Enabled")!.PropertyType.ShouldBe(typeof(bool));
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `WithSameSignatureAndOrder` 场景下，结果为 `ReturnsCachedType`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithSameSignatureAndOrder_ReturnsCachedType()
    {
        var first = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var second = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var type1 = TypeVisit.CreateDynamicType(first);
        var type2 = TypeVisit.CreateDynamicType(second);
        type1.ShouldBeSameAs(type2);
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `WithSamePropertiesDifferentOrder` 场景下，结果为 `CurrentlyReturnsDifferentTypes`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithSamePropertiesDifferentOrder_CurrentlyReturnsDifferentTypes()
    {
        var first = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var second = new Dictionary<string, Type>
        {
            ["Age"] = typeof(int),
            ["Name"] = typeof(string)
        };
        var type1 = TypeVisit.CreateDynamicType(first);
        var type2 = TypeVisit.CreateDynamicType(second);
        type1.ShouldNotBeSameAs(type2);
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `WithEmptyProperties` 场景下，结果为 `ReturnsTypeDerivedFromDynamicBase`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithEmptyProperties_ReturnsTypeDerivedFromDynamicBase()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>());
        type.ShouldNotBeNull();
        type.IsSubclassOf(typeof(Bing.Dynamic.DynamicBase)).ShouldBeTrue();
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Length.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `WithNullProperties` 场景下，结果为 `CurrentlyThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithNullProperties_CurrentlyThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `WithNullDictionary` 场景下，结果为 `CurrentlyThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstance_WithNullDictionary_CurrentlyThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((IDictionary<string, object>)null));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `WithNullPropertyValue` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void CreateInstance_WithNullPropertyValue_CurrentlyThrowsNullReferenceException()
    {
        var values = new Dictionary<string, object>
        {
            ["Name"] = null
        };
        Should.Throw<NullReferenceException>(() => TypeVisit.CreateInstance(values));
    }
#endif
}

