using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="DictionaryExtensions"/> 单元测试
/// </summary>
public class DictionaryExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // Sort — 默认排序
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_DefaultComparer_ReturnsSortedDictionary()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
        var sorted = dict.Sort();
        sorted.Keys.ToList().ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Sort_NullDictionary_ThrowsArgumentNullException()
    {
        IDictionary<string, int> dict = null!;
        Should.Throw<ArgumentNullException>(() => dict.Sort());
    }

    // ─────────────────────────────────────────────────────────────────
    // Sort — 自定义比较器（逆序）
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_WithComparer_ReturnsSortedByComparer()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["c"] = 3, ["a"] = 1, ["b"] = 2 };
        var sorted = dict.Sort(StringComparer.Ordinal);
        sorted.Keys.ToList().ShouldBe(new[] { "a", "b", "c" });
    }

    [Fact]
    public void Sort_NullComparer_ThrowsArgumentNullException()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        Should.Throw<ArgumentNullException>(() => dict.Sort(null!));
    }

    // ─────────────────────────────────────────────────────────────────
    // SortByValue
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SortByValue_ReturnsDictSortedByValue()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["b"] = 2, ["a"] = 3, ["c"] = 1 };
        var sorted = dict.SortByValue();
        sorted.Values.ToList().ShouldBe(new[] { 1, 2, 3 });
    }

    // ─────────────────────────────────────────────────────────────────
    // ToQueryString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToQueryString_SingleEntry_ReturnsPair()
    {
        IDictionary<string, string> dict = new Dictionary<string, string> { ["key"] = "value" };
        dict.ToQueryString().ShouldBe("key=value");
    }

    [Fact]
    public void ToQueryString_MultipleEntries_ReturnsConcatenated()
    {
        IDictionary<string, string> dict = new Dictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = "2"
        };
        var result = dict.ToQueryString();
        result.ShouldBe("a=1&b=2");
    }

    [Fact]
    public void ToQueryString_EmptyDictionary_ReturnsEmpty()
    {
        IDictionary<string, string> dict = new Dictionary<string, string>();
        dict.ToQueryString().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToQueryString_NullDictionary_ReturnsEmpty()
    {
        IDictionary<string, string> dict = null!;
        dict.ToQueryString().ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试目的：既有字典入口应对键和值进行百分号编码并忽略空值。
    /// </summary>
    [Fact]
    public void ToQueryString_SpecialCharactersAndNullValue_UsesEncodedSharedImplementation()
    {
        // Arrange
        IDictionary<string, string> dict = new Dictionary<string, string>
        {
            ["a b"] = "x&y",
            ["ignored"] = null
        };

        // Act
        var result = dict.ToQueryString();

        // Assert
        result.ShouldBe("a%20b=x%26y");
    }

    // ─────────────────────────────────────────────────────────────────
    // ToHashTable
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToHashTable_ContainsAllEntries()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["x"] = 10, ["y"] = 20 };
        var ht = dict.ToHashTable();
        ht["x"].ShouldBe(10);
        ht["y"].ShouldBe(20);
    }

    [Fact]
    public void ToHashTable_EmptyDictionary_ReturnsEmptyHashtable()
    {
        IDictionary<string, int> dict = new Dictionary<string, int>();
        dict.ToHashTable().Count.ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // Reverse
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reverse_SwapsKeysAndValues()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var reversed = dict.Reverse();
        reversed[1].ShouldBe("a");
        reversed[2].ShouldBe("b");
    }

    [Fact]
    public void Reverse_NullDictionary_ThrowsArgumentNullException()
    {
        IDictionary<string, int> dict = null!;
        Should.Throw<ArgumentNullException>(() => dict.Reverse());
    }

    // ─────────────────────────────────────────────────────────────────
    // EqualsTo
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EqualsTo_SameDictionaries_ReturnsTrue()
    {
        IDictionary<string, int> a = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 };
        IDictionary<string, int> b = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 };
        a.EqualsTo(b).ShouldBeTrue();
    }

    [Fact]
    public void EqualsTo_DifferentValues_ReturnsFalse()
    {
        IDictionary<string, int> a = new Dictionary<string, int> { ["x"] = 1 };
        IDictionary<string, int> b = new Dictionary<string, int> { ["x"] = 99 };
        a.EqualsTo(b).ShouldBeFalse();
    }

    [Fact]
    public void EqualsTo_DifferentSize_ReturnsFalse()
    {
        IDictionary<string, int> a = new Dictionary<string, int> { ["x"] = 1 };
        IDictionary<string, int> b = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 };
        a.EqualsTo(b).ShouldBeFalse();
    }

    [Fact]
    public void EqualsTo_BothEmpty_ReturnsTrue()
    {
        IDictionary<string, int> a = new Dictionary<string, int>();
        IDictionary<string, int> b = new Dictionary<string, int>();
        a.EqualsTo(b).ShouldBeTrue();
    }

    [Fact]
    public void EqualsTo_NullSource_ThrowsArgumentNullException()
    {
        IDictionary<string, int> a = null!;
        IDictionary<string, int> b = new Dictionary<string, int>();
        Should.Throw<ArgumentNullException>(() => a.EqualsTo(b));
    }

    [Fact]
    public void EqualsTo_NullTarget_ThrowsArgumentNullException()
    {
        IDictionary<string, int> a = new Dictionary<string, int>();
        IDictionary<string, int> b = null!;
        Should.Throw<ArgumentNullException>(() => a.EqualsTo(b));
    }

    // ─────────────────────────────────────────────────────────────────
    // FillFormDataStream / FillFormDataStreamAsync
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void FillFormDataStream_WritesQueryStringToStream()
    {
        IDictionary<string, string> data = new Dictionary<string, string> { ["k"] = "v" };
        using var ms = new MemoryStream();
        data.FillFormDataStream(ms);
        ms.Position = 0;
        var result = new StreamReader(ms).ReadToEnd();
        result.ShouldContain("k=v");
    }

    [Fact]
    public async Task FillFormDataStreamAsync_WritesQueryStringToStream()
    {
        IDictionary<string, string> data = new Dictionary<string, string> { ["x"] = "1" };
        using var ms = new MemoryStream();
        await data.FillFormDataStreamAsync(ms);
        ms.Position = 0;
        var result = new StreamReader(ms).ReadToEnd();
        result.ShouldContain("x=1");
    }
}
