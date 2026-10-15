using System.Collections.Specialized;
using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Collections;

/// <summary>
/// <see cref="DictionaryExtensions"/> 扩展方法（Contains / Get / To 分组）单元测试
/// </summary>
public class DictionaryExtensionsAdditionalTests
{
    // ─────────────────────────────────────────────────────────────────
    // ContainsAnyKey / ContainsAllKey
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证候选键至少一个存在时返回肯定结果。
    /// </summary>
    [Fact]
    public void ContainsAnyKey_AnyKeyPresent_ReturnsTrue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        dict.ContainsAnyKey("a", "z").ShouldBeTrue();
    }

    /// <summary>
    /// 验证候选键均不存在时返回否定结果。
    /// </summary>
    [Fact]
    public void ContainsAnyKey_NoKeyPresent_ReturnsFalse()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        dict.ContainsAnyKey("x", "y").ShouldBeFalse();
    }

    /// <summary>
    /// 验证全部候选键存在时返回肯定结果。
    /// </summary>
    [Fact]
    public void ContainsAllKey_AllKeysPresent_ReturnsTrue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        dict.ContainsAllKey("a", "b").ShouldBeTrue();
    }

    /// <summary>
    /// 验证缺少任一候选键时返回否定结果。
    /// </summary>
    [Fact]
    public void ContainsAllKey_OneKeyMissing_ReturnsFalse()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        dict.ContainsAllKey("a", "b").ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // GetKey / GetOrDefault
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证按字典值查找对应键。
    /// </summary>
    [Fact]
    public void GetKey_ValueExists_ReturnsCorrectKey()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["x"] = 42 };
        dict.GetKey(42).ShouldBe("x");
    }

    /// <summary>
    /// 验证不存在的字典值返回默认键。
    /// </summary>
    [Fact]
    public void GetKey_ValueNotFound_ReturnsDefault()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["x"] = 1 };
        dict.GetKey(99).ShouldBeNull();
    }

    /// <summary>
    /// 验证已有键返回保存值。
    /// </summary>
    [Fact]
    public void GetOrDefault_KeyExists_ReturnsValue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["k"] = 7 };
        dict.GetOrDefault("k").ShouldBe(7);
    }

    /// <summary>
    /// 验证缺失键返回指定默认值。
    /// </summary>
    [Fact]
    public void GetOrDefault_KeyMissing_ReturnsDefaultValue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>();
        dict.GetOrDefault("missing", 99).ShouldBe(99);
    }

    /// <summary>
    /// 验证缺失键未指定默认值时返回类型默认值。
    /// </summary>
    [Fact]
    public void GetOrDefault_KeyMissing_NoDefaultSpecified_ReturnsTypeDefault()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>();
        dict.GetOrDefault("missing").ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // ToSortedDictionary
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证转换字典后的首尾键符合升序。
    /// </summary>
    [Fact]
    public void ToSortedDictionary_ReturnsSortedByKey()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1, ["c"] = 3 };
        var sorted = dict.ToSortedDictionary();
        sorted.Keys.First().ShouldBe("a");
        sorted.Keys.Last().ShouldBe("c");
    }

    /// <summary>
    /// 验证指定比较器的字典转换返回非空结果。
    /// </summary>
    [Fact]
    public void ToSortedDictionary_WithComparer_AppliesComparer()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 };
        var sorted = dict.ToSortedDictionary(StringComparer.OrdinalIgnoreCase);
        sorted.ShouldNotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────
    // ToNameValueCollection
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字典转换为名称值集合后保留键值。
    /// </summary>
    [Fact]
    public void ToNameValueCollection_FromDictionary_ReturnsCorrectCollection()
    {
        var dict = new Dictionary<string, string> { ["name"] = "Alice", ["city"] = "Beijing" };
        var nvc = dict.ToNameValueCollection();
        nvc.ShouldNotBeNull();
        nvc["name"].ShouldBe("Alice");
        nvc["city"].ShouldBe("Beijing");
    }

    /// <summary>
    /// 验证空字典转换为名称值集合时返回空引用。
    /// </summary>
    [Fact]
    public void ToNameValueCollection_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, string>)null!).ToNameValueCollection().ShouldBeNull();

    /// <summary>
    /// 验证键值对序列转换为名称值集合后保留指定键值。
    /// </summary>
    [Fact]
    public void ToNameValueCollection_FromKvpEnumerable_ReturnsCorrectCollection()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("a", "1"),
            new("b", "2")
        };
        var nvc = pairs.ToNameValueCollection();
        nvc["a"].ShouldBe("1");
    }

    /// <summary>
    /// 验证空键值对序列转换为名称值集合时返回空引用。
    /// </summary>
    [Fact]
    public void ToNameValueCollection_FromKvpEnumerable_NullInput_ReturnsNull() =>
        ((IEnumerable<KeyValuePair<string, string>>)null!).ToNameValueCollection().ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // ToDynamic / ToDictionary (from KVP)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字典键值转换为可动态访问的属性。
    /// </summary>
    [Fact]
    public void ToDynamic_ReturnsExpandoWithProperties()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "Bob", ["Age"] = 30 };
        dynamic result = dict.ToDynamic();
        ((string)result.Name).ShouldBe("Bob");
        ((int)result.Age).ShouldBe(30);
    }

    /// <summary>
    /// 验证键值对序列转换为字典后保留键值。
    /// </summary>
    [Fact]
    public void ToDictionary_FromKvpEnumerable_ReturnsCorrectDictionary()
    {
        var pairs = new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) };
        var dict = DictConvExtensions.ToDictionary(pairs);
        dict["a"].ShouldBe(1);
        dict["b"].ShouldBe(2);
    }

    // ─────────────────────────────────────────────────────────────────
    // ToQueryString (from KVP enumerable)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证查询文本包含各键值参数。
    /// </summary>
    [Fact]
    public void ToQueryString_KvpEnumerable_BuildsCorrectQueryString()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("name", "Alice"),
            new("age", "30")
        };
        var qs = pairs.ToQueryString();
        qs.ShouldContain("name=Alice");
        qs.ShouldContain("age=30");
    }

    /// <summary>
    /// 验证空序列的查询文本为空。
    /// </summary>
    [Fact]
    public void ToQueryString_NullInput_ReturnsEmpty() =>
        ((IEnumerable<KeyValuePair<string, string>>)null!).ToQueryString().ShouldBe(string.Empty);

    /// <summary>
    /// 验证空列表的查询文本为空。
    /// </summary>
    [Fact]
    public void ToQueryString_EmptyList_ReturnsEmpty() =>
        new List<KeyValuePair<string, string>>().ToQueryString().ShouldBe("");

    /// <summary>
    /// 验证集合查询字符串入口应编码空格与特殊字符并保留空键。
    /// </summary>
    [Fact]
    public void ToQueryString_KvpEnumerableWithSpecialValues_UsesEncodedSharedImplementation()
    {
        // Arrange
        var pairs = new[]
        {
            new KeyValuePair<string, string>(string.Empty, "a b"),
            new KeyValuePair<string, string>("x&y", "z=")
        };

        // Act
        var result = pairs.ToQueryString();

        // Assert
        result.ShouldBe("=a%20b&x%26y=z%3D");
    }

    // ─────────────────────────────────────────────────────────────────
    // ToDataTable (from IDictionary<string, object>)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证字典转换的数据表包含对应列。
    /// </summary>
    [Fact]
    public void ToDataTable_FromDictionary_ContainsColumns()
    {
        var dict = new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Alice" };
        var table = dict.ToDataTable();
        table.Columns.Contains("Id").ShouldBeTrue();
        table.Columns.Contains("Name").ShouldBeTrue();
    }

    /// <summary>
    /// 验证数据表转换拒绝空字典。
    /// </summary>
    [Fact]
    public void ToDataTable_NullDictionary_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IDictionary<string, object>)null!).ToDataTable());
}
