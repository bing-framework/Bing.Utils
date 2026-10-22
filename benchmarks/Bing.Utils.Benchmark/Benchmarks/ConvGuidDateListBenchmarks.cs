using BenchmarkDotNet.Attributes;
using Bing.Helpers;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 比较 Guid 和日期列表与旧逐段截取路径。
/// </summary>
/// <typeparam name="T">列表元素类型。</typeparam>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(Guid))]
[GenericTypeArguments(typeof(DateTime))]
public class ConvGuidDateListBenchmarks<T>
{
    /// <summary>
    /// 选择是否包含失败项。
    /// </summary>
    [Params(false, true)]
    public bool IncludeInvalidItem { get; set; }

    /// <summary>
    /// 测量用的固定列表文本。
    /// </summary>
    private string _input = "";

    /// <summary>
    /// 初始化输入并确认两种方法体的结果相同。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var item = typeof(T) == typeof(Guid)
            ? "83B0233C-A24F-49FD-8083-1337209EBC9A"
            : "2024-01-02T03:04:05Z";
        _input = string.Join(",", Enumerable.Repeat(item, 10));
        if (IncludeInvalidItem)
            _input += ", ,invalid";
        if (!Current().SequenceEqual(SubstringBaseline()))
            throw new InvalidOperationException("当前列表转换与旧方法体结果不一致。");
    }

    /// <summary>
    /// 测量当前列表转换。
    /// </summary>
    /// <returns>转换后的列表。</returns>
    [Benchmark]
    public List<T> Current() => Conv.ToList<T>(_input);

    /// <summary>
    /// 测量旧逐段截取方法体。
    /// </summary>
    /// <returns>转换后的列表。</returns>
    [Benchmark(Baseline = true)]
    public List<T> SubstringBaseline()
    {
        var result = new List<T>();
        var start = 0;
        for (var index = 0; index <= _input.Length; index++)
        {
            if (index != _input.Length && _input[index] != ',')
                continue;
            if (index > start)
            {
                var item = _input.Substring(start, index - start);
                if (!string.IsNullOrWhiteSpace(item))
                    result.Add(Conv.To<T>(item));
            }
            start = index + 1;
        }
        return result;
    }
}
