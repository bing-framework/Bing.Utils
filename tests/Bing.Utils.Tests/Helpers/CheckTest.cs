using System.Collections.Generic;
using Bing.Helpers;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 参数检查操作 测试
/// </summary>
public class CheckTest
{
    /// <summary>
    /// 测试 - 不可空检查
    /// </summary>
    [Fact]
    public void Test_NotNull()
    {
        Check.NotNull("test", nameof(Test_NotNull)).ShouldBe("test");
        Check.NotNull(string.Empty, nameof(Test_NotNull)).ShouldBe(string.Empty);
        Check.NotNull("test", nameof(Test_NotNull), maxLength: 4, minLength: 0).ShouldBe("test");

        Assert.Throws<ArgumentNullException>(() => Check.NotNull<object>(null, nameof(Test_NotNull)));
        Assert.Throws<ArgumentException>(() => Check.NotNull(null, nameof(Test_NotNull)));
        Assert.Throws<ArgumentException>(() => Check.NotNull("test", nameof(Test_NotNull), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNull("test", nameof(Test_NotNull), minLength: 5));
    }

    /// <summary>
    /// 测试 - 检查字符串不能为空引用或空白字符
    /// </summary>
    [Fact]
    public void Test_NotNullOrWhiteSpace()
    {
        Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace)).ShouldBe("test");
        Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), maxLength: 4, minLength: 0).ShouldBe("test");

        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace(null, nameof(Test_NotNullOrWhiteSpace)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace(string.Empty, nameof(Test_NotNullOrWhiteSpace)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), minLength: 5));
    }

    /// <summary>
    /// 测试 - 检查不为 null 或空集合
    /// </summary>
    [Fact]
    public void Test_NotNullOrEmpty()
    {
        Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty)).ShouldBe("test");
        Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), maxLength: 4, minLength: 0).ShouldBe("test");
        Check.NotNullOrEmpty(new List<string> { "test" }, nameof(Test_NotNullOrEmpty));

        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(null, nameof(Test_NotNullOrEmpty)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(string.Empty, nameof(Test_NotNullOrEmpty)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), minLength: 5));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(new List<string>(), nameof(Test_NotNullOrEmpty)));
    }

    /// <summary>
    /// 测试 - 验证类型是否可分配给指定基础类型
    /// </summary>
    [Fact]
    public void Test_AssignableTo()
    {
        Check.AssignableTo<object>(typeof(string), nameof(Test_AssignableTo)).ShouldBe(typeof(string));
        Check.AssignableTo<Parent>(typeof(Child), nameof(Test_AssignableTo)).ShouldBe(typeof(Child));
        Check.AssignableTo<Child>(typeof(Child2), nameof(Test_AssignableTo)).ShouldBe(typeof(Child2));
        Check.AssignableTo<Parent>(typeof(Child2), nameof(Test_AssignableTo)).ShouldBe(typeof(Child2));

        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child>(typeof(Parent), nameof(Test_AssignableTo)));
        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child2>(typeof(Child), nameof(Test_AssignableTo)));
        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child2>(typeof(Parent), nameof(Test_AssignableTo)));
    }

    /// <summary>
    /// 测试 - 验证字符串的长度是否符合指定的范围
    /// </summary>
    [Fact]
    public void Test_Length()
    {
        Check.Length("test", nameof(Test_Length), maxLength: 4).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 5).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 0).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 4).ShouldBe("test");

        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 0));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 5));
    }

    /// <summary>
    /// 测试 - 确保值为正数
    /// </summary>
    [Fact]
    public void Test_Positive()
    {
        Check.Positive(Conv.To<short>(1), nameof(Test_Positive)).ShouldBe(Conv.To<short>(1));
        Check.Positive(Conv.To<int>(1), nameof(Test_Positive)).ShouldBe(Conv.To<int>(1));
        Check.Positive(Conv.To<long>(1), nameof(Test_Positive)).ShouldBe(Conv.To<long>(1));
        Check.Positive(Decimal.One, nameof(Test_Positive)).ShouldBe(Decimal.One);
        Check.Positive(1.0f, nameof(Test_Positive)).ShouldBe(1.0f);
        Check.Positive(1.0, nameof(Test_Positive)).ShouldBe(1.0);

        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<short>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<int>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<long>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Decimal.Zero, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(0.0f, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(0.0, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<short>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<int>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<long>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-Decimal.One, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-1.0f, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-1.0, nameof(Test_Positive)));
    }

    /// <summary>
    /// 测试 - 确保值处于指定的范围内
    /// </summary>
    [Fact]
    public void Test_Range()
    {
        Check.Range(Conv.To<short>(1), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)).ShouldBe(Conv.To<short>(1));
        Check.Range(Conv.To<int>(1), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)).ShouldBe(Conv.To<int>(1));
        Check.Range(Conv.To<long>(1), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)).ShouldBe(Conv.To<long>(1));
        Check.Range(decimal.One, nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)).ShouldBe(decimal.One);
        Check.Range(1.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f).ShouldBe(1.0f);
        Check.Range(1.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0).ShouldBe(1.0);

        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<short>(0), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<int>(0), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<long>(0), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(decimal.Zero, nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(0.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f));
        Assert.Throws<ArgumentException>(() => Check.Range(0.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<short>(11), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<int>(11), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<long>(11), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<decimal>(11), nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(11.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f));
        Assert.Throws<ArgumentException>(() => Check.Range(11.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0));
    }

    class Parent;

    class Child : Parent;

    class Child2 : Child;
}