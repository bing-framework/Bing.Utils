using Bing.Net.IPv6;
using System.Numerics;

namespace Bing.Net;

[Trait("Bing.Net", "IPv6AddressAnalyzerAndPool")]
public class IPv6AddressAnalyzerAndPoolTest : TestBase
{
    public IPv6AddressAnalyzerAndPoolTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("::1", IPv6AddressType.Loopback)]
    [InlineData("::", IPv6AddressType.Unspecified)]
    [InlineData("ff02::1", IPv6AddressType.Multicast)]
    [InlineData("2001:db8::1", IPv6AddressType.Documentation)]
    [InlineData("2001:4860:4860::8888", IPv6AddressType.GlobalUnicast)]
    [InlineData("invalid", IPv6AddressType.Invalid)]
    public void GetAddressType_ShouldReturnExpected(string ip, IPv6AddressType expected)
    {
        IPv6AddressAnalyzer.GetAddressType(ip).ShouldBe(expected);
    }

    [Fact]
    public void IsPrivate_And_IsPublic_ShouldReturnExpected()
    {
        IPv6AddressAnalyzer.IsPrivate("::1").ShouldBeTrue();
        IPv6AddressAnalyzer.IsPrivate("2001:db8::1").ShouldBeTrue();
        IPv6AddressAnalyzer.IsPublic("2001:4860:4860::8888").ShouldBeTrue();
        IPv6AddressAnalyzer.IsPublic("::1").ShouldBeFalse();
    }

    [Fact]
    public void GetAddressScope_And_MulticastScope_ShouldReturnExpected()
    {
        IPv6AddressAnalyzer.GetAddressScope("2001:4860:4860::8888").ShouldBe(IPv6AddressScope.Global);
        IPv6AddressAnalyzer.GetMulticastScope("ff02::1").ShouldBe(IPv6AddressScope.Link);
        IPv6AddressAnalyzer.GetMulticastScope("2001:db8::1").ShouldBe(IPv6AddressScope.Invalid);
    }

    [Fact]
    public void AnalyzeAddress_ShouldReturnConsistentResult()
    {
        var result = IPv6AddressAnalyzer.AnalyzeAddress("2001:db8::1");
        result.AddressType.ShouldBe(IPv6AddressType.Documentation);
        result.Scope.ShouldBe(IPv6AddressScope.Documentation);
        result.IsPrivate.ShouldBeTrue();
        result.IsPublic.ShouldBeFalse();
        result.CompressedForm.ShouldNotBeNullOrWhiteSpace();
        result.ExpandedForm.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void IPv6AddressPool_GetNext_Reset_And_Exhaustion_ShouldWork()
    {
        var pool = new IPv6AddressPool
        {
            Addresses = new List<string> { "2001:db8::1", "2001:db8::2" },
            AddressType = IPv6AddressType.Documentation,
            Prefix = "2001:db8::",
            PrefixLength = 64,
            CreatedAt = DateTime.UtcNow
        };

        pool.GetNext().ShouldBe("2001:db8::1");
        pool.RemainingCount.ShouldBe(1);
        pool.IsExhausted.ShouldBeFalse();

        pool.GetNext().ShouldBe("2001:db8::2");
        pool.RemainingCount.ShouldBe(0);
        pool.IsExhausted.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => pool.GetNext());

        pool.Reset();
        pool.IsExhausted.ShouldBeFalse();
        pool.GetNext().ShouldBe("2001:db8::1");
    }

    [Fact]
    public void IPv6AddressStatistics_ToString_ShouldContainCounts()
    {
        var stats = new IPv6AddressStatistics
        {
            TotalCount = 10,
            ValidCount = 8,
            InvalidCount = 2,
            AddressRange = new BigInteger(100)
        };

        var text = stats.ToString();
        text.ShouldContain("Total: 10");
        text.ShouldContain("Valid: 8");
        text.ShouldContain("Invalid: 2");
        text.ShouldContain("Range: 100");
    }
}
