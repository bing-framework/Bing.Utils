using System.Reflection;
using System.Runtime.CompilerServices;
using Bing.Dynamic;

namespace Bing.Dynamic;

/// <summary>
/// 用于测试的 <see cref="DynamicBase"/> 具体子类
/// </summary>
internal class DynamicSample : DynamicBase
{
    /// <summary>字符串属性</summary>
    public string Name { get; set; } = "Default";

    /// <summary>整数属性</summary>
    public int Age { get; set; } = 0;

    /// <summary>只有公开 getter 的属性。</summary>
    public string PrivateSetter { get; private set; } = "Original";

    /// <summary>只有公开 setter 的属性。</summary>
    public string PrivateGetter { private get; set; }

    /// <summary>读取仅有公开 setter 的属性，用于验证拒绝访问后未被写入。</summary>
    public string ReadPrivateGetter() => PrivateGetter;
}

/// <summary>用于验证动态基类属性缓存不会持有实例的独立类型。</summary>
internal sealed class DynamicBaseCacheProbeSample : DynamicBase
{
    /// <summary>缓存探针属性。</summary>
    public int Value { get; set; }
}

/// <summary>
/// 测试类：覆盖 <see cref="DynamicBase"/> 相关行为。
/// </summary>
[Trait("Reflection", "DynamicBase")]
public class DynamicBaseTest
{
    private readonly DynamicSample _sample = new();

    /// <summary>
    /// 测试用例：验证 <see cref="DynamicBase.GetPropertyValue"/> 在 `ExistingStringProperty`
    /// 场景下，结果为 `ReturnsCorrectValue`。
    /// </summary>
    [Fact]
    public void GetPropertyValue_ExistingStringProperty_ReturnsCorrectValue()
    {
        _sample.Name = "Alice";
        var result = _sample.GetPropertyValue(nameof(DynamicSample.Name));
        result.ShouldBe("Alice");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DynamicBase.GetPropertyValue"/> 在 `ExistingIntProperty`
    /// 场景下，结果为 `ReturnsCorrectValue`。
    /// </summary>
    [Fact]
    public void GetPropertyValue_ExistingIntProperty_ReturnsCorrectValue()
    {
        _sample.Age = 42;
        var result = _sample.GetPropertyValue(nameof(DynamicSample.Age));
        result.ShouldBe(42);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DynamicBase.SetPropertyValue"/> 在 `StringProperty`
    /// 场景下，结果为 `PropertyValueChanged`。
    /// </summary>
    [Fact]
    public void SetPropertyValue_StringProperty_PropertyValueChanged()
    {
        _sample.SetPropertyValue(nameof(DynamicSample.Name), "Bob");
        _sample.Name.ShouldBe("Bob");
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DynamicBase.SetPropertyValue"/> 在 `IntProperty`
    /// 场景下，结果为 `PropertyValueChanged`。
    /// </summary>
    [Fact]
    public void SetPropertyValue_IntProperty_PropertyValueChanged()
    {
        _sample.SetPropertyValue(nameof(DynamicSample.Age), 99);
        _sample.Age.ShouldBe(99);
    }

    /// <summary>
    /// 测试用例：验证 <see cref="DynamicBase.GetPropertyValue"/> 和
    /// <see cref="DynamicBase.SetPropertyValue"/> 往返读写正确。
    /// </summary>
    [Fact]
    public void GetAndSet_RoundTrip_ValueConsistent()
    {
        _sample.SetPropertyValue(nameof(DynamicSample.Name), "RoundTrip");
        var value = _sample.GetPropertyValue(nameof(DynamicSample.Name));
        value.ShouldBe("RoundTrip");
    }

    /// <summary>默认访问不能调用非公开设置器。</summary>
    [Fact]
    public void SetPropertyValue_PrivateSetter_ThrowsWithoutWriting()
    {
        Should.Throw<InvalidOperationException>(() => _sample.SetPropertyValue(nameof(DynamicSample.PrivateSetter), "Changed"));
        _sample.PrivateSetter.ShouldBe("Original");
    }

    /// <summary>默认访问不能调用非公开读取器。</summary>
    [Fact]
    public void GetPropertyValue_PrivateGetter_Throws()
    {
        _sample.PrivateGetter = "Hidden";
        Should.Throw<InvalidOperationException>(() => _sample.GetPropertyValue(nameof(DynamicSample.PrivateGetter)));
        _sample.ReadPrivateGetter().ShouldBe("Hidden");
    }

    /// <summary>属性缓存首次编译后不应长期持有查询实例。</summary>
    [Fact]
    public void PropertyCache_DoesNotRetainQueriedInstance()
    {
        var weakReference = CreateCachedInstanceWeakReference();
        for (var attempt = 0; attempt < 20 && weakReference.IsAlive; attempt++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        weakReference.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCachedInstanceWeakReference()
    {
        var instance = new DynamicBaseCacheProbeSample();
        instance.SetPropertyValue(nameof(DynamicBaseCacheProbeSample.Value), 7);
        instance.GetPropertyValue(nameof(DynamicBaseCacheProbeSample.Value)).ShouldBe(7);
        return new WeakReference(instance);
    }
}
