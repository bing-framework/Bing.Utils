using BenchmarkDotNet.Running;
using Bing.Utils.Benchmark.Benchmarks;

namespace Bing.Utils.Benchmark;

internal class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<StringRemoveBenchmarks>();
        Console.ReadLine();
    }
}