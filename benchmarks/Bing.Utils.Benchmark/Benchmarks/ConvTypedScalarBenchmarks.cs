using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较同类型标量转换入口的装箱成本。
/// </summary>
/// <typeparam name="T">参与同类型转换的非可空值类型。</typeparam>
/// <remarks>
/// 每个封闭泛型使用相同输入和返回类型；数据准备及预装箱在测量区外执行。
/// 直接返回是成本控制，不代表历史版本的转换实现。
/// </remarks>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(double))]
[GenericTypeArguments(typeof(decimal))]
[GenericTypeArguments(typeof(bool))]
[GenericTypeArguments(typeof(Guid))]
[GenericTypeArguments(typeof(DateTime))]
public class ConvTypedScalarBenchmarks<T> where T : struct
{
    /// <summary>
    /// 供直接返回和调用端传参使用的固定值。
    /// </summary>
    private T _input;

    /// <summary>
    /// 在数据准备阶段装箱一次的相同输入。
    /// </summary>
    private object _preboxedInput = null!;

    /// <summary>
    /// 准备各转换入口共用的标量输入。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        object value;
        if (typeof(T) == typeof(int))
            value = 12345;
        else if (typeof(T) == typeof(long))
            value = 12345L;
        else if (typeof(T) == typeof(double))
            value = 12345.625d;
        else if (typeof(T) == typeof(decimal))
            value = 12345.625m;
        else if (typeof(T) == typeof(bool))
            value = true;
        else if (typeof(T) == typeof(Guid))
            value = Guid.Parse("f94d5b93-4c4b-4fca-bf97-a306db4257c5");
        else if (typeof(T) == typeof(DateTime))
            value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234);
        else
            throw new NotSupportedException($"未配置标量类型：{typeof(T)}");

        _input = (T)value;
        _preboxedInput = value;
    }

    /// <summary>
    /// 测量直接返回标量的控制路径。
    /// </summary>
    /// <returns>固定输入值。</returns>
    [Benchmark(Baseline = true)]
    public T DirectControl() => _input;

    /// <summary>
    /// 测量预装箱输入的旧转换入口。
    /// </summary>
    /// <returns>转换后的同类型值。</returns>
    [Benchmark]
    public T PreboxedObjectEntry() => Conv.To<T>(_preboxedInput);

    /// <summary>
    /// 测量调用端装箱的旧转换入口。
    /// </summary>
    /// <returns>转换后的同类型值。</returns>
    [Benchmark]
    public T CallerBoxingObjectEntry() => Conv.To<T>(_input);

    /// <summary>
    /// 测量保留源类型的转换入口。
    /// </summary>
    /// <returns>转换后的同类型值。</returns>
    [Benchmark]
    public T TypedEntry() => Conv.To<T, T>(_input);
}
