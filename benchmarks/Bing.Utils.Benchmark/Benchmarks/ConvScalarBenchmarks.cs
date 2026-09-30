using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较泛型标量解析与旧 ChangeType 方法体。
/// </summary>
/// <typeparam name="T">标量目标类型。</typeparam>
/// <remarks>
/// 只比较固定标量分支，不代表历史完整版本。
/// </remarks>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(double))]
[GenericTypeArguments(typeof(decimal))]
[GenericTypeArguments(typeof(bool))]
[GenericTypeArguments(typeof(long?))]
[GenericTypeArguments(typeof(double?))]
[GenericTypeArguments(typeof(decimal?))]
[GenericTypeArguments(typeof(bool?))]
public class ConvScalarBenchmarks<T>
{
    /// <summary>
    /// 待解析的固定文本。
    /// </summary>
    private object _input = string.Empty;

    /// <summary>
    /// 获取或设置是否使用失败输入。
    /// </summary>
    [ParamsSource(nameof(InvalidValues))]
    public bool Invalid { get; set; }

    /// <summary>
    /// 获取本次测量的输入有效性集合。
    /// </summary>
    /// <remarks>
    /// CONV_SCALAR_SUCCESS_ONLY=1 时只确认成功路径。
    /// </remarks>
    public IEnumerable<bool> InvalidValues => Environment.GetEnvironmentVariable("CONV_SCALAR_SUCCESS_ONLY") == "1"
        ? new[] { false } : new[] { false, true };

    /// <summary>
    /// 准备同语义输入。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        _input = Invalid ? "invalid" : type == typeof(bool) ? "True" : type == typeof(long) ? "12345" : "12345.625";
    }

    /// <summary>
    /// 测量当前标量转换。
    /// </summary>
    /// <returns>转换结果；失败时返回默认值。</returns>
    [Benchmark]
    public T Current() => Conv.To<T>(_input);

    /// <summary>
    /// 测量旧标量转换方法体。
    /// </summary>
    /// <returns>转换结果；失败时返回默认值。</returns>
    [Benchmark(Baseline = true)]
    public T LegacyBody()
    {
        if (_input == null || _input is DBNull || _input is string text && string.IsNullOrWhiteSpace(text))
            return default!;
        if (_input is T same)
            return same;
        var type = Common.GetType<T>();
        try { return (T)Convert.ChangeType(_input, type, CultureInfo.InvariantCulture); }
        catch { return default!; }
    }
}
