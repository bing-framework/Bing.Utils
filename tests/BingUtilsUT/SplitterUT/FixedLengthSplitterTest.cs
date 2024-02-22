using Bing.Text.Splitters;

namespace BingUtilsUT.SplitterUT;

public class FixedLengthSplitterTest
{
    private static class OriginalStrings
    {
        public static string NormalString => "abcdefghijklmnopqrstuvwxyz";
        public static string IncludeWhiteSpaceString => "abcdefghijklmnopqrstuvwx yz";
    }

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

        var enumerable = Splitter.FixedLength(3).TrimResults(s=>s.TrimStart()).Split(OriginalStrings.IncludeWhiteSpaceString);
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

    [Fact]
    public void Test_StringToFixedLengthList_With_Limit()
    {
        var list = Splitter.FixedLength(3).Limit(3).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(3);

        list[0].ShouldBe("abc");
        list[1].ShouldBe("def");
        list[2].ShouldBe("ghi");
    }

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

        var list = Splitter.FixedLength(3).TrimResults(s=>s.TrimStart()).SplitToList(OriginalStrings.IncludeWhiteSpaceString);
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