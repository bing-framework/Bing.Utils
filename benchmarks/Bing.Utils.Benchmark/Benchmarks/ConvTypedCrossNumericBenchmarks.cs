using System.Globalization;
using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较类型化跨数值转换与对象入口的分配成本。
/// </summary>
/// <typeparam name="TSource">源数值类型。</typeparam>
/// <typeparam name="TTarget">目标数值类型。</typeparam>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(int), typeof(long))]
[GenericTypeArguments(typeof(long), typeof(int))]
[GenericTypeArguments(typeof(int), typeof(long?))]
[GenericTypeArguments(typeof(long), typeof(int?))]
[GenericTypeArguments(typeof(float), typeof(int))]
[GenericTypeArguments(typeof(float), typeof(int?))]
[GenericTypeArguments(typeof(float), typeof(long))]
[GenericTypeArguments(typeof(float), typeof(long?))]
[GenericTypeArguments(typeof(double), typeof(int))]
[GenericTypeArguments(typeof(double), typeof(int?))]
[GenericTypeArguments(typeof(double), typeof(long))]
[GenericTypeArguments(typeof(double), typeof(long?))]
[GenericTypeArguments(typeof(decimal), typeof(int))]
[GenericTypeArguments(typeof(decimal), typeof(int?))]
[GenericTypeArguments(typeof(decimal), typeof(long))]
[GenericTypeArguments(typeof(decimal), typeof(long?))]
[GenericTypeArguments(typeof(int), typeof(float))]
[GenericTypeArguments(typeof(int), typeof(float?))]
[GenericTypeArguments(typeof(int), typeof(double))]
[GenericTypeArguments(typeof(int), typeof(double?))]
[GenericTypeArguments(typeof(int), typeof(decimal))]
[GenericTypeArguments(typeof(int), typeof(decimal?))]
[GenericTypeArguments(typeof(long), typeof(float))]
[GenericTypeArguments(typeof(long), typeof(float?))]
[GenericTypeArguments(typeof(long), typeof(double))]
[GenericTypeArguments(typeof(long), typeof(double?))]
[GenericTypeArguments(typeof(long), typeof(decimal))]
[GenericTypeArguments(typeof(long), typeof(decimal?))]
public class ConvTypedCrossNumericBenchmarks<TSource, TTarget>
{
    /// <summary>
    /// 测量用的源值。
    /// </summary>
    private TSource _input = default!;

    /// <summary>
    /// 本次测量采用的输入场景。
    /// </summary>
    [ParamsSource(nameof(InputCases))]
    public NumericInputCase InputCase { get; set; }

    /// <summary>
    /// 根据源和目标类型提供有效、越界及非有限输入场景。
    /// </summary>
    public IEnumerable<NumericInputCase> InputCases
    {
        get
        {
            yield return NumericInputCase.Success;

            var sourceType = GetUnderlyingType(typeof(TSource));
            var targetType = GetUnderlyingType(typeof(TTarget));
            if (IsIntegralType(sourceType) && IsFloatingType(targetType))
            {
                yield return NumericInputCase.SourceMinimum;
                yield return NumericInputCase.SourceMaximum;
                yield break;
            }

            if (sourceType == typeof(int) && targetType == typeof(long))
            {
                yield return NumericInputCase.SourceMinimum;
                yield return NumericInputCase.SourceMaximum;
                yield break;
            }

            if (sourceType == typeof(long) && targetType == typeof(int))
            {
                yield return NumericInputCase.OverflowLow;
                yield return NumericInputCase.OverflowHigh;
                yield break;
            }

            if (IsFloatingOrDecimalType(sourceType) && IsIntegralType(targetType))
            {
                yield return NumericInputCase.OverflowLow;
                yield return NumericInputCase.OverflowHigh;
                if (sourceType == typeof(float) || sourceType == typeof(double))
                {
                    yield return NumericInputCase.NaN;
                    yield return NumericInputCase.PositiveInfinity;
                    yield return NumericInputCase.NegativeInfinity;
                }
            }
        }
    }

    /// <summary>
    /// 准备与场景匹配的源值；有效值和失败值均在测量区外创建。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = (TSource)CreateInput();

        var currentSuccess = Conv.TryTo<TSource, TTarget>(_input, out var currentResult);
        var legacySuccess = LegacyTypedTryTo(_input, out var legacyResult);
        if (currentSuccess != legacySuccess ||
            !EqualityComparer<TTarget>.Default.Equals(currentResult, legacyResult))
            throw new InvalidOperationException($"当前与旧方法体结果不一致：{typeof(TSource)} -> {typeof(TTarget)}, {InputCase}");
    }

    /// <summary>
    /// 测量类型化跨数值入口。
    /// </summary>
    /// <returns>转换后的目标值。</returns>
    [Benchmark]
    public TTarget TypedEntry() => Conv.To<TSource, TTarget>(_input);

    /// <summary>
    /// 测量旧类型化方法体的对象回退：在方法内装箱源值并使用 ChangeType 转换。
    /// </summary>
    /// <returns>转换后的目标值；失败时返回默认值。</returns>
    [Benchmark(Baseline = true)]
    public TTarget LegacyTypedBody() => LegacyTypedTryTo(_input, out var result) ? result : default!;

    /// <summary>
    /// 测量对象入口包含调用端装箱的同语义路径。
    /// </summary>
    /// <returns>转换后的目标值。</returns>
    [Benchmark]
    public TTarget ObjectEntry() => Conv.To<TTarget>((object)_input!);

    /// <summary>
    /// 复现旧类型化方法体的对象回退，并在异常时返回失败。
    /// </summary>
    /// <param name="input">待转换的源值。</param>
    /// <param name="result">转换成功时的目标值；失败时为默认值。</param>
    /// <returns>转换成功时返回 <see langword="true"/>。</returns>
    private static bool LegacyTypedTryTo(TSource input, out TTarget result)
    {
        result = default!;
        if (input is null || input is DBNull)
            return false;
        try
        {
            var targetType = GetUnderlyingType(typeof(TTarget));
            var boxedInput = (object)input;
            result = (TTarget)Convert.ChangeType(boxedInput, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }

    /// <summary>
    /// 将当前输入场景映射为对应源类型的数值。
    /// </summary>
    /// <returns>在测量区外创建的源值。</returns>
    private object CreateInput()
    {
        var sourceType = GetUnderlyingType(typeof(TSource));
        var targetType = GetUnderlyingType(typeof(TTarget));
        if (sourceType == typeof(int))
        {
            return InputCase switch
            {
                NumericInputCase.SourceMinimum => int.MinValue,
                NumericInputCase.SourceMaximum => int.MaxValue,
                _ => 12345
            };
        }
        if (sourceType == typeof(long))
        {
            return InputCase switch
            {
                NumericInputCase.SourceMinimum => targetType == typeof(int) ? (long)int.MinValue : long.MinValue,
                NumericInputCase.SourceMaximum => targetType == typeof(int) ? (long)int.MaxValue : long.MaxValue,
                NumericInputCase.OverflowLow => long.MinValue,
                NumericInputCase.OverflowHigh => long.MaxValue,
                _ => 12345L
            };
        }
        if (sourceType == typeof(float))
        {
            return InputCase switch
            {
                NumericInputCase.OverflowLow => float.MinValue,
                NumericInputCase.OverflowHigh => float.MaxValue,
                NumericInputCase.NaN => float.NaN,
                NumericInputCase.PositiveInfinity => float.PositiveInfinity,
                NumericInputCase.NegativeInfinity => float.NegativeInfinity,
                _ => 12345.625f
            };
        }
        if (sourceType == typeof(double))
        {
            return InputCase switch
            {
                NumericInputCase.OverflowLow => double.MinValue,
                NumericInputCase.OverflowHigh => double.MaxValue,
                NumericInputCase.NaN => double.NaN,
                NumericInputCase.PositiveInfinity => double.PositiveInfinity,
                NumericInputCase.NegativeInfinity => double.NegativeInfinity,
                _ => 12345.625d
            };
        }
        if (sourceType == typeof(decimal))
        {
            return InputCase switch
            {
                NumericInputCase.OverflowLow => decimal.MinValue,
                NumericInputCase.OverflowHigh => decimal.MaxValue,
                _ => 12345.625m
            };
        }
        throw new NotSupportedException($"未配置数值类型：{sourceType}");
    }

    /// <summary>
    /// 获取 nullable 包装对应的基础类型。
    /// </summary>
    /// <param name="type">声明的源类型或目标类型。</param>
    /// <returns>去除 nullable 包装后的类型。</returns>
    private static Type GetUnderlyingType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// 判断类型是否为本基准使用的整数类型。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>类型为 <see cref="int"/> 或 <see cref="long"/> 时返回 <see langword="true"/>。</returns>
    private static bool IsIntegralType(Type type) => type == typeof(int) || type == typeof(long);

    /// <summary>
    /// 判断类型是否为本基准使用的浮点或十进制类型。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>类型为 <see cref="float"/>、<see cref="double"/> 或 <see cref="decimal"/> 时返回 <see langword="true"/>。</returns>
    private static bool IsFloatingType(Type type) => type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    /// <summary>
    /// 判断类型是否为可转换到整数的浮点或十进制类型。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>类型为 <see cref="float"/>、<see cref="double"/> 或 <see cref="decimal"/> 时返回 <see langword="true"/>。</returns>
    private static bool IsFloatingOrDecimalType(Type type) => IsFloatingType(type);

    /// <summary>
    /// 描述跨数值基准使用的输入类别。
    /// </summary>
    public enum NumericInputCase
    {
        /// <summary>通常的成功输入。</summary>
        Success,
        /// <summary>源类型的最小值。</summary>
        SourceMinimum,
        /// <summary>源类型的最大值。</summary>
        SourceMaximum,
        /// <summary>超出目标下界的输入。</summary>
        OverflowLow,
        /// <summary>超出目标上界的输入。</summary>
        OverflowHigh,
        /// <summary>非数字浮点输入。</summary>
        NaN,
        /// <summary>正无穷浮点输入。</summary>
        PositiveInfinity,
        /// <summary>负无穷浮点输入。</summary>
        NegativeInfinity
    }
}
