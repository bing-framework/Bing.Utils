using Bing.Tests;

namespace Bing.Net;

/// <summary>
/// CIDR计算器 单元测试
/// </summary>
[Trait("Bing.Net", "CidrCalculator")]
public class CidrCalculatorTest : TestBase
{
    /// <inheritdoc />
    public CidrCalculatorTest(ITestOutputHelper output) : base(output)
    {
    }
}