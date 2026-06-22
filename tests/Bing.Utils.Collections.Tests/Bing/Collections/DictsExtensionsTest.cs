using System;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Shouldly;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：DictsExtensions 字典工具扩展方法
/// </summary>
[Trait("CollUT", "DictsExtensions")]
public class DictsExtensionsTest
{
    #region AddRange — 批量添加

    /// <summary>
    /// 测试目的：AddRange 应将另一个字典的所有键值对添加到源字典
    /// </summary>
    [Fact]
    public void AddRange_FromAnotherDict_AddsAllPairs()
    {
        // Arrange
        var source = new Dictionary<string, int> { { "a", 1 } };
        var other = new Dictionary<string, int> { { "b", 2 }, { "c", 3 } };

        // Act
        source.AddRange(other);

        // Assert
        source.Count.ShouldBe(3);
        source["b"].ShouldBe(2);
        source["c"].ShouldBe(3);
    }

    #endregion

    #region Add(KeyValuePair) — 键值对添加

    /// <summary>
    /// 测试目的：Add(KeyValuePair) 扩展方法应将键值对添加到字典
    /// </summary>
    [Fact]
    public void Add_KeyValuePair_AddsPairToDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var pair = new KeyValuePair<string, int>("key1", 42);

        // Act
        dict.Add(pair);

        // Assert
        dict.ContainsKey("key1").ShouldBeTrue();
        dict["key1"].ShouldBe(42);
    }

    #endregion

    #region AddValueOrOverride — 添加或覆盖

    /// <summary>
    /// 测试目的：AddValueOrOverride 对不存在的键应正常添加
    /// </summary>
    [Fact]
    public void AddValueOrOverride_NewKey_AddsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        dict.AddValueOrOverride("key1", 100);

        // Assert
        dict["key1"].ShouldBe(100);
    }

    /// <summary>
    /// 测试目的：AddValueOrOverride 对已存在的键应覆盖值
    /// </summary>
    [Fact]
    public void AddValueOrOverride_ExistingKey_OverridesValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 10 } };

        // Act
        dict.AddValueOrOverride("key1", 999);

        // Assert
        dict["key1"].ShouldBe(999);
        dict.Count.ShouldBe(1);
    }

    #endregion

    #region AddValueIfNotExist — 不存在时添加

    /// <summary>
    /// 测试目的：AddValueIfNotExist 对不存在的键应添加值
    /// </summary>
    [Fact]
    public void AddValueIfNotExist_NewKey_AddsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        dict.AddValueIfNotExist("key1", 55);

        // Assert
        dict["key1"].ShouldBe(55);
    }

    /// <summary>
    /// 测试目的：AddValueIfNotExist 对已存在的键不应覆盖原值
    /// </summary>
    [Fact]
    public void AddValueIfNotExist_ExistingKey_DoesNotOverride()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 10 } };

        // Act
        dict.AddValueIfNotExist("key1", 999);

        // Assert
        dict["key1"].ShouldBe(10);
    }

    /// <summary>
    /// 测试目的：AddValueIfNotExist(valueCalculator) 对不存在的键应使用工厂函数计算值
    /// </summary>
    [Fact]
    public void AddValueIfNotExist_WithCalculator_AddsCalculatedValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        dict.AddValueIfNotExist("key1", k => k.Length);

        // Assert
        dict["key1"].ShouldBe(4); // "key1".Length = 4
    }

    #endregion

    #region GetValueOrDefault — 获取或默认值

    /// <summary>
    /// 测试目的：GetValueOrDefault(key, valueCalculator) 对存在的键应返回实际值
    /// </summary>
    [Fact]
    public void GetValueOrDefault_ExistingKey_ReturnsActualValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 42 } };

        // Act
        var result = dict.GetValueOrDefault("key1", k => -1);

        // Assert
        result.ShouldBe(42);
    }

    /// <summary>
    /// 测试目的：GetValueOrDefault(key, valueCalculator) 对不存在的键应返回 calculator 计算值
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKey_ReturnsCalculatedDefault()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 42 } };

        // Act
        var result = dict.GetValueOrDefault("missing", k => -99);

        // Assert
        result.ShouldBe(-99);
    }

    #endregion

    #region GetValueOrAdd — 获取或添加

    /// <summary>
    /// 测试目的：GetValueOrAdd(key, value) 对存在的键应返回已有值，不添加新值
    /// </summary>
    [Fact]
    public void GetValueOrAdd_ExistingKey_ReturnsExistingValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 10 } };

        // Act
        var result = dict.GetValueOrAdd("key1", 999);

        // Assert
        result.ShouldBe(10);
        dict.Count.ShouldBe(1);
    }

    /// <summary>
    /// 测试目的：GetValueOrAdd(key, value) 对不存在的键应添加并返回新值
    /// </summary>
    [Fact]
    public void GetValueOrAdd_MissingKey_AddsAndReturnsNewValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = dict.GetValueOrAdd("key1", 77);

        // Assert
        result.ShouldBe(77);
        dict.ContainsKey("key1").ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：GetValueOrAdd(key, newValueCreator) 对不存在的键应使用工厂函数创建值
    /// </summary>
    [Fact]
    public void GetValueOrAdd_MissingKey_WithCreator_AddsCreatedValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = dict.GetValueOrAdd("hello", k => k.Length);

        // Assert
        result.ShouldBe(5); // "hello".Length = 5
        dict["hello"].ShouldBe(5);
    }

    #endregion

    #region SetValue — 设置值

    /// <summary>
    /// 测试目的：SetValue 应无论键是否存在都能设置值
    /// </summary>
    [Fact]
    public void SetValue_NewKey_SetsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        dict.SetValue("key1", 123);

        // Assert
        dict["key1"].ShouldBe(123);
    }

    /// <summary>
    /// 测试目的：SetValue 对已存在的键应覆盖旧值
    /// </summary>
    [Fact]
    public void SetValue_ExistingKey_OverridesOldValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 1 } };

        // Act
        dict.SetValue("key1", 456);

        // Assert
        dict["key1"].ShouldBe(456);
    }

    #endregion

    #region AddValueOrUpdate — 添加或更新

    /// <summary>
    /// 测试目的：AddValueOrUpdate 对不存在的键应使用 insertFunc 插入
    /// </summary>
    [Fact]
    public void AddValueOrUpdate_NewKey_UsesInsertFunc()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        dict.AddValueOrUpdate("key1", k => k.Length, (k, v) => v + 10);

        // Assert
        dict["key1"].ShouldBe(4); // "key1".Length = 4
    }

    /// <summary>
    /// 测试目的：AddValueOrUpdate 对已存在的键应使用 updateFunc 更新
    /// </summary>
    [Fact]
    public void AddValueOrUpdate_ExistingKey_UsesUpdateFunc()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 5 } };

        // Act
        dict.AddValueOrUpdate("key1", k => k.Length, (k, v) => v * 2);

        // Assert
        dict["key1"].ShouldBe(10); // 5 * 2 = 10
    }

    #endregion

    #region GetValueOrDefaultCascading — 级联获取

    /// <summary>
    /// 测试目的：GetValueOrDefaultCascading 在第一个字典找到键时应返回对应值
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_KeyInFirstDict_ReturnsFirstValue()
    {
        // Arrange
        var dict1 = new Dictionary<string, int> { { "key", 1 } };
        var dict2 = new Dictionary<string, int> { { "key", 2 } };
        var dicts = new List<IDictionary<string, int>> { dict1, dict2 };

        // Act
        var result = dicts.GetValueOrDefaultCascading("key", -1);

        // Assert
        result.ShouldBe(1);
    }

    /// <summary>
    /// 测试目的：GetValueOrDefaultCascading 在所有字典均无该键时应返回默认值
    /// </summary>
    [Fact]
    public void GetValueOrDefaultCascading_KeyNotFound_ReturnsDefault()
    {
        // Arrange
        var dict1 = new Dictionary<string, int> { { "a", 1 } };
        var dict2 = new Dictionary<string, int> { { "b", 2 } };
        var dicts = new List<IDictionary<string, int>> { dict1, dict2 };

        // Act
        var result = dicts.GetValueOrDefaultCascading("missing", -99);

        // Assert
        result.ShouldBe(-99);
    }

    #endregion
}
