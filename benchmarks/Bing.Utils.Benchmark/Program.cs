using BenchmarkDotNet.Running;

namespace Bing.Utils.Benchmark;

using Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 启动基准测试程序。
/// </summary>
internal class Program
{
    /// <summary>
    /// 处理基准测试命令行参数。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    static void Main(string[] args)
    {
        if (args.Any(arg => arg.StartsWith("--image-performance=", StringComparison.OrdinalIgnoreCase)))
        {
            ImagePerformanceProbe.Run(args);
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--reflection-cache", StringComparison.OrdinalIgnoreCase)))
        {
            ReflectionBenchmarks.RunConcurrentCacheProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--reflection-concurrent", StringComparison.OrdinalIgnoreCase)))
        {
            ReflectionBenchmarks.RunConcurrentSchemaProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--reflection-capacity", StringComparison.OrdinalIgnoreCase)))
        {
            ReflectionBenchmarks.RunCapacityProbe(args);
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--reflection-struct-map-steady", StringComparison.OrdinalIgnoreCase)))
        {
            ReflectionBenchmarks.RunStructMapSteadyProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--reflection-cold", StringComparison.OrdinalIgnoreCase)))
        {
            ReflectionBenchmarks.RunColdStartProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-steady", StringComparison.OrdinalIgnoreCase)))
        {
            var benchmark = new ConvSteadyStateBenchmarks();
            benchmark.Setup();
            benchmark.InterleavedSteadyState();
            benchmark.WriteEvidence();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-baseline", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunBaseline();
            return;
        }

        var firstCall = args.FirstOrDefault(arg =>
            string.Equals(arg, "--conv-first-call", StringComparison.OrdinalIgnoreCase) ||
            arg.StartsWith("--conv-first-call=", StringComparison.OrdinalIgnoreCase));
        if (firstCall != null)
        {
            var separator = firstCall.IndexOf('=');
            ConvBenchmarks.RunFirstCallProbe(separator < 0 ? null : firstCall[(separator + 1)..]);
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-memory", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunMemoryProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-sustained-gc-list", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunSustainedGcProbe(listOnly: true);
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-sustained-gc", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunSustainedGcProbe();
            return;
        }

        if (args.Any(arg => string.Equals(arg, "--conv-collection-compare", StringComparison.OrdinalIgnoreCase)))
        {
            ConvBenchmarks.RunCollectionComparison();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
