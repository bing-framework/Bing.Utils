using System;
using System.Collections.Generic;
using Bing.Dynamic;

namespace Bing.Reflection;

#if NET6_0_OR_GREATER

/// <summary>
/// 测试类：覆盖 `TypeVisit.CreateDynamicType` 的动态类型生成契约。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.DynamicType")]
public class TypeVisitDynamicTypeTest
{
    /// <summary>
    /// 测试用例：验证 `CreateDynamicType` 在 `SameProperties` 场景下，结果为 `ReturnsCachedSameType`。
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
    /// 测试用例：验证 `CreateDynamicType` 在 `SetAndGetPropertyValue` 场景下，结果为 `SupportsPropertyReadWrite`。
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
    /// 测试用例：验证 `CreateDynamicType` 在 `ParameterizedConstructor` 场景下，结果为 `BindsArgumentsByPropertyOrder`。
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
    /// 测试用例：验证 `CreateDynamicType` 在 `NullProperties` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateDynamicType_NullProperties_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateDynamicType(null));
        ex.ParamName.ShouldBe("source");
    }
}

#endif
