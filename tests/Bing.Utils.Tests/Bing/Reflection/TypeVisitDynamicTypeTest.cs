using System;
using System.Collections.Generic;
using Bing.Dynamic;

namespace Bing.Reflection;

#if NET6_0_OR_GREATER

/// <summary>
/// 验证动态类型的缓存、属性访问及构造参数绑定。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.DynamicType")]
public class TypeVisitDynamicTypeTest
{
    /// <summary>
    /// 验证相同属性模式复用缓存中的动态类型。
    /// </summary>
    [Fact]
    public void CreateDynamicType_SameProperties_ReturnsCachedSameType()
    {
        var left = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            ["Id"] = typeof(int),
            ["Name"] = typeof(string)
        });
        var right = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            ["Id"] = typeof(int),
            ["Name"] = typeof(string)
        });

        left.ShouldBeSameAs(right);
    }

    /// <summary>
    /// 验证生成类型继承动态基类且支持属性读写。
    /// </summary>
    [Fact]
    public void CreateDynamicType_SetAndGetPropertyValue_SupportsPropertyReadWrite()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            ["Count"] = typeof(int),
            ["Title"] = typeof(string)
        });

        type.IsSubclassOf(typeof(DynamicBase)).ShouldBeTrue();

        var instance = Activator.CreateInstance(type);
        instance.ShouldNotBeNull();

        type.GetProperty("Count")!.SetValue(instance, 7);
        type.GetProperty("Title")!.SetValue(instance, "ok");

        type.GetProperty("Count")!.GetValue(instance).ShouldBe(7);
        type.GetProperty("Title")!.GetValue(instance).ShouldBe("ok");
    }

    /// <summary>
    /// 验证动态类型构造参数按属性声明顺序绑定。
    /// </summary>
    [Fact]
    public void CreateDynamicType_ParameterizedConstructor_BindsArgumentsByPropertyOrder()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>
        {
            ["Code"] = typeof(int),
            ["Alias"] = typeof(string)
        });

        var instance = Activator.CreateInstance(type, 99, "bing");
        instance.ShouldNotBeNull();

        type.GetProperty("Code")!.GetValue(instance).ShouldBe(99);
        type.GetProperty("Alias")!.GetValue(instance).ShouldBe("bing");
    }

    /// <summary>
    /// 验证空属性模式触发参数空异常。
    /// </summary>
    [Fact]
    public void CreateDynamicType_NullProperties_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("source");
    }
}

#endif
