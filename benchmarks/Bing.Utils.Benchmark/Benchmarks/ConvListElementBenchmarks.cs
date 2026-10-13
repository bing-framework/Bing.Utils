using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较内置数值列表解析与逐段子字符串基线的分配成本。
/// </summary>
/// <typeparam name="T">列表元素类型。</typeparam>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(double))]
[GenericTypeArguments(typeof(decimal))]
public class ConvListElementBenchmarks<T>
{
    /// <summary>
    /// 无效数值项输入。
    /// </summary>
    private const string InputWithInvalidItem = "1, ,invalid, +2,3,4,5,6,7,8,9,10";

    /// <summary>
    /// 有效数值列表输入。
    /// </summary>
    private const string InputWithoutInvalidItem = "1,2,3,4,5,6,7,8,9,10";

    /// <summary>
    /// 当前列表输入，在测量开始前按场景设置。
    /// </summary>
    private string _input = InputWithoutInvalidItem;

    /// <summary>
    /// 是否包含空白项和转换失败项。
    /// </summary>
    [Params(false, true)]
    public bool IncludeInvalidItem { get; set; }

    /// <summary>
    /// 在基准计时前选择输入场景。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = IncludeInvalidItem ? InputWithInvalidItem : InputWithoutInvalidItem;
        if (!Conv.ToList<T>(_input).SequenceEqual(ConvertWithSubstringLoop(_input)))
            throw new InvalidOperationException("当前列表路径与逐段 Substring 基线结果不一致。");
    }

    /// <summary>
    /// 测量当前列表解析路径。
    /// </summary>
    /// <returns>解析后的列表。</returns>
    [Benchmark]
    public List<T> Current() => Conv.ToList<T>(_input);

    /// <summary>
    /// 测量上一轮逐段 Substring 后逐项转换的旧方法体。
    /// </summary>
    /// <returns>解析后的列表。</returns>
    [Benchmark(Baseline = true)]
    public List<T> SubstringBaseline() => ConvertWithSubstringLoop(_input);

    /// <summary>
    /// 复现上一轮逐段创建子字符串并逐项转换的列表方法体。
    /// </summary>
    /// <param name="input">逗号分隔的列表输入。</param>
    /// <returns>按原列表语义转换后的元素。</returns>
    private static List<T> ConvertWithSubstringLoop(string input)
    {
        var result = new List<T>();
        if (string.IsNullOrWhiteSpace(input))
            return result;

        var start = 0;
        for (var index = 0; index <= input.Length; index++)
        {
            if (index != input.Length && input[index] != ',')
                continue;
            if (index > start)
            {
                var item = input.Substring(start, index - start);
                if (!string.IsNullOrWhiteSpace(item))
                    result.Add(Conv.To<T>(item));
            }
            start = index + 1;
        }
        return result;
    }
}
