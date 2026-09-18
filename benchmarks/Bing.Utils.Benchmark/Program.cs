using BenchmarkDotNet.Running;

namespace Bing.Utils.Benchmark;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}