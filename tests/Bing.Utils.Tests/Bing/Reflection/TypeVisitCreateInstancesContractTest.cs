namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeVisitCreateInstancesContract` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.CreateInstance.Contract")]
public class TypeVisitCreateInstancesContractTest
{
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `TypeOverload_NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `GenericTypeOverload_NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericTypeOverload_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance<Sample>((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `NoMatchingConstructor` 场景下，结果为 `CurrentBehaviorReturnsNull`。
    /// </summary>
    [Fact]
    public void CreateInstance_NoMatchingConstructor_CurrentBehaviorReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(Sample), 123);
        instance.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `WithMatchingConstructor` 场景下，结果为 `ReturnsCreatedObject`。
    /// </summary>
    [Fact]
    public void CreateInstance_WithMatchingConstructor_ReturnsCreatedObject()
    {
        var instance = TypeVisit.CreateInstance(typeof(Sample), "Alice", 18) as Sample;
        instance.ShouldNotBeNull();
        instance!.Name.ShouldBe("Alice");
        instance.Age.ShouldBe(18);
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `GenericOverload_WithArgs` 场景下，结果为 `ReturnsTypedInstance`。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericOverload_WithArgs_ReturnsTypedInstance()
    {
        var instance = TypeVisit.CreateInstance<Sample>("Bob", 20);
        instance.ShouldNotBeNull();
        instance.Name.ShouldBe("Bob");
        instance.Age.ShouldBe(20);
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `GenericWithTypeOverload_WithArgs` 场景下，结果为 `ReturnsTypedInstance`。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithTypeOverload_WithArgs_ReturnsTypedInstance()
    {
        var instance = TypeVisit.CreateInstance<Sample>(typeof(Sample), "Carol", 22);
        instance.ShouldNotBeNull();
        instance.Name.ShouldBe("Carol");
        instance.Age.ShouldBe(22);
    }
#if NET6_0_OR_GREATER
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `DynamicValuesNull` 场景下，结果为 `CurrentBehaviorThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstance_DynamicValuesNull_CurrentBehaviorThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((IDictionary<string, object>)null));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `SamePropertySchema` 场景下，结果为 `ReturnsSameCachedType`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_SamePropertySchema_ReturnsSameCachedType()
    {
        var properties1 = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var properties2 = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var type1 = TypeVisit.CreateDynamicType(properties1);
        var type2 = TypeVisit.CreateDynamicType(properties2);
        type1.ShouldBe(type2);
    }
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `NullProperties` 场景下，结果为 `CurrentBehaviorThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_NullProperties_CurrentBehaviorThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("source");
    }
#endif
    private sealed class Sample
    {
        public Sample()
        {
        }
        public Sample(string name, int age)
        {
            Name = name;
            Age = age;
        }
        public string Name { get; }
        public int Age { get; }
    }
}

