using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较专用整数转换与旧文本方法体的代表路径。
/// </summary>
/// <remarks>
/// 输入预装箱；两个测量方法使用相同当前区域性和远离零点舍入。
/// 返回值统一提升为可空 decimal，避免基准结果装箱且保留所有目标整数精度。
/// 旧方法体只复现本轮直接数值优化之前的分支，不代表完整历史版本。
/// </remarks>
[MemoryDiagnoser]
public class ConvDedicatedIntegerBenchmarks
{
    /// <summary>
    /// 当前场景共用的预装箱数值输入。
    /// </summary>
    private object _input = 123;

    /// <summary>
    /// 获取或设置专用整数目标类型。
    /// </summary>
    [Params("sbyte", "byte", "short", "uint", "long", "ulong")]
    public string Target { get; set; } = "sbyte";

    /// <summary>
    /// 获取或设置是否使用需要舍入的小数输入。
    /// </summary>
    [ParamsSource(nameof(Cases))]
    public bool Fractional { get; set; }

    /// <summary>
    /// 获取或设置是否使用目标范围外的输入。
    /// </summary>
    [ParamsSource(nameof(Cases))]
    public bool Invalid { get; set; }

    /// <summary>
    /// 获取本次测量的输入场景选项。
    /// </summary>
    /// <remarks>
    /// CONV_DEDICATED_CONFIRM 为 1 时只测成功整数输入；默认同时覆盖小数和失败输入。
    /// </remarks>
    public IEnumerable<bool> Cases => Environment.GetEnvironmentVariable("CONV_DEDICATED_CONFIRM") == "1"
        ? new[] { false }
        : new[] { false, true };

    /// <summary>
    /// 准备输入。
    /// </summary>
    /// <remarks>
    /// 初始化后验证当前路径与旧方法体的结果一致。
    /// </remarks>
    [GlobalSetup]
    public void Setup()
    {
        if (!Fractional)
        {
            _input = !Invalid ? (object)123 : Target switch
            {
                "long" => ulong.MaxValue,
                "ulong" => (object)(-1L),
                _ => long.MaxValue
            };
        }
        else
        {
            _input = Target switch
            {
                "sbyte" => (object)(Invalid ? sbyte.MaxValue + 0.5d : 123.5d),
                "byte" => (object)(Invalid ? byte.MaxValue + 0.5d : 123.5d),
                "short" => (object)(Invalid ? short.MaxValue + 0.5d : 123.5d),
                "uint" => (object)(Invalid ? uint.MaxValue + 0.5d : 123.5d),
                "long" => (object)(Invalid ? (decimal)long.MaxValue + 0.5m : 123.5m),
                "ulong" => (object)(Invalid ? (decimal)ulong.MaxValue + 0.5m : 123.5m),
                _ => throw new NotSupportedException(Target)
            };
        }

        var current = Current();
        if (current != LegacyBody() || Invalid == current.HasValue)
            throw new InvalidOperationException("专用转换基准输入未满足预期契约。");
    }

    /// <summary>
    /// 测量当前专用整数转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    [Benchmark]
    public decimal? Current() => Target switch
    {
        "sbyte" => Conv.ToSByteOrNull(_input),
        "byte" => Conv.ToByteOrNull(_input),
        "short" => Conv.ToShortOrNull(_input),
        "uint" => Conv.ToUIntOrNull(_input),
        "long" => Conv.ToLongOrNull(_input),
        "ulong" => Conv.ToULongOrNull(_input),
        _ => throw new NotSupportedException(Target)
    };

    /// <summary>
    /// 测量旧文本转换方法体。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    [Benchmark(Baseline = true)]
    public decimal? LegacyBody() => Target switch
    {
        "sbyte" => LegacySByte(),
        "byte" => LegacyByte(),
        "short" => LegacyShort(),
        "uint" => LegacyUInt(),
        "long" => LegacyLong(),
        "ulong" => LegacyULong(),
        _ => throw new NotSupportedException(Target)
    };

    /// <summary>
    /// 复现旧 sbyte 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private sbyte? LegacySByte()
    {
        if (_input is sbyte direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (sbyte.TryParse(text, out var value)) return value;
        var rounded = RoundedDouble(text);
        return rounded is >= sbyte.MinValue and <= sbyte.MaxValue ? (sbyte)rounded.Value : null;
    }

    /// <summary>
    /// 复现旧 byte 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private byte? LegacyByte()
    {
        if (_input is byte direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (byte.TryParse(text, out var value)) return value;
        var rounded = RoundedDouble(text);
        return rounded is >= byte.MinValue and <= byte.MaxValue ? (byte)rounded.Value : null;
    }

    /// <summary>
    /// 复现旧 short 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private short? LegacyShort()
    {
        if (_input is short direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (short.TryParse(text, out var value)) return value;
        var rounded = RoundedDouble(text);
        return rounded is >= short.MinValue and <= short.MaxValue ? (short)rounded.Value : null;
    }

    /// <summary>
    /// 复现旧 uint 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private uint? LegacyUInt()
    {
        if (_input is uint direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (uint.TryParse(text, out var value)) return value;
        var rounded = RoundedDouble(text);
        return rounded is >= uint.MinValue and <= uint.MaxValue ? (uint)rounded.Value : null;
    }

    /// <summary>
    /// 复现旧 long 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private long? LegacyLong()
    {
        if (_input is long direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (long.TryParse(text, out var value)) return value;
        var rounded = RoundedDecimal(text);
        return rounded is >= long.MinValue and <= long.MaxValue ? (long)rounded.Value : null;
    }

    /// <summary>
    /// 复现旧 ulong 文本转换。
    /// </summary>
    /// <returns>转换值；越界时返回 null。</returns>
    private ulong? LegacyULong()
    {
        if (_input is ulong direct) return direct;
        var text = _input?.ToString()?.Trim() ?? string.Empty;
        if (ulong.TryParse(text, out var value)) return value;
        var rounded = RoundedDecimal(text);
        return rounded is >= ulong.MinValue and <= ulong.MaxValue ? (ulong)rounded.Value : null;
    }

    /// <summary>
    /// 按旧规则舍入双精度输入。
    /// </summary>
    /// <param name="text">已生成的当前区域性文本。</param>
    /// <returns>舍入值；解析失败时返回 null。</returns>
    private double? RoundedDouble(string text)
    {
        if (_input is double value)
            return Math.Round(value, 0, MidpointRounding.AwayFromZero);
        return double.TryParse(text, out value) ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : null;
    }

    /// <summary>
    /// 按旧规则舍入十进制输入。
    /// </summary>
    /// <param name="text">已生成的当前区域性文本。</param>
    /// <returns>舍入值；解析失败时返回 null。</returns>
    private decimal? RoundedDecimal(string text)
    {
        if (_input is decimal value)
            return Math.Round(value, 0, MidpointRounding.AwayFromZero);
        return decimal.TryParse(text, out value) ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : null;
    }
}
