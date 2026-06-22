using System.Reflection;
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
}
