using BenchmarkDotNet.Attributes;
using Bing.Text;
using Bing.Extensions;

namespace Bing.Utils.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class StringRemoveBenchmarks
{
    private readonly string _needle = "needle";

    private readonly char[] c = new char[] { 'e' };

    [Benchmark]
    public void ExtensionRemove()
    {
        _needle.Remove(c);
    }

    [Benchmark]
    public void StringsRemove()
    {
        Strings.RemoveChars(_needle, c);
    }
}