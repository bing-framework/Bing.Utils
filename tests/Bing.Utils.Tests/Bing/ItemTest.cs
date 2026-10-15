using System.Runtime.Serialization;

namespace Bing;

/// <summary>
/// 测试类：`Item` 构造函数、比较行为与序列化契约测试
/// </summary>
public class ItemTest
{
    /// <summary>
    /// 验证默认构造函数创建的实例，其可空/引用属性应保持默认值
    /// </summary>
    [Fact]
    public void Constructor_DefaultCtor_PropertiesKeepDefaultValues()
    {
        var item = new Item();

        item.Text.ShouldBeNull();
        item.Value.ShouldBeNull();
        item.SortId.ShouldBeNull();
        item.Group.ShouldBeNull();
        item.Disabled.ShouldBeNull();
    }

    /// <summary>
    /// 验证带参构造函数应按输入赋值，包括可选参数
    /// </summary>
    [Fact]
    public void Constructor_WithArguments_AssignsAllProperties()
    {
        var value = new object();

        var item = new Item("文本", value, 3, "分组A", true);

        item.Text.ShouldBe("文本");
        item.Value.ShouldBeSameAs(value);
        item.SortId.ShouldBe(3);
        item.Group.ShouldBe("分组A");
        item.Disabled.ShouldBe(true);
    }

    /// <summary>
    /// 验证带参构造函数在可选参数缺省时应保留对应属性为空
    /// </summary>
    [Fact]
    public void Constructor_WithRequiredArgumentsOnly_OptionalPropertiesRemainNull()
    {
        var item = new Item("文本", 123);

        item.Text.ShouldBe("文本");
        item.Value.ShouldBe(123);
        item.SortId.ShouldBeNull();
        item.Group.ShouldBeNull();
        item.Disabled.ShouldBeNull();
    }

    /// <summary>
    /// 验证条目文本比较结果的正负符号。
    /// </summary>
    /// <param name="leftText">左侧条目文本。</param>
    /// <param name="rightText">右侧条目文本。</param>
    /// <param name="expectedSign">预期比较结果符号。</param>
    [Theory]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("abc", "abc", 0)]
    [InlineData(null, "abc", -1)]
    [InlineData("abc", null, 1)]
    [InlineData(null, null, 0)]
    public void CompareTo_WithDifferentTextInputs_ReturnsExpectedSign(string leftText, string rightText, int expectedSign)
    {
        var left = new Item { Text = leftText };
        var right = new Item { Text = rightText };

        var result = left.CompareTo(right);

        Math.Sign(result).ShouldBe(expectedSign);
    }

    /// <summary>
    /// 验证`CompareTo` 传入 `null` 时，当前实现会因访问 `other.Text` 抛出空引用异常
    /// </summary>
    [Fact]
    public void CompareTo_WhenOtherIsNull_ThrowsNullReferenceException()
    {
        var item = new Item { Text = "a" };

        Should.Throw<NullReferenceException>(() => item.CompareTo(null));
    }

    /// <summary>
    /// 验证`Item` 类型应声明 `DataContract` 序列化契约特性
    /// </summary>
    [Fact]
    public void Type_Metadata_ShouldContainDataContractAttribute()
    {
        var attribute = typeof(Item).GetCustomAttribute<DataContractAttribute>();

        attribute.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证关键属性声明不输出默认值的数据成员特性。
    /// </summary>
    /// <param name="propertyName">待检查的属性名。</param>
    [Theory]
    [InlineData(nameof(Item.Text))]
    [InlineData(nameof(Item.Value))]
    [InlineData(nameof(Item.SortId))]
    [InlineData(nameof(Item.Group))]
    [InlineData(nameof(Item.Disabled))]
    public void Property_Metadata_ShouldContainDataMemberAttribute_WithEmitDefaultValueFalse(string propertyName)
    {
        var property = typeof(Item).GetProperty(propertyName);

        property.ShouldNotBeNull();
        var attribute = property.GetCustomAttribute<DataMemberAttribute>();
        attribute.ShouldNotBeNull();
        attribute.EmitDefaultValue.ShouldBeFalse();
    }
}
