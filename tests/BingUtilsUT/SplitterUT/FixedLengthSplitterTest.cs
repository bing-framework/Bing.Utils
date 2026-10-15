using Bing.Text.Splitters;

namespace BingUtilsUT.SplitterUT;

/// <summary>
/// 验证固定长度分割对字符串和映射值的序列、列表及边界处理。
/// </summary>
public class FixedLengthSplitterTest
{
    /// <summary>
    /// 提供连续字符串和含空格字符串的定长切分样例。
    /// </summary>
    private static class OriginalStrings
    {
        /// <summary>
        /// 获取用于定长切分的连续字母字符串。
        /// </summary>
        public static string NormalString => "abcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// 获取包含空格的定长切分字符串。
        /// </summary>
        public static string IncludeWhiteSpaceString => "abcdefghijklmnopqrstuvwx yz";
    }

    /// <summary>
    /// 验证字符串可按固定长度切分为序列。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthEnumerable()
    {
        var enumerable = Splitter.FixedLength(3).Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(9);

        var list = enumerable.ToList();
        list.Count.ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }

    /// <summary>
    /// 验证定长序列切分遵守结果数量上限。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthEnumerable_With_Limit()
    {
        var enumerable = Splitter.FixedLength(3).Limit(3).Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(3);

        var list = enumerable.ToList();
        list.Count.ShouldBe(3);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
    }

    /// <summary>
    /// 验证定长序列切分可修剪片段两端的空白。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthEnumerable_With_Trim()
    {
        var @base = Splitter.FixedLength(3).Split(OriginalStrings.IncludeWhiteSpaceString);
        @base.Count().ShouldBe(9);

        var @baseList = @base.ToList();
        @baseList.Count.ShouldBe(9);

        @baseList[0].ShouldBe("abc");
        @baseList[1].ShouldBe("def");
        @baseList[2].ShouldBe("ghi");
        @baseList[3].ShouldBe("jkl");
        @baseList[4].ShouldBe("mno");
        @baseList[5].ShouldBe("pqr");
        @baseList[6].ShouldBe("stu");
        @baseList[7].ShouldBe("vwx");
        @baseList[8].ShouldBe(" yz");

        var enumerable = Splitter.FixedLength(3).TrimResults().Split(OriginalStrings.IncludeWhiteSpaceString);
        enumerable.Count().ShouldBe(9);

        var list = enumerable.ToList();
        list.Count.ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }

    /// <summary>
    /// 验证定长序列切分可按自定义规则修剪片段。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthEnumerable_With_CustomTrim()
    {
        var @base = Splitter.FixedLength(3).Split(OriginalStrings.IncludeWhiteSpaceString);
        @base.Count().ShouldBe(9);

        var @baseList = @base.ToList();
        @baseList.Count.ShouldBe(9);

        @baseList[0].ShouldBe("abc");
        @baseList[1].ShouldBe("def");
        @baseList[2].ShouldBe("ghi");
        @baseList[3].ShouldBe("jkl");
        @baseList[4].ShouldBe("mno");
        @baseList[5].ShouldBe("pqr");
        @baseList[6].ShouldBe("stu");
        @baseList[7].ShouldBe("vwx");
        @baseList[8].ShouldBe(" yz");

        var enumerable = Splitter.FixedLength(3).TrimResults(s => s.TrimStart()).Split(OriginalStrings.IncludeWhiteSpaceString);
        enumerable.Count().ShouldBe(9);

        var list = enumerable.ToList();
        list.Count.ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }

    /// <summary>
    /// 验证字符串可按固定长度切分为列表。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthList()
    {
        var list = Splitter.FixedLength(3).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }

    /// <summary>
    /// 验证定长列表切分遵守结果数量上限。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthList_With_Limit()
    {
        var list = Splitter.FixedLength(3).Limit(3).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(3);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
    }

    /// <summary>
    /// 验证定长列表切分可修剪片段两端的空白。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthList_With_Trim()
    {
        var @baseList = Splitter.FixedLength(3).SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        @baseList.Count().ShouldBe(9);

        @baseList[0].ShouldBe("abc");
        @baseList[1].ShouldBe("def");
        @baseList[2].ShouldBe("ghi");
        @baseList[3].ShouldBe("jkl");
        @baseList[4].ShouldBe("mno");
        @baseList[5].ShouldBe("pqr");
        @baseList[6].ShouldBe("stu");
        @baseList[7].ShouldBe("vwx");
        @baseList[8].ShouldBe(" yz");

        var list = Splitter.FixedLength(3).TrimResults().SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        list.Count().ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }

    /// <summary>
    /// 验证定长列表切分可按自定义规则修剪片段。
    /// </summary>
    [Fact]
    public void Test_StringToFixedLengthList_With_CustomTrim()
    {
        var @baseList = Splitter.FixedLength(3).SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        @baseList.Count().ShouldBe(9);

        @baseList[0].ShouldBe("abc");
        @baseList[1].ShouldBe("def");
        @baseList[2].ShouldBe("ghi");
        @baseList[3].ShouldBe("jkl");
        @baseList[4].ShouldBe("mno");
        @baseList[5].ShouldBe("pqr");
        @baseList[6].ShouldBe("stu");
        @baseList[7].ShouldBe("vwx");
        @baseList[8].ShouldBe(" yz");

        var list = Splitter.FixedLength(3).TrimResults(s => s.TrimStart()).SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        list.Count().ShouldBe(9);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
        list[3].ShouldBe("jkl");
        list[4].ShouldBe("mno");
        list[5].ShouldBe("pqr");
        list[6].ShouldBe("stu");
        list[7].ShouldBe("vwx");
        list[8].ShouldBe("yz");
    }
}
