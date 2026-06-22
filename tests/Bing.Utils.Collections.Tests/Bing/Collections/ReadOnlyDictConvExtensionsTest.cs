using System;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：ReadOnlyDictConvExtensions 只读字典转换扩展方法（AsReadOnlyDictionary 系列）
/// </summary>
[Trait("CollUT", "ReadOnlyDictConvExtensions")]
public class ReadOnlyDictConvExtensionsTest
{
    #region AsReadOnlyDictionary(IEnumerable<KVP>) — 从 KVP 集合转换

    /// <summary>
    /// 测试目的：从 KVP 集合转换为只读字典应包含所有键值对
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_FromKvpEnumerable_ContainsAllPairs()
    {
        // Arrange
        var pairs = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3),
        };

        // Act
        var result = pairs.AsReadOnlyDictionary();

        // Assert
        result.Count.ShouldBe(3);
        result["a"].ShouldBe(1);
        result["b"].ShouldBe(2);
        result["c"].ShouldBe(3);
    }

    /// <summary>
    /// 测试目的：从 KVP 集合转换后应返回 IReadOnlyDictionary 接口类型
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_FromKvpEnumerable_ReturnsReadOnlyInterface()
    {
        // Arrange
        var pairs = new[] { new KeyValuePair<string, int>("key", 42) };

        // Act
        var result = pairs.AsReadOnlyDictionary();

        // Assert
        result.ShouldBeAssignableTo<IReadOnlyDictionary<string, int>>();
    }

    /// <summary>
    /// 测试目的：从 null KVP 集合转换应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_NullKvpSource_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<KeyValuePair<string, int>> source = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => source.AsReadOnlyDictionary());
    }

    #endregion

    #region AsReadOnlyDictionary(IEnumerable<KVP>, comparer) — 带比较器

    /// <summary>
    /// 测试目的：带比较器的版本应使用该比较器进行键比较（忽略大小写）
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithComparer_UsesComparer()
    {
        // Arrange
        var pairs = new[]
        {
            new KeyValuePair<string, int>("Key1", 100),
            new KeyValuePair<string, int>("Key2", 200),
        };

        // Act: 使用大小写不敏感比较器
        var result = pairs.AsReadOnlyDictionary(StringComparer.OrdinalIgnoreCase);

        // Assert: 通过小写键也能访问
        result["key1"].ShouldBe(100);
        result["KEY2"].ShouldBe(200);
    }

    #endregion

    #region AsReadOnlyDictionary(IEnumerable<TValue>, keySelector) — 值集合转换（按键选择器）

    /// <summary>
    /// 测试目的：通过键选择器从值集合创建只读字典应正确映射
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithKeySelector_MapsCorrectly()
    {
        // Arrange: 使用字符串作为值，用其长度作为键
        var values = new[] { "a", "bb", "ccc" };

        // Act
        var result = values.AsReadOnlyDictionary(v => v.Length);

        // Assert
        result[1].ShouldBe("a");
        result[2].ShouldBe("bb");
        result[3].ShouldBe("ccc");
    }

    /// <summary>
    /// 测试目的：通过键选择器从空集合转换应返回空只读字典
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithKeySelector_EmptySource_ReturnsEmpty()
    {
        // Act
        var result = Array.Empty<string>().AsReadOnlyDictionary(v => v.Length);

        // Assert
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：通过键选择器且 keySelector 为 null 时应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithNullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var values = new[] { "a", "b" };
        Func<string, int> keySelector = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => values.AsReadOnlyDictionary(keySelector));
    }

    #endregion

    #region AsReadOnlyDictionary(IEnumerable<TValue>, keySelector, comparer) — 带键选择器和比较器

    /// <summary>
    /// 测试目的：带键选择器和比较器版本应结合使用两者
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithKeySelectorAndComparer_Works()
    {
        // Arrange: 使用大小写不敏感的字符串键
        var values = new[] { "apple", "banana" };

        // Act: 用字符串本身作为键（大小写不敏感比较器）
        var result = values.AsReadOnlyDictionary(v => v, StringComparer.OrdinalIgnoreCase);

        // Assert
        result["APPLE"].ShouldBe("apple");
        result["BANANA"].ShouldBe("banana");
    }

    #endregion

    #region AsReadOnlyDictionary(IEnumerable<TSource>, keySelector, elementSelector) — 带元素选择器

    /// <summary>
    /// 测试目的：带键选择器和元素选择器版本应对每个元素分别投影键和值
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_WithKeyAndElementSelector_ProjectsBoth()
    {
        // Arrange: 确保每个元素长度唯一，避免重复键
        var source = new[] { "a", "bb", "ccc" };

        // Act: 键=长度，值=大写形式
        var result = source.AsReadOnlyDictionary(s => s.Length, s => s.ToUpper());

        // Assert
        result[1].ShouldBe("A");
        result[2].ShouldBe("BB");
        result[3].ShouldBe("CCC");
    }

    /// <summary>
    /// 测试目的：带全部参数的版本（keySelector, elementSelector, comparer）应正确工作
    /// </summary>
    [Fact]
    public void AsReadOnlyDictionary_AllParams_ReturnsCorrectReadOnlyDict()
    {
        // Arrange
        var source = new[] { "a", "bb", "ccc" };

        // Act: 键=长度（int，默认比较器），值=字符串本身
        var result = source.AsReadOnlyDictionary(
            s => s.Length,
            s => s,
            EqualityComparer<int>.Default);

        // Assert
        result[1].ShouldBe("a");
        result[2].ShouldBe("bb");
        result[3].ShouldBe("ccc");
    }

    #endregion
}
