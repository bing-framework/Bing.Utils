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

    [Fact]
    public void ContainsAnyKey_AnyKeyPresent_ReturnsTrue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        dict.ContainsAnyKey("a", "z").ShouldBeTrue();
    }

    [Fact]
    public void ContainsAnyKey_NoKeyPresent_ReturnsFalse()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        dict.ContainsAnyKey("x", "y").ShouldBeFalse();
    }

    [Fact]
    public void ContainsAllKey_AllKeysPresent_ReturnsTrue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        dict.ContainsAllKey("a", "b").ShouldBeTrue();
    }

    [Fact]
    public void ContainsAllKey_OneKeyMissing_ReturnsFalse()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        dict.ContainsAllKey("a", "b").ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // GetKey / GetOrDefault
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetKey_ValueExists_ReturnsCorrectKey()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["x"] = 42 };
        dict.GetKey(42).ShouldBe("x");
    }

    [Fact]
    public void GetKey_ValueNotFound_ReturnsDefault()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["x"] = 1 };
        dict.GetKey(99).ShouldBeNull();
    }

    [Fact]
    public void GetOrDefault_KeyExists_ReturnsValue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["k"] = 7 };
        dict.GetOrDefault("k").ShouldBe(7);
    }

    [Fact]
    public void GetOrDefault_KeyMissing_ReturnsDefaultValue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>();
        dict.GetOrDefault("missing", 99).ShouldBe(99);
    }

    [Fact]
    public void GetOrDefault_KeyMissing_NoDefaultSpecified_ReturnsTypeDefault()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>();
        dict.GetOrDefault("missing").ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // ToSortedDictionary
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToSortedDictionary_ReturnsSortedByKey()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1, ["c"] = 3 };
        var sorted = dict.ToSortedDictionary();
        sorted.Keys.First().ShouldBe("a");
        sorted.Keys.Last().ShouldBe("c");
    }

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

    [Fact]
    public void ToNameValueCollection_FromDictionary_ReturnsCorrectCollection()
    {
        var dict = new Dictionary<string, string> { ["name"] = "Alice", ["city"] = "Beijing" };
        var nvc = dict.ToNameValueCollection();
        nvc.ShouldNotBeNull();
        nvc["name"].ShouldBe("Alice");
        nvc["city"].ShouldBe("Beijing");
    }

    [Fact]
    public void ToNameValueCollection_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, string>)null!).ToNameValueCollection().ShouldBeNull();

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

    [Fact]
    public void ToNameValueCollection_FromKvpEnumerable_NullInput_ReturnsNull() =>
        ((IEnumerable<KeyValuePair<string, string>>)null!).ToNameValueCollection().ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // ToDynamic / ToDictionary (from KVP)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDynamic_ReturnsExpandoWithProperties()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "Bob", ["Age"] = 30 };
        dynamic result = dict.ToDynamic();
        ((string)result.Name).ShouldBe("Bob");
        ((int)result.Age).ShouldBe(30);
    }

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

    [Fact]
    public void ToQueryString_NullInput_ReturnsEmpty() =>
        ((IEnumerable<KeyValuePair<string, string>>)null!).ToQueryString().ShouldBe(string.Empty);

    [Fact]
    public void ToQueryString_EmptyList_ReturnsEmpty() =>
        new List<KeyValuePair<string, string>>().ToQueryString().ShouldBe("");

    // ─────────────────────────────────────────────────────────────────
    // ToDataTable (from IDictionary<string, object>)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDataTable_FromDictionary_ContainsColumns()
    {
        var dict = new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "Alice" };
        var table = dict.ToDataTable();
        table.Columns.Contains("Id").ShouldBeTrue();
        table.Columns.Contains("Name").ShouldBeTrue();
    }

    [Fact]
    public void ToDataTable_NullDictionary_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            ((IDictionary<string, object>)null!).ToDataTable());
}
