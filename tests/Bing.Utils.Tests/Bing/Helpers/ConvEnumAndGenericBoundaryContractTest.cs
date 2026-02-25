namespace Bing.Helpers;

/// <summary>
/// 测试类：`Conv` 枚举与泛型转换边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Conv.EnumAndGenericBoundary")]
public class ConvEnumAndGenericBoundaryContractTest
{
    /// <summary>
    /// 测试用例：`ToEnumOrNull` 对合法输入应返回已解析的枚举值（大小写/数字字符串/数值）
    /// </summary>
    [Theory]
    [MemberData(nameof(GetValidEnumInputs))]
    public void ToEnumOrNull_ValidInput_ReturnsParsedEnum(object input, SampleState expected)
    {
        var result = Conv.ToEnumOrNull<SampleState>(input);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：`ToEnumOrNull` 对空值或非法名称输入应返回 `null`
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidEnumNameInputs))]
    public void ToEnumOrNull_InvalidNameOrEmpty_ReturnsNull(object input)
    {
        var result = Conv.ToEnumOrNull<SampleState>(input);

        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：`ToEnumOrNull` 对未定义数值当前仍返回原始枚举值（未回退）
    /// </summary>
    [Theory]
    [InlineData(99)]
    [InlineData("99")]
    public void ToEnumOrNull_UndefinedNumericInput_CurrentlyReturnsRawEnumValue(object input)
    {
        var result = Conv.ToEnumOrNull<SampleState>(input);

        result.ShouldNotBeNull();
        result.Value.ShouldBe((SampleState)99);
        System.Enum.IsDefined(typeof(SampleState), result.Value).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：`ToEnum` 对非法输入（显式默认值）应返回调用方提供的默认值
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidEnumDefaultFallbackInputs))]
    public void ToEnum_InvalidInputWithExplicitDefault_ReturnsProvidedDefault(object input)
    {
        var result = Conv.ToEnum(input, SampleState.Closed);

        result.ShouldBe(SampleState.Closed);
    }

    /// <summary>
    /// 测试用例：`ToEnum` 对非法输入（无显式默认值）应返回枚举零值
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidEnumDefaultZeroInputs))]
    public void ToEnum_InvalidInputWithoutExplicitDefault_ReturnsZeroEnum(object input)
    {
        var result = Conv.ToEnum<SampleState>(input);

        result.ShouldBe(SampleState.None);
    }

    /// <summary>
    /// 测试用例：`ToEnum` 对未定义数值（显式默认值）当前不会回退到默认值
    /// </summary>
    [Theory]
    [InlineData(99)]
    [InlineData("99")]
    public void ToEnum_UndefinedNumericInputWithExplicitDefault_DoesNotFallbackToDefault(object input)
    {
        var result = Conv.ToEnum(input, SampleState.Closed);

        result.ShouldBe((SampleState)99);
    }

    /// <summary>
    /// 测试用例：`To<string>` 对 `null/empty/whitespace` 当前返回 `null`
    /// </summary>
    [Theory]
    [MemberData(nameof(GetStringToStringNullCases))]
    public void To_StringEmptyLikeInput_ReturnsNull(string input)
    {
        var result = Conv.To<string>(input);

        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：`To<Enum>` 对非法名称输入应返回枚举默认值
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidEnumNameStringInputs))]
    public void To_EnumInvalidName_ReturnsDefaultEnumValue(string input)
    {
        var result = Conv.To<SampleState>(input);

        result.ShouldBe(default);
    }

    /// <summary>
    /// 测试用例：`To<Enum>` 对未定义数字字符串当前会返回原始枚举值（不回退默认值）
    /// </summary>
    [Theory]
    [InlineData("99")]
    [InlineData("404")]
    public void To_EnumUndefinedNumericString_CurrentlyReturnsRawEnumValue(string input)
    {
        var result = Conv.To<SampleState>(input);

        result.ShouldBe((SampleState)int.Parse(input));
        System.Enum.IsDefined(typeof(SampleState), result).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：`To<int>` 对越界数值输入应返回默认值 `0`
    /// </summary>
    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void To_Int32OverflowInput_ReturnsDefaultZero(long input)
    {
        var result = Conv.To<int>(input);

        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试用例：`To<T>` 当输入对象已是目标类型时，应返回同一引用
    /// </summary>
    [Fact]
    public void To_CustomTypeInputSameType_ReturnsSameReference()
    {
        var source = new Payload { Name = "alpha" };

        var result = Conv.To<Payload>(source);

        result.ShouldBeSameAs(source);
    }

    /// <summary>
    /// 测试数据：`ToEnumOrNull` 合法输入
    /// </summary>
    public static IEnumerable<object[]> GetValidEnumInputs()
    {
        yield return new object[] { "Opened", SampleState.Opened };
        yield return new object[] { "opened", SampleState.Opened };
        yield return new object[] { "CLOSED", SampleState.Closed };
        yield return new object[] { 1, SampleState.Opened };
        yield return new object[] { "2", SampleState.Closed };
    }

    /// <summary>
    /// 测试数据：`ToEnumOrNull` 非法名称/空输入
    /// </summary>
    public static IEnumerable<object[]> GetInvalidEnumNameInputs()
    {
        yield return new object[] { null };
        yield return new object[] { "" };
        yield return new object[] { "   " };
        yield return new object[] { "not-exists" };
    }

    /// <summary>
    /// 测试数据：`ToEnum` 显式默认值回退场景（非法名称/空输入）
    /// </summary>
    public static IEnumerable<object[]> GetInvalidEnumDefaultFallbackInputs()
    {
        yield return new object[] { null };
        yield return new object[] { "" };
        yield return new object[] { "invalid" };
        yield return new object[] { "  invalid  " };
    }

    /// <summary>
    /// 测试数据：`ToEnum` 无显式默认值返回零值场景
    /// </summary>
    public static IEnumerable<object[]> GetInvalidEnumDefaultZeroInputs()
    {
        yield return new object[] { null };
        yield return new object[] { "" };
        yield return new object[] { "   " };
        yield return new object[] { "invalid" };
    }

    /// <summary>
    /// 测试数据：`To<string>` 输入为空样式字符串时返回 `null`
    /// </summary>
    public static IEnumerable<object[]> GetStringToStringNullCases()
    {
        yield return new object[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { "   " };
        yield return new object[] { "\t" };
        yield return new object[] { "\r\n" };
    }

    /// <summary>
    /// 测试数据：无效枚举名称字符串
    /// </summary>
    public static IEnumerable<object[]> GetInvalidEnumNameStringInputs()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "   " };
        yield return new object[] { "invalid" };
    }

    public enum SampleState
    {
        None = 0,
        Opened = 1,
        Closed = 2
    }

    private sealed class Payload
    {
        public string Name { get; set; }
    }
}
