using Bing.Dynamic;

namespace Bing.Reflection;

#if NET6_0_OR_GREATER

/// <summary>
/// 测试类：覆盖 TypeVisit 动态实例化分支的边界契约。
/// </summary>
[Trait("Bing.Reflection", "TypeVisit.DynamicInstance.Boundary")]
public class TypeVisitDynamicInstanceBoundaryTest
{
    /// <summary>
    /// 测试用例：空字典输入时，应返回一个 DynamicBase 子类实例。
    /// </summary>
    [Fact]
    public void CreateInstance_EmptyDictionary_ReturnsDynamicBaseDerivedInstance()
    {
        var instance = TypeVisit.CreateInstance(new Dictionary<string, object>());

        instance.ShouldNotBeNull();
        instance.GetType().IsSubclassOf(typeof(DynamicBase)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：相同属性模式多次创建时，应复用同一动态类型。
    /// </summary>
    [Fact]
    public void CreateInstance_EmptyDictionary_MultipleCalls_UsesSameGeneratedType()
    {
        var first = TypeVisit.CreateInstance(new Dictionary<string, object>());
        var second = TypeVisit.CreateInstance(new Dictionary<string, object>());

        first.GetType().ShouldBe(second.GetType());
    }

    /// <summary>
    /// 测试用例：空字典创建的对象调用 DynamicBase 访问器时，当前行为抛出空引用异常。
    /// </summary>
    [Fact]
    public void CreateInstance_EmptyDictionary_DynamicBaseAccessor_CurrentlyThrowsNullReferenceException()
    {
        var instance = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object>());

        Should.Throw<NullReferenceException>(() => instance.GetPropertyValue("Any"));
        Should.Throw<NullReferenceException>(() => instance.SetPropertyValue("Any", 1));
    }
}

#endif
