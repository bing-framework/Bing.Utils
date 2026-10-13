using System.Runtime.InteropServices;
using Shouldly;
using Xunit.Abstractions;

namespace Bing.Reflection;

/// <summary>验证测试确实运行于目标框架，避免向新版运行时滚动后误报旧运行时通过。</summary>
public class ReflectionRuntimeVerificationTests
{
    private readonly ITestOutputHelper _output;
    public ReflectionRuntimeVerificationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Runtime_TargetFramework_UsesExpectedNativeRuntime()
    {
#if NET8_0
        const int expected = 8;
#elif NET7_0
        const int expected = 7;
#elif NET6_0
        const int expected = 6;
#elif NET5_0
        const int expected = 5;
#elif NETCOREAPP3_1
        const int expected = 3;
#else
#error Add the expected runtime for this target framework.
#endif
        _output.WriteLine($"Actual runtime: {RuntimeInformation.FrameworkDescription}; CLR: {RuntimeEnvironment.GetRuntimeDirectory()}");
        Environment.Version.Major.ShouldBe(expected);
    }
}
