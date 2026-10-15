namespace Bing.Reflection;
/// <summary>
/// 验证实例创建重载的参数匹配及动态类型缓存。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.CreateInstance.Contract")]
public class TypeVisitCreateInstancesContractTest
{
    /// <summary>
    /// 验证按类型创建实例时拒绝空类型。
    /// </summary>
    [Fact]
    public void CreateInstance_TypeOverload_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证泛型类型入口拒绝空类型。
    /// </summary>
    [Fact]
    public void CreateInstance_GenericTypeOverload_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance<Sample>((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证没有匹配构造函数时当前返回空。
    /// </summary>
    [Fact]
    public void CreateInstance_NoMatchingConstructor_CurrentBehaviorReturnsNull()
    {
        var instance = TypeVisit.CreateInstance(typeof(Sample), 123);
        instance.ShouldBeNull();
    }
    /// <summary>
    /// 验证匹配构造函数后正确初始化名称和年龄。
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
    /// 验证泛型入口按参数创建强类型实例。
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
    /// 验证同时指定泛型与运行时类型时按参数创建强类型实例。
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
    /// 验证空动态属性值字典当前触发参数空异常。
    /// </summary>
    [Fact]
    public void CreateInstance_DynamicValuesNull_CurrentBehaviorThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((IDictionary<string, object>)null));
        ex.ParamName.ShouldBe("source");
    }
    /// <summary>
    /// 验证相同属性模式复用缓存中的动态类型。
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
    /// 验证空动态属性模式当前触发参数空异常。
    /// </summary>
    [Fact]
    public void CreateDynamicType_NullProperties_CurrentBehaviorThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("source");
    }
#endif
    /// <summary>
    /// 提供无参与双参数构造函数的实例创建样例。
    /// </summary>
    private sealed class Sample
    {
        /// <summary>
        /// 初始化一个 <see cref="Sample"/> 类型的实例。
        /// </summary>
        public Sample()
        {
        }
        /// <summary>
        /// 初始化一个 <see cref="Sample"/> 类型的实例。
        /// </summary>
        /// <param name="name">测试名称。</param>
        /// <param name="age">测试年龄。</param>
        public Sample(string name, int age)
        {
            Name = name;
            Age = age;
        }
        /// <summary>
        /// 获取构造函数传入的测试名称。
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 获取构造函数传入的测试年龄。
        /// </summary>
        public int Age { get; }
    }
}

