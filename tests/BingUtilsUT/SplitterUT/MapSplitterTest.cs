using Bing.Text.Splitters;

namespace BingUtilsUT.SplitterUT;

/// <summary>
/// 验证键值映射字符串的分割、字典转换和固定长度解析。
/// </summary>
public class MapSplitterTest
{
    /// <summary>
    /// 提供分隔符分组和固定长度分组的键值对字符串样例。
    /// </summary>
    private static class OriginalStrings
    {
        /// <summary>
        /// 获取使用分隔符分开的键值对字符串。
        /// </summary>
        public static string NormalMapString => "a=1&b=2&c=3&d=4&e=5";

        /// <summary>
        /// 获取由连续固定长度键值片段组成的字符串。
        /// </summary>
        public static string FixedLengthMapString => "a=1b=2c=3d=4e=5";
    }

    /// <summary>
    /// 验证分隔的键值对字符串可转换为键值对序列。
    /// </summary>
    [Fact]
    public void Test_StringToKvp()
    {
        var kvp = Splitter.On("&").WithKeyValueSeparator("=").Split(OriginalStrings.NormalMapString);
        kvp.Count().ShouldBe(5);

        var dict = kvp.ToDictionary(k => k.Key, v => v.Value);
        dict.Count.ShouldBe(5);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
        dict["d"].ShouldBe("4");
        dict["e"].ShouldBe("5");
    }

    /// <summary>
    /// 验证键值对序列转换遵守结果数量上限。
    /// </summary>
    [Fact]
    public void Test_StringToKvp_With_Limit()
    {
        var kvp = Splitter.On("&").WithKeyValueSeparator("=").Limit(3).Split(OriginalStrings.NormalMapString);
        kvp.Count().ShouldBe(3);

        var dict = kvp.ToDictionary(k => k.Key, v => v.Value);
        dict.Count.ShouldBe(3);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
    }

    /// <summary>
    /// 验证分隔的键值对字符串可转换为字典。
    /// </summary>
    [Fact]
    public void Test_StringToDictionary()
    {
        var dict = Splitter.On("&").WithKeyValueSeparator("=").SplitToDictionary(OriginalStrings.NormalMapString);
        dict.Count().ShouldBe(5);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
        dict["d"].ShouldBe("4");
        dict["e"].ShouldBe("5");
    }

    /// <summary>
    /// 验证字典转换遵守键值对数量上限。
    /// </summary>
    [Fact]
    public void Test_StringToDictionary_With_Limit()
    {
        var dict = Splitter.On("&").WithKeyValueSeparator("=").Limit(3).SplitToDictionary(OriginalStrings.NormalMapString);
        dict.Count().ShouldBe(3);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
    }

    /// <summary>
    /// 验证定长键值对字符串可转换为键值对序列。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthKvp()
    {
        var kvp = Splitter.FixedLength(3).WithKeyValueSeparator("=").Split(OriginalStrings.FixedLengthMapString);
        kvp.Count().ShouldBe(5);

        var dict = kvp.ToDictionary(k => k.Key, v => v.Value);
        dict.Count.ShouldBe(5);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
        dict["d"].ShouldBe("4");
        dict["e"].ShouldBe("5");
    }

    /// <summary>
    /// 验证定长键值对字符串可转换为字典。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthDictionary()
    {
        var dict = Splitter.FixedLength(3).WithKeyValueSeparator("=").SplitToDictionary(OriginalStrings.FixedLengthMapString);
        dict.Count().ShouldBe(5);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
        dict["d"].ShouldBe("4");
        dict["e"].ShouldBe("5");
    }
}
