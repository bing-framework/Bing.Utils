using Bing.Text.Joiners;
namespace Bing.Text;
/// <summary>
/// 测试类：覆盖 `Joiner` 相关行为。
/// </summary>
[Trait("Bing.Text", "Joiner")]
public class JoinerTest
{
    /// <summary>
    /// 测试用例：验证 `Join` 在 `SkipNullsEnabled` 场景下，结果为 `SkipsNullAndWhitespace`。
    /// </summary>
    [Fact]
    public void Join_SkipNullsEnabled_SkipsNullAndWhitespace()
    {
        var joiner = Joiner.On(",").SkipNulls();
        var input = new[] { "A", null, " ", "B" };
        var result = joiner.Join(input);
        result.ShouldBe("A,B");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `UseForNullEnabled` 场景下，结果为 `ReplacesNullValues`。
    /// </summary>
    [Fact]
    public void Join_UseForNullEnabled_ReplacesNullValues()
    {
        var joiner = Joiner.On(",").UseForNull("N/A");
        var input = new[] { "A", null, "B" };
        var result = joiner.Join(input);
        result.ShouldBe("A,N/A,B");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapModeWithOddItems` 场景下，结果为 `UsesDefaultValueForLastKey`。
    /// </summary>
    [Fact]
    public void Join_MapModeWithOddItems_UsesDefaultValueForLastKey()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=');
        var input = new[] { "k1", "v1", "k2" };
        var result = mapJoiner.Join(input);
        result.ShouldBe("k1=v1&k2=");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapModeSkipWhenEither` 场景下，结果为 `SkipsIncompletePairs`。
    /// </summary>
    [Fact]
    public void Join_MapModeSkipWhenEither_SkipsIncompletePairs()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=').SkipNulls(SkipNullType.WhenEither);
        var input = new[] { "k1", "v1", "k2", null, "", "v3" };
        var result = mapJoiner.Join(input);
        result.ShouldBe("k1=v1");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `TupleModeUseForNull` 场景下，结果为 `ReplacesMissingTupleValues`。
    /// </summary>
    [Fact]
    public void Join_TupleModeUseForNull_ReplacesMissingTupleValues()
    {
        var tupleJoiner = Joiner.On("&")
            .WithKeyValueSeparator('=')
            .FromTuple()
            .UseForNull<string, string>((k, _) => k ?? "K", (_, v) => v ?? "V");
        var input = new List<(string, string)>
        {
            ("A", "1"),
            (null, "2"),
            ("C", null)
        };
        var result = tupleJoiner.Join(input);
        result.ShouldBe("A=1&K=2&C=V");
    }
    /// <summary>
    /// 测试用例：验证 `AppendTo` 在 `WithBuilder` 场景下，结果为 `ReturnsSameBuilderAndExpectedText`。
    /// </summary>
    [Fact]
    public void AppendTo_WithBuilder_ReturnsSameBuilderAndExpectedText()
    {
        var builder = new StringBuilder();
        var joiner = Joiner.On("-");
        var resultBuilder = joiner.AppendTo(builder, "x", "y", "z");
        resultBuilder.ShouldBeSameAs(builder);
        resultBuilder.ToString().ShouldBe("x-y-z");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `NullList` 场景下，结果为 `ReturnsEmptyString`。
    /// </summary>
    [Fact]
    public void Join_NullList_ReturnsEmptyString()
    {
        var joiner = Joiner.On(",");
        var result = joiner.Join((IEnumerable<string>)null);
        result.ShouldBeEmpty();
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `UseForNullIndexedFunc` 场景下，结果为 `CurrentlySkipsNullValues`。
    /// </summary>
    [Fact]
    public void Join_UseForNullIndexedFunc_CurrentlySkipsNullValues()
    {
        var joiner = Joiner.On(",").UseForNull((_, index) => $"N{index}");
        var input = new[] { "A", null, "B" };
        var result = joiner.Join(input);
        result.ShouldBe("A,B");
    }
    /// <summary>
    /// 测试用例：验证 `AppendTo` 在 `UseForNullIndexedFunc` 场景下，结果为 `CurrentlySkipsNullValues`。
    /// </summary>
    [Fact]
    public void AppendTo_UseForNullIndexedFunc_CurrentlySkipsNullValues()
    {
        var joiner = Joiner.On("|").UseForNull((_, index) => $"R{index}");
        var builder = new StringBuilder();
        var input = new[] { "A", null, "B" };
        joiner.AppendTo(builder, input);
        builder.ToString().ShouldBe("A|B");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapUseForNullConstants` 场景下，结果为 `ReplacesMissingKeyAndValue`。
    /// </summary>
    [Fact]
    public void Join_MapUseForNullConstants_ReplacesMissingKeyAndValue()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=').UseForNull("DK", "DV");
        var input = new[] { null, "v1", "k2", null };
        var result = mapJoiner.Join(input);
        result.ShouldBe("DK=v1&k2=DV");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapUseForNullIndexedFuncs` 场景下，结果为 `CurrentlyFallsBackToEmptyDefaults`。
    /// </summary>
    [Fact]
    public void Join_MapUseForNullIndexedFuncs_CurrentlyFallsBackToEmptyDefaults()
    {
        var mapJoiner = Joiner.On("&")
            .WithKeyValueSeparator('=')
            .UseForNull((_, index) => $"K{index}", (_, index) => $"V{index}");
        var input = new[] { null, "v1", "k2", null };
        var result = mapJoiner.Join(input);
        result.ShouldBe("=v1&k2=");
    }
    /// <summary>
    /// 测试用例：验证 `AppendTo` 在 `MapJoinWithDefaultValue` 场景下，结果为 `UsesProvidedDefaultForOddCount`。
    /// </summary>
    [Fact]
    public void AppendTo_MapJoinWithDefaultValue_UsesProvidedDefaultForOddCount()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=');
        var builder = new StringBuilder();
        var input = new[] { "k1", "v1", "k2" };
        mapJoiner.AppendTo(builder, input, "ignored", "dv");
        builder.ToString().ShouldBe("k1=v1&k2=dv");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapSkipNullsWhenKeyIsNull` 场景下，结果为 `KeepsNonNullKeyWithEmptyValue`。
    /// </summary>
    [Fact]
    public void Join_MapSkipNullsWhenKeyIsNull_KeepsNonNullKeyWithEmptyValue()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=').SkipNulls(SkipNullType.WhenKeyIsNull);
        var input = new[] { null, "v0", "k1", null };
        var result = mapJoiner.Join(input);
        result.ShouldBe("k1=");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `MapSkipNullsWhenValueIsNull` 场景下，结果为 `SkipsPairWithNullValue`。
    /// </summary>
    [Fact]
    public void Join_MapSkipNullsWhenValueIsNull_SkipsPairWithNullValue()
    {
        var mapJoiner = Joiner.On("&").WithKeyValueSeparator('=').SkipNulls(SkipNullType.WhenValueIsNull);
        var input = new[] { "k1", null, "k2", "v2" };
        var result = mapJoiner.Join(input);
        result.ShouldBe("k2=v2");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `TupleSkipNullsWhenEither` 场景下，结果为 `SkipsTupleWithNullSegments`。
    /// </summary>
    [Fact]
    public void Join_TupleSkipNullsWhenEither_SkipsTupleWithNullSegments()
    {
        var tupleJoiner = Joiner.On("&").WithKeyValueSeparator('=').FromTuple().SkipNulls(SkipNullType.WhenEither);
        var input = new List<(string, string)>
        {
            ("k1", "v1"),
            (null, "v2"),
            ("k3", null)
        };
        var result = tupleJoiner.Join(input);
        result.ShouldBe("k1=v1");
    }
    /// <summary>
    /// 测试用例：验证 `Join` 在 `TupleGenericJoin` 场景下，结果为 `UsesProjectionAndDefaults`。
    /// </summary>
    [Fact]
    public void Join_TupleGenericJoin_UsesProjectionAndDefaults()
    {
        var tupleJoiner = Joiner.On("&").WithKeyValueSeparator('=').FromTuple();
        var input = new List<(int?, int?)>
        {
            (1, 11),
            (null, 22),
            (3, null)
        };
        var result = tupleJoiner.Join(input, -1, -2, k => k?.ToString(), v => v?.ToString());
        result.ShouldBe("1=11&-1=22&3=-2");
    }
    /// <summary>
    /// 测试用例：验证 `AppendTo` 在 `TupleGenericJoin` 场景下，结果为 `ReturnsSameBuilderAndExpectedResult`。
    /// </summary>
    [Fact]
    public void AppendTo_TupleGenericJoin_ReturnsSameBuilderAndExpectedResult()
    {
        var tupleJoiner = Joiner.On("&").WithKeyValueSeparator('=').FromTuple();
        var builder = new StringBuilder("prefix:");
        var input = new List<(int, int)> { (1, 10), (2, 20) };
        var result = tupleJoiner.AppendTo(builder, input, k => $"k{k}", v => $"v{v}");
        result.ShouldBeSameAs(builder);
        result.ToString().ShouldBe("prefix:k1=v10&k2=v20");
    }
}

