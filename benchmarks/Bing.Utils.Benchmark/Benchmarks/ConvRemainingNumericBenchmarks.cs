using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较主跨数值矩阵未覆盖的类型化数值对与对象入口。
/// </summary>
/// <typeparam name="TSource">保持类型化传入的源数值类型。</typeparam>
/// <typeparam name="TTarget">转换目标类型。</typeparam>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(uint), typeof(long))]
[GenericTypeArguments(typeof(ulong), typeof(long))]
[GenericTypeArguments(typeof(ulong), typeof(long?))]
[GenericTypeArguments(typeof(double), typeof(decimal))]
[GenericTypeArguments(typeof(decimal), typeof(double))]
[GenericTypeArguments(typeof(short), typeof(int))]
public class ConvRemainingNumericBenchmarks<TSource, TTarget>
{
    /// <summary>
    /// 测量用源值，由 <see cref="Setup"/> 在基准运行前准备。
    /// </summary>
    private TSource _input = default!;

    /// <summary>
    /// 当前闭合类型对适用的输入场景。
    /// </summary>
    [ParamsSource(nameof(InputCases))]
    public NumericInputCase InputCase { get; set; }

    /// <summary>
    /// 按源目标类型提供少量代表性边界或失败场景。
    /// </summary>
    public IEnumerable<NumericInputCase> InputCases
    {
        get
        {
            var sourceType = GetUnderlyingType(typeof(TSource));
            var targetType = GetUnderlyingType(typeof(TTarget));
            if (sourceType == typeof(uint))
            {
                yield return NumericInputCase.UpperBoundary;
                yield break;
            }
            if (sourceType == typeof(ulong) && targetType == typeof(long))
            {
                yield return NumericInputCase.UpperBoundary;
                yield return NumericInputCase.Overflow;
                yield break;
            }
            if (sourceType == typeof(double) && targetType == typeof(decimal))
            {
                yield return NumericInputCase.Representative;
                yield return NumericInputCase.Overflow;
                yield return NumericInputCase.NaN;
                yield return NumericInputCase.PositiveInfinity;
                yield break;
            }
            if (sourceType == typeof(decimal) && targetType == typeof(double))
            {
                yield return NumericInputCase.UpperBoundary;
                yield break;
            }
            if (sourceType == typeof(short) && targetType == typeof(int))
            {
                yield return NumericInputCase.LowerBoundary;
                yield return NumericInputCase.UpperBoundary;
            }
        }
    }

    /// <summary>
    /// 准备源值并确认 typed 与 object 入口的返回状态和值一致。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = (TSource)CreateInput();

        var typedSuccess = Conv.TryTo<TSource, TTarget>(_input, out var typedResult);
        var objectSuccess = Conv.TryTo<TTarget>((object)_input!, out var objectResult);
        if (typedSuccess != objectSuccess ||
            !EqualityComparer<TTarget>.Default.Equals(typedResult, objectResult))
            throw new InvalidOperationException($"typed 与 object 入口结果不一致：{typeof(TSource)} -> {typeof(TTarget)}, {InputCase}");
    }

    /// <summary>
    /// 测量保留源类型的 typed 入口，不在调用点将输入转为 object。
    /// </summary>
    /// <returns>转换后的目标值；失败时返回默认值。</returns>
    [Benchmark]
    public TTarget TypedEntry() => Conv.To<TSource, TTarget>(_input);

    /// <summary>
    /// 测量 object 入口；源值在调用点装箱。
    /// </summary>
    /// <returns>转换后的目标值；失败时返回默认值。</returns>
    [Benchmark(Baseline = true)]
    public TTarget ObjectEntryWithCallerBoxing() => Conv.To<TTarget>((object)_input!);

    /// <summary>
    /// 根据当前输入场景创建对应的数值源值。
    /// </summary>
    /// <returns>用于本次基准的源值。</returns>
    private object CreateInput()
    {
        var sourceType = GetUnderlyingType(typeof(TSource));
        if (sourceType == typeof(uint))
            return uint.MaxValue;
        if (sourceType == typeof(ulong))
        {
            return InputCase == NumericInputCase.Overflow ? ulong.MaxValue : (ulong)long.MaxValue;
        }
        if (sourceType == typeof(double))
        {
            return InputCase switch
            {
                NumericInputCase.Overflow => double.MaxValue,
                NumericInputCase.NaN => double.NaN,
                NumericInputCase.PositiveInfinity => double.PositiveInfinity,
                _ => 12345.625d
            };
        }
        if (sourceType == typeof(decimal))
            return decimal.MaxValue;
        if (sourceType == typeof(short))
        {
            return InputCase == NumericInputCase.LowerBoundary ? short.MinValue : short.MaxValue;
        }
        throw new NotSupportedException($"未配置源数值类型：{sourceType}");
    }

    /// <summary>
    /// 获取 nullable 包装对应的基础类型。
    /// </summary>
    /// <param name="type">声明的源类型或目标类型。</param>
    /// <returns>去除 nullable 包装后的类型。</returns>
    private static Type GetUnderlyingType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// 描述本基准支持的数值输入场景。
    /// </summary>
    public enum NumericInputCase
    {
        /// <summary>代表性有限数值。</summary>
        Representative,
        /// <summary>源值的下边界。</summary>
        LowerBoundary,
        /// <summary>源值的上边界。</summary>
        UpperBoundary,
        /// <summary>转换结果超出目标类型范围。</summary>
        Overflow,
        /// <summary>非数字浮点值。</summary>
        NaN,
        /// <summary>正无穷浮点值。</summary>
        PositiveInfinity
    }
}
