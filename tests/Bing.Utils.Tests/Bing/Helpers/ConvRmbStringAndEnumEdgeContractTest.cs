using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 测试类：`Conv` 在人民币、字符串格式化与枚举转换上的边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Conv.RmbStringEnum.EdgeContract")]
public class ConvRmbStringAndEnumEdgeContractTest
{
    /// <summary>
    /// 测试用例：`ToRMB` 对无法解析的字符串应原样返回
    /// </summary>
    [Theory]
    [MemberData(nameof(GetNonNumericRmbInputs))]
    public void ToRMB_NonNumericString_ReturnsOriginalInput(string input)
    {
        var result = Conv.ToRMB(input);

        result.ShouldBe(input);
    }

    /// <summary>
    /// 测试用例：`ToRMB` 对负小数金额当前实现会丢失负号（先固化现状，待修复）
    /// </summary>
    [Fact]
    public void ToRMB_NegativeDecimal_CurrentlyDropsNegativePrefix()
    {
        var result = Conv.ToRMB(-1.05m);

        result.ShouldBe("壹元零伍分");
        result.ShouldNotContain("负");
    }

    /// <summary>
    /// 测试用例：`ToRMB` 对可解析的空白包裹数字字符串，应与纯数字字符串结果一致
    /// </summary>
    [Theory]
    [MemberData(nameof(GetWhitespaceWrappedNumericCases))]
    public void ToRMB_WhitespaceWrappedNumericString_MatchesPlainNumericResult(string plainInput, string wrappedInput)
    {
        var plain = Conv.ToRMB(plainInput);
        var wrapped = Conv.ToRMB(wrappedInput);

        wrapped.ShouldBe(plain);
    }

    /// <summary>
    /// 测试用例：`ToStringOrDefault` 在输入为 `null` 且默认值为 `null` 时，应返回 `null`
    /// </summary>
    [Fact]
    public void ToStringOrDefault_NullInputAndNullDefault_ReturnsNull()
    {
        int? input = null;

        var result = Conv.ToStringOrDefault(input, defaultValue: null);

        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：`ToStringOrDefault(带 format)` 在 `format=null` 时，应走类型默认格式化输出
    /// </summary>
    [Theory]
    [InlineData(12.3)]
    [InlineData(1000.5)]
    public void ToStringOrDefault_NullFormat_UsesDefaultFormatting(double numeric)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            decimal? value = (decimal)numeric;

            var result = Conv.ToStringOrDefault(value, format: null, defaultValue: "N/A");

            result.ShouldBe(value.Value.ToString(CultureInfo.CurrentCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 测试用例：`ToEnumOrNull` 对两侧空白枚举名应可解析（依赖内部 Trim）
    /// </summary>
    [Theory]
    [InlineData("  Closed  ", SampleState.Closed)]
    [InlineData("\tOpened\r\n", SampleState.Opened)]
    public void ToEnumOrNull_NameWithWhitespaces_ReturnsParsedEnum(string input, SampleState expected)
    {
        var result = Conv.ToEnumOrNull<SampleState>(input);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：`ToEnumOrNull` 对 Flags 组合字符串应可解析为按位组合值
    /// </summary>
    [Theory]
    [InlineData("Read, Write", SampleFlags.Read | SampleFlags.Write)]
    [InlineData(" Read , Execute ", SampleFlags.Read | SampleFlags.Execute)]
    public void ToEnumOrNull_FlagsCombinedNames_ReturnsCombinedEnumValue(string input, SampleFlags expected)
    {
        var result = Conv.ToEnumOrNull<SampleFlags>(input);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：`ToEnum` 在输入为 `null` 且给定默认值时，应返回显式默认值
    /// </summary>
    [Theory]
    [InlineData(SampleState.None)]
    [InlineData(SampleState.Opened)]
    [InlineData(SampleState.Closed)]
    public void ToEnum_NullInput_WithExplicitDefault_ReturnsProvidedDefault(SampleState defaultValue)
    {
        var result = Conv.ToEnum<SampleState>(null, defaultValue);

        result.ShouldBe(defaultValue);
    }

    /// <summary>
    /// 测试数据：`ToRMB` 无法解析的非数字字符串
    /// </summary>
    public static IEnumerable<object[]> GetNonNumericRmbInputs()
    {
        yield return new object[] { "not-a-number" };
        yield return new object[] { "abc123x" };
        yield return new object[] { "￥100" };
    }

    /// <summary>
    /// 测试数据：空白包裹数字字符串与纯数字字符串对照组
    /// </summary>
    public static IEnumerable<object[]> GetWhitespaceWrappedNumericCases()
    {
        yield return new object[] { "10.23", "  10.23  " };
        yield return new object[] { "0.50", "\t0.50\r\n" };
        yield return new object[] { "-100", "  -100  " };
    }

    public enum SampleState
    {
        None = 0,
        Opened = 1,
        Closed = 2
    }

    [Flags]
    public enum SampleFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }
}
