using System.Linq;
using Bing.Text.Splitters;

namespace Bing.Utils.Tests.Text.Splitters;

public class MapSplitterTest
{
    private static class OriginalStrings
    {
        public static string NormalMapString => "a=1&b=2&c=3&d=4&e=5";

        public static string FixedLengthMapString => "a=1b=2c=3d=4e=5";
    }

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

    [Fact]
    public void Test_StringToDictionary_With_Limit()
    {
        var dict = Splitter.On("&").WithKeyValueSeparator("=").Limit(3).SplitToDictionary(OriginalStrings.NormalMapString);
        dict.Count().ShouldBe(3);

        dict["a"].ShouldBe("1");
        dict["b"].ShouldBe("2");
        dict["c"].ShouldBe("3");
    }

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