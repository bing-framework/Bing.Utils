using System.Globalization;
using System.Text.Json;
using System.Threading;
using Bing.Conversions;

namespace Bing.Helpers;

/// <summary>
/// 验证类型化转换入口与原有入口的行为一致性。
/// </summary>
[Trait("Bing.Helpers", "Conv.TypedConversion")]
public class ConvTypedConversionTest
{
    /// <summary>
    /// 验证同类型值、可空值和失败输入。
    /// </summary>
    [Fact]
    public void BuiltInConversion_PreservesExistingResults()
    {
        Conv.To<int, int>(7).ShouldBe(Conv.To<int>(7));
        Conv.To<long, int>(7L).ShouldBe(Conv.To<int>(7L));
        Conv.To<int?, int>(7).ShouldBe(Conv.To<int>((int?)7));
        Conv.TryTo<int?, int>(null, out var missing).ShouldBeFalse();
        missing.ShouldBe(0);
        Conv.TryTo<string, int>(" ", out _).ShouldBeFalse();
        Conv.TryTo<object, int>(DBNull.Value, out _).ShouldBeFalse();

        object boxed = 9;
        Conv.To<object, int>(boxed).ShouldBe(Conv.To<int>(boxed));
        Conv.To<string, int>("12").ShouldBe(12);
    }

    /// <summary>
    /// 验证旧入口和类型化入口的常见调用形式均可编译并返回一致结果。
    /// </summary>
    [Fact]
    public void PublicOverloads_PreserveTypeInferenceAndDefaultCalls()
    {
        Conv.To<int>(default(string)).ShouldBe(0);
        Conv.TryTo(default(string), out int inferred).ShouldBeFalse();
        inferred.ShouldBe(0);
        Conv.TryTo("7", out int parsed).ShouldBeTrue();
        parsed.ShouldBe(7);
        Conv.TryTo<string, int>(default, out var typedDefault).ShouldBeFalse();
        typedDefault.ShouldBe(0);

        var converter = new ConvConverterBuilder().Build();
        Conv.To<int>("7", converter).ShouldBe(7);
        Conv.TryTo("7", converter, out int staticResult).ShouldBeTrue();
        staticResult.ShouldBe(7);
        converter.TryTo("7", out int instanceResult).ShouldBeTrue();
        instanceResult.ShouldBe(7);
        converter.To<string, int>("7").ShouldBe(7);
    }

    /// <summary>
    /// 验证类型化同类型标量与旧入口的转换结果一致。
    /// </summary>
    [Fact]
    public void TypedSameType_PreservesScalarValues()
    {
        AssertSameValue(17L);
        AssertSameValue(1.25d);
        AssertSameValue(79228162514264337593543950335m);
        AssertSameValue(false);
        AssertSameValue(System.Guid.NewGuid());
        AssertSameValue(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234));
    }

    /// <summary>
    /// 验证自定义转换精确匹配运行时源类型并优先执行。
    /// </summary>
    [Fact]
    public void CustomConversion_UsesExactRuntimeSourceType()
    {
        var converter = new ConvConverterBuilder()
            .Register<int, string>((int input, out string result) =>
            {
                result = $"custom:{input}";
                return true;
            })
            .Register<Source, int>((Source _, out int result) =>
            {
                result = 41;
                return true;
            })
            .Build();

        Conv.To<int, string>(7, converter).ShouldBe("custom:7");
        converter.To<int, string>(7).ShouldBe("custom:7");
        converter.TryTo<int, string>(7, out var custom).ShouldBeTrue();
        custom.ShouldBe("custom:7");

        Source source = new DerivedSource();
        converter.TryTo<Source, int>(source, out _).ShouldBeFalse();
        converter.To<Source, int>(new Source()).ShouldBe(41);
        Conv.To<int?, string>(7, converter).ShouldBe("custom:7");
    }

    /// <summary>
    /// 验证注册按运行时源类型匹配，且同类型值仍优先执行注册。
    /// </summary>
    [Fact]
    public void CustomConversion_MatchesDerivedRuntimeTypeAndSameTypeRegistration()
    {
        var converter = new ConvConverterBuilder()
            .Register<DerivedSource, int>((DerivedSource _, out int result) =>
            {
                result = 42;
                return true;
            })
            .Register<int, int>((int _, out int result) =>
            {
                result = 99;
                return true;
            })
            .Register<string, int>((string input, out int result) =>
            {
                result = input.Length;
                return true;
            })
            .Build();

        Source source = new DerivedSource();
        Conv.To<Source, int>(source, converter).ShouldBe(42);
        object boxed = source;
        converter.To<object, int>(boxed).ShouldBe(42);
        Conv.To<int, int>(7, converter).ShouldBe(99);
        Conv.To<string, int>("  ", converter).ShouldBe(2);
    }

    /// <summary>
    /// 验证注册失败不回退，成功返回默认值仍算成功。
    /// </summary>
    [Fact]
    public void CustomConversion_PreservesSuccessAndFailureContract()
    {
        var converter = new ConvConverterBuilder()
            .Register<int, string>((int _, out string result) =>
            {
                result = null;
                return true;
            })
            .Register<string, int>((string _, out int result) =>
            {
                result = 99;
                return false;
            })
            .Register<string, long>((string _, out long result) =>
            {
                result = 0;
                throw new FormatException();
            })
            .Build();

        Conv.TryTo<int, string>(1, converter, out var nullResult).ShouldBeTrue();
        nullResult.ShouldBeNull();
        converter.TryTo<string, int>("123", out var failed).ShouldBeFalse();
        failed.ShouldBe(0);
        Conv.To<string, int>("123", converter).ShouldBe(0);
        Conv.TryTo<string, long>("123", converter, out var thrown).ShouldBeFalse();
        thrown.ShouldBe(0);
        Conv.TryTo<string, int>(null, converter, out _).ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => Conv.To<int, string>(1, null));
        Should.Throw<ArgumentNullException>(() => Conv.TryTo<int, string>(1, null, out _));
    }

    /// <summary>
    /// 验证 JSON 元素的类型化入口保留标量与对象行为。
    /// </summary>
    [Fact]
    public void JsonElementConversion_MatchesObjectEntry()
    {
        using var number = JsonDocument.Parse("123");
        using var text = JsonDocument.Parse("\"hello\"");
        using var objectValue = JsonDocument.Parse("{\"Name\":\"Ada\"}");
        var converter = new ConvConverterBuilder().Build();

        Conv.To<JsonElement, int>(number.RootElement).ShouldBe(Conv.To<int>(number.RootElement));
        Conv.To<JsonElement, string>(text.RootElement).ShouldBe(Conv.To<string>(text.RootElement));
        Conv.To<JsonElement, JsonTarget>(objectValue.RootElement).Name.ShouldBe("Ada");
        converter.To<JsonElement, JsonTarget>(objectValue.RootElement).Name.ShouldBe("Ada");
    }

    /// <summary>
    /// 验证类型化 JSON 标量、空值及自定义转换优先级。
    /// </summary>
    [Fact]
    public void JsonElementConversion_PreservesScalarFailureAndCustomPriority()
    {
        var guid = System.Guid.NewGuid();
        using var identifier = JsonDocument.Parse($"\"{guid}\"");
        using var enumValue = JsonDocument.Parse("\"42\"");
        using var number = JsonDocument.Parse("7");
        using var nullValue = JsonDocument.Parse("null");
        using var invalid = JsonDocument.Parse("\"text\"");

        Conv.To<JsonElement, System.Guid>(identifier.RootElement).ShouldBe(Conv.To<System.Guid>(identifier.RootElement));
        Conv.To<JsonElement, SampleEnum>(enumValue.RootElement).ShouldBe(Conv.To<SampleEnum>(enumValue.RootElement));
        Conv.To<JsonElement, int?>(number.RootElement).ShouldBe(7);
        Conv.TryTo<JsonElement, int>(nullValue.RootElement, out _).ShouldBeFalse();
        Conv.TryTo<JsonElement, int>(default, out _).ShouldBeFalse();
        Conv.TryTo<JsonElement, int>(invalid.RootElement, out _).ShouldBeFalse();

        var converter = new ConvConverterBuilder()
            .Register<JsonElement, int>((JsonElement _, out int result) =>
            {
                result = 91;
                return true;
            })
            .Build();
        Conv.To<JsonElement, int>(number.RootElement, converter).ShouldBe(91);
    }

    /// <summary>
    /// 验证 invariant 字符串解析及失败返回。
    /// </summary>
    [Fact]
    public void GenericScalarParsing_UsesInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Conv.To<long>("9223372036854775807").ShouldBe(long.MaxValue);
            Conv.To<double>("1.5").ShouldBe(1.5d);
            Conv.To<decimal?>("1.5").ShouldBe(1.5m);
            Conv.To<bool>(" TRUE ").ShouldBeTrue();
            Conv.TryTo<long>("9223372036854775808", out _).ShouldBeFalse();
            Conv.TryTo<decimal>("invalid", out _).ShouldBeFalse();
            Conv.TryTo<bool>("1", out _).ShouldBeFalse();
            Conv.To<double>("NaN").ShouldBe(double.NaN);
            Conv.To<double>("Infinity").ShouldBe(double.PositiveInfinity);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 验证新增标量解析与可空目标的成功、失败及边界行为。
    /// </summary>
    [Fact]
    public void GenericScalarParsing_CoversNullableAndBoundaryValues()
    {
        Conv.To<long>(long.MinValue.ToString(CultureInfo.InvariantCulture)).ShouldBe(long.MinValue);
        Conv.TryTo<long>("-9223372036854775809", out _).ShouldBeFalse();
        Conv.To<string, long?>("0").ShouldBe(0);
        Conv.To<string, double?>("1,234.5").ShouldBe(1234.5d);
        Conv.To<string, decimal?>("1,234.5").ShouldBe(1234.5m);
        Conv.To<string, bool?>(" false ").ShouldBe(false);
        Conv.TryTo<string, long?>("invalid", out _).ShouldBeFalse();
        Conv.TryTo<string, double?>("invalid", out _).ShouldBeFalse();
        Conv.TryTo<string, decimal?>("1e2", out _).ShouldBeFalse();
        Conv.TryTo<string, bool?>("1", out _).ShouldBeFalse();
        Conv.To<decimal>(decimal.MaxValue.ToString(CultureInfo.InvariantCulture)).ShouldBe(decimal.MaxValue);
        Conv.To<decimal>(decimal.MinValue.ToString(CultureInfo.InvariantCulture)).ShouldBe(decimal.MinValue);
        Conv.TryTo<decimal>("79228162514264337593543950336", out _).ShouldBeFalse();
        Conv.To<double>("-Infinity").ShouldBe(double.NegativeInfinity);
        Conv.To<int>("2.5").ShouldBe(0);
        Conv.ToInt("2.5").ShouldBe(3);
    }

    /// <summary>
    /// 验证跨数值输入继续采用内置的偶数舍入和越界规则。
    /// </summary>
    [Fact]
    public void GenericCrossNumeric_PreservesChangeTypeResults()
    {
        object[] values =
        {
            2L, long.MinValue, long.MaxValue, 2.5d, 3.5d,
            double.NaN, double.PositiveInfinity, 2.5m, 3.5m, decimal.MaxValue
        };
        foreach (var value in values)
        {
            int expected;
            try
            {
                expected = (int)Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture);
                Conv.TryTo<int>(value, out var converted).ShouldBeTrue();
                converted.ShouldBe(expected);
            }
            catch (OverflowException)
            {
                Conv.TryTo<int>(value, out _).ShouldBeFalse();
                Conv.To<int>(value).ShouldBe(0);
            }
        }
    }

    /// <summary>
    /// 验证可空源和可空目标在跨类型转换中的成功与失败结果。
    /// </summary>
    [Fact]
    public void GenericTypedConversion_NullableSourceAndTarget_PreservesResults()
    {
        int? source = 7;

        Conv.To<int?, long>(source).ShouldBe(7L);
        Conv.To<int?, long?>(source).ShouldBe(7L);
        Conv.To<int, long?>(7).ShouldBe(7L);

        Conv.TryTo<int?, decimal?>(source, out var nullableDecimal).ShouldBeTrue();
        nullableDecimal.ShouldBe(7m);

        Conv.TryTo<int?, long?>(null, out var missingNullable).ShouldBeFalse();
        missingNullable.ShouldBeNull();
        Conv.TryTo<int?, long>(null, out var missingValue).ShouldBeFalse();
        missingValue.ShouldBe(0L);

        decimal? overflow = decimal.MaxValue;
        Conv.TryTo<decimal?, int?>(overflow, out var failed).ShouldBeFalse();
        failed.ShouldBeNull();
    }

    /// <summary>
    /// 验证泛型跨数值转换保留负数中点舍入和 ulong 边界规则。
    /// </summary>
    [Fact]
    public void GenericCrossNumeric_CoversNegativeMidpointsAndULongBoundaries()
    {
        object[] midpoints = { -2.5d, -3.5d, -2.5m, -3.5m };
        foreach (var input in midpoints)
        {
            var expected = (int)Convert.ChangeType(input, typeof(int), CultureInfo.InvariantCulture);
            Conv.TryTo<int>(input, out var result).ShouldBeTrue();
            result.ShouldBe(expected);
        }

        ulong[] values =
        {
            0UL,
            1UL,
            (ulong)int.MaxValue,
            (ulong)int.MaxValue + 1UL,
            (ulong)long.MaxValue,
            (ulong)long.MaxValue + 1UL,
            ulong.MaxValue
        };
        foreach (var input in values)
        {
            var fitsInt = input <= int.MaxValue;
            Conv.TryTo<int>(input, out var intResult).ShouldBe(fitsInt);
            if (fitsInt)
                intResult.ShouldBe((int)input);

            var fitsLong = input <= long.MaxValue;
            Conv.TryTo<long>(input, out var longResult).ShouldBe(fitsLong);
            if (fitsLong)
                longResult.ShouldBe((long)input);
        }
    }

    /// <summary>
    /// 验证日期、Guid 和枚举成功与失败路径保留既有结果。
    /// </summary>
    [Fact]
    public void GenericOtherScalars_PreserveExistingResults()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var date = "2024-01-02T03:04:05";
            var expectedDate = (DateTime)Convert.ChangeType(date, typeof(DateTime), CultureInfo.InvariantCulture);
            Conv.To<DateTime>(date).ShouldBe(expectedDate);
            Conv.To<DateTime?>(date).ShouldBe(expectedDate);
            Conv.TryTo<DateTime>("not-a-date", out _).ShouldBeFalse();
            var identifier = System.Guid.NewGuid();
            Conv.To<System.Guid>(identifier.ToString("B")).ShouldBe(identifier);
            Conv.To<System.Guid?>(identifier.ToString("D")).ShouldBe(identifier);
            Conv.TryTo<System.Guid>("invalid", out _).ShouldBeFalse();
            Conv.To<SampleEnum>("42").ShouldBe((SampleEnum)42);
            Conv.TryTo<SampleEnum>("invalid", out _).ShouldBeFalse();
            Conv.To<SampleEnum?>("42").ShouldBe((SampleEnum)42);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 验证列表片段解析保留空项过滤、失败默认值和文本内容。
    /// </summary>
    [Fact]
    public void ListScalarParsing_PreservesExistingRules()
    {
        Conv.ToList<int>("1, ,invalid, +2 ,2147483648,-3,0").ShouldBe(new[] { 1, 0, 2, 0, -3, 0 });
        Conv.ToList<string>(" a , ,b").ShouldBe(new[] { " a ", "b" });
    }

    /// <summary>
    /// 验证可空目标必须按声明类型单独注册。
    /// </summary>
    [Fact]
    public void TypedCustomConversion_RequiresExactNullableTargetRegistration()
    {
        var builder = new ConvConverterBuilder()
            .Register<string, int>((string _, out int result) =>
            {
                result = 41;
                return true;
            });
        var first = builder.Build();
        first.To<string, int?>("7").ShouldBe(7);

        var second = builder.Register<string, int?>((string _, out int? result) =>
        {
            result = 42;
            return true;
        }).Build();
        second.To<string, int?>("7").ShouldBe(42);
        first.To<string, int?>("7").ShouldBe(7);
    }

    /// <summary>
    /// 验证内置整数输入与原文本路径在边界值上等价。
    /// </summary>
    /// <param name="cultureName">用于验证区域性差异的区域名称。</param>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void DedicatedIntegerConversion_MatchesTextPath(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            object[] inputs =
            {
                sbyte.MinValue, byte.MaxValue, short.MinValue, ushort.MaxValue,
                int.MinValue, uint.MaxValue, long.MinValue, long.MaxValue,
                9007199254740991L, 9007199254740993L, ulong.MaxValue
            };
            foreach (var input in inputs)
            {
                var text = input.ToString();
                Conv.ToSByteOrNull(input).ShouldBe(Conv.ToSByteOrNull(text));
                Conv.ToByteOrNull(input).ShouldBe(Conv.ToByteOrNull(text));
                Conv.ToShortOrNull(input).ShouldBe(Conv.ToShortOrNull(text));
                Conv.ToIntOrNull(input).ShouldBe(Conv.ToIntOrNull(text));
                Conv.ToUIntOrNull(input).ShouldBe(Conv.ToUIntOrNull(text));
                Conv.ToLongOrNull(input).ShouldBe(Conv.ToLongOrNull(text));
                Conv.ToULongOrNull(input).ShouldBe(Conv.ToULongOrNull(text));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// 验证类型化入口保留未定义枚举数值与并发注册读取行为。
    /// </summary>
    [Fact]
    public void TypedConversion_PreservesEnumAndConcurrentRegistration()
    {
        Conv.To<string, SampleEnum>("42").ShouldBe((SampleEnum)42);
        var converter = new ConvConverterBuilder()
            .Register<int, long>((int input, out long result) =>
            {
                result = input + 1L;
                return true;
            })
            .Build();
        var errors = 0;
        Parallel.For(0, 256, index =>
        {
            if (!converter.TryTo<int, long>(index, out var value) || value != index + 1L)
                Interlocked.Increment(ref errors);
        });
        errors.ShouldBe(0);
    }

    /// <summary>
    /// 验证浮点和十进制输入保留既有舍入及越界行为。
    /// </summary>
    [Fact]
    public void DedicatedIntegerConversion_PreservesRoundingAndRange()
    {
        Conv.ToIntOrNull(-1.5d).ShouldBe(-2);
        Conv.ToIntOrNull(1.5d).ShouldBe(2);
        Conv.ToIntOrNull(double.NaN).ShouldBeNull();
        Conv.ToIntOrNull(double.PositiveInfinity).ShouldBeNull();
        Conv.ToIntOrNull((double)int.MaxValue + 0.5d).ShouldBeNull();
        Conv.ToLongOrNull(1.5m).ShouldBe(2);
        Conv.ToLongOrNull(-1.5m).ShouldBe(-2);
        Conv.ToLongOrNull((decimal)long.MaxValue + 0.5m).ShouldBeNull();
        Conv.ToULongOrNull(-0.5m).ShouldBeNull();
        Conv.ToLongOrNull((decimal)long.MaxValue + 0.4m).ShouldBe(long.MaxValue);
        Conv.ToULongOrNull((decimal)ulong.MaxValue + 0.5m).ShouldBeNull();
        Conv.ToULongOrNull(-0.4m).ShouldBe(0UL);
    }

    /// <summary>
    /// 验证 int 输入的专用转换与文本路径在边界处一致。
    /// </summary>
    /// <param name="input">覆盖目标边界、负值及 int 极值的输入。</param>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(sbyte.MinValue - 1)]
    [InlineData(sbyte.MinValue)]
    [InlineData(sbyte.MaxValue)]
    [InlineData(sbyte.MaxValue + 1)]
    [InlineData(byte.MaxValue)]
    [InlineData(byte.MaxValue + 1)]
    [InlineData(short.MinValue - 1)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MaxValue + 1)]
    public void DedicatedIntegerConversion_IntInputMatchesTextAtBoundaries(int input)
    {
        var text = input.ToString(CultureInfo.CurrentCulture);
        Conv.ToSByteOrNull(input).ShouldBe(Conv.ToSByteOrNull(text));
        Conv.ToByteOrNull(input).ShouldBe(Conv.ToByteOrNull(text));
        Conv.ToShortOrNull(input).ShouldBe(Conv.ToShortOrNull(text));
        Conv.ToUIntOrNull(input).ShouldBe(Conv.ToUIntOrNull(text));
        Conv.ToLongOrNull(input).ShouldBe(Conv.ToLongOrNull(text));
        Conv.ToULongOrNull(input).ShouldBe(Conv.ToULongOrNull(text));
    }

    /// <summary>
    /// 比较类型化和旧入口的同类型转换结果。
    /// </summary>
    /// <typeparam name="T">源和目标类型。</typeparam>
    /// <param name="value">待转换的输入值。</param>
    private static void AssertSameValue<T>(T value) where T : struct
    {
        Conv.TryTo<T, T>(value, out var typed).ShouldBeTrue();
        typed.ShouldBe(Conv.To<T>(value));
    }

    /// <summary>
    /// 表示自定义转换的基类源类型。
    /// </summary>
    private class Source { }

    /// <summary>
    /// 表示未注册的派生源类型。
    /// </summary>
    private sealed class DerivedSource : Source { }

    /// <summary>
    /// 表示 JSON 对象目标。
    /// </summary>
    private sealed class JsonTarget
    {
        /// <summary>
        /// 获取或设置对象名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 表示枚举转换测试目标。
    /// </summary>
    private enum SampleEnum
    {
        /// <summary>
        /// 零值。
        /// </summary>
        None = 0
    }
}
