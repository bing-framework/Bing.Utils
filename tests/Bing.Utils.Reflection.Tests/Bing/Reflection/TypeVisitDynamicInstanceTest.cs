using System.Reflection;

namespace Bing.Reflection;
/// <summary>
/// 验证动态类型的属性模式、缓存及实例创建边界。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.DynamicInstance")]
public class TypeVisitDynamicInstanceTest
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// 验证非空属性值字典的实例创建当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void CreateInstance_WithPropertyDictionary_CreatesReadableObject()
    {
        var values = new Dictionary<string, object>
        {
            ["Name"] = "Alice",
            ["Age"] = 18
        };
        var instance = (Bing.Dynamic.DynamicBase)TypeVisit.CreateInstance(values);
        instance.GetPropertyValue("Name").ShouldBe("Alice");
        instance.GetPropertyValue("Age").ShouldBe(18);
    }
    /// <summary>
    /// 验证动态类型包含指定名称及类型的属性。
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
    /// 验证相同属性签名及顺序复用动态类型。
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
    /// 验证相同属性以不同顺序声明时当前生成不同类型。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithSamePropertiesDifferentOrder_ReturnsSameType()
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
        type1.ShouldBeSameAs(type2);
    }
    /// <summary>
    /// 验证空属性模式生成不含自有属性的动态基类派生类型。
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
    /// 验证空属性模式当前触发参数空异常。
    /// </summary>
    [Fact]
    public void CreateDynamicType_WithNullProperties_CurrentlyThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("properties");
    }
    /// <summary>
    /// 验证空属性值字典当前触发参数空异常。
    /// </summary>
    [Fact]
    public void CreateInstance_WithNullDictionary_CurrentlyThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((IDictionary<string, object>)null));
        ex.ParamName.ShouldBe("values");
    }
    /// <summary>
    /// 验证包含空属性值的动态实例创建当前抛出空引用异常。
    /// </summary>
    [Fact]
    public void CreateInstance_WithNullPropertyValue_UsesObjectProperty()
    {
        var values = new Dictionary<string, object>
        {
            ["Name"] = null
        };
        var instance = (Bing.Dynamic.DynamicBase)TypeVisit.CreateInstance(values);
        instance.GetType().GetProperty("Name")!.PropertyType.ShouldBe(typeof(object));
        instance.GetPropertyValue("Name").ShouldBeNull();
        instance.SetPropertyValue("Name", null);
    }
#endif
}

