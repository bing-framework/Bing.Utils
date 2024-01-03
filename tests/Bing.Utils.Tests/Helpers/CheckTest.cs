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
        Check.NotNullOrEmpty(new List<string>{"test"}, nameof(Test_NotNullOrEmpty));
        
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
        Check.Length("test", nameof(Test_Length), maxLength:4, minLength: 0).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength:4, minLength: 4).ShouldBe("test");
        
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 0));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 5));
    }

    class Parent;

    class Child : Parent;

    class Child2 : Child;
}