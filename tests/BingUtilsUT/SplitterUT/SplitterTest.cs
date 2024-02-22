using Bing.Text.Splitters;

namespace BingUtilsUT.SplitterUT;

public class SplitterTest
{
    private static class OriginalStrings
    {
        public static string NormalString => "a,b,c,d,e";
        public static string IncludeNullString => "a,,b,,,c,d,e";
        public static string IncludeWhiteSpaceString => "a, b ,c,d,e";
    }

    [Fact]
    public void Test_StringToEnumerable()
    {
        var enumerable = Splitter.On(",").Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();
        list.Count.ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToEnumerable_IncludeNull()
    {
        var @base = Splitter.On(",").Split(OriginalStrings.IncludeNullString);
        @base.Count().ShouldBe(8);

        var @baseList = @base.ToList();

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe("");
        @baseList[2].ShouldBe("b");
        @baseList[3].ShouldBe("");
        @baseList[4].ShouldBe("");
        @baseList[5].ShouldBe("c");
        @baseList[6].ShouldBe("d");
        @baseList[7].ShouldBe("e");

        var enumerable = Splitter.On(",").OmitEmptyStrings().Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToEnumerable_From_StringPattern()
    {
        var patter = ",";

        var enumerable = Splitter.OnPattern(patter).Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToEnumerable_From_RegexPattern()
    {
        var patter = new Regex(",");

        var enumerable = Splitter.On(patter).Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToEnumerable_With_Limit()
    {
        var enumerable = Splitter.On(",").Limit(3).Split(OriginalStrings.NormalString);
        enumerable.Count().ShouldBe(3);

        var list = enumerable.ToList();
        list.Count.ShouldBe(3);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
    }

    [Fact]
    public void Test_StringToEnumerable_With_Trim()
    {
        var @base = Splitter.On(",").Split(OriginalStrings.IncludeWhiteSpaceString);
        @base.Count().ShouldBe(5);

        var @baseList = @base.ToList();

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe(" b ");
        @baseList[2].ShouldBe("c");
        @baseList[3].ShouldBe("d");
        @baseList[4].ShouldBe("e");

        var enumerable = Splitter.On(",").TrimResults().Split(OriginalStrings.IncludeWhiteSpaceString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToEnumerable_With_CustomTrim()
    {
        var @base = Splitter.On(",").Split(OriginalStrings.IncludeWhiteSpaceString);
        @base.Count().ShouldBe(5);

        var @baseList = @base.ToList();

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe(" b ");
        @baseList[2].ShouldBe("c");
        @baseList[3].ShouldBe("d");
        @baseList[4].ShouldBe("e");

        var enumerable = Splitter.On(",").TrimResults(s=>s.TrimStart()).Split(OriginalStrings.IncludeWhiteSpaceString);
        enumerable.Count().ShouldBe(5);

        var list = enumerable.ToList();

        list[0].ShouldBe("a");
        list[1].ShouldBe("b ");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList()
    {
        var list = Splitter.On(",").SplitToList(OriginalStrings.NormalString);
        list.Count.ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList_IncludeNull()
    {
        var @baseList = Splitter.On(",").SplitToList(OriginalStrings.IncludeNullString);
        @baseList.Count().ShouldBe(8);

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe("");
        @baseList[2].ShouldBe("b");
        @baseList[3].ShouldBe("");
        @baseList[4].ShouldBe("");
        @baseList[5].ShouldBe("c");
        @baseList[6].ShouldBe("d");
        @baseList[7].ShouldBe("e");

        var list = Splitter.On(",").OmitEmptyStrings().SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList_From_StringPattern()
    {
        var patter = ",";

        var list = Splitter.OnPattern(patter).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList_From_RegexPattern()
    {
        var patter = new Regex(",");

        var list = Splitter.On(patter).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList_With_Limit()
    {
        var list = Splitter.On(",").Limit(3).SplitToList(OriginalStrings.NormalString);
        list.Count().ShouldBe(3);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
    }

    [Fact]
    public void Test_StringToList_With_Trim()
    {
        var @baseList = Splitter.On(",").SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        @baseList.Count().ShouldBe(5);

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe(" b ");
        @baseList[2].ShouldBe("c");
        @baseList[3].ShouldBe("d");
        @baseList[4].ShouldBe("e");

        var list = Splitter.On(",").TrimResults().SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        list.Count().ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }

    [Fact]
    public void Test_StringToList_With_CustomTrim()
    {
        var @baseList = Splitter.On(",").SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        @baseList.Count().ShouldBe(5);

        @baseList[0].ShouldBe("a");
        @baseList[1].ShouldBe(" b ");
        @baseList[2].ShouldBe("c");
        @baseList[3].ShouldBe("d");
        @baseList[4].ShouldBe("e");

        var list = Splitter.On(",").TrimResults(s=>s.TrimStart()).SplitToList(OriginalStrings.IncludeWhiteSpaceString);
        list.Count().ShouldBe(5);

        list[0].ShouldBe("a");
        list[1].ShouldBe("b ");
        list[2].ShouldBe("c");
        list[3].ShouldBe("d");
        list[4].ShouldBe("e");
    }
}