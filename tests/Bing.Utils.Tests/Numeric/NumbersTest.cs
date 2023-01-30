using Bing.Numeric;

namespace Bing.Utils.Tests.Numeric;

public class NumbersTest
{
    [Theory]
    [InlineData(1.2345678900, 1.2345678900, 10)]
    [InlineData(1.2345678900, 1.234567890, 9)]
    [InlineData(1.2345678900, 1.23456789, 8)]
    [InlineData(1.2345678900, 1.2345678, 7)]
    [InlineData(1.2345678900, 1.234567, 6)]
    [InlineData(1.2345678900, 1.23456, 5)]
    [InlineData(1.2345678900, 1.2345, 4)]
    [InlineData(1.2345678900, 1.234, 3)]
    [InlineData(1.2345678900, 1.23, 2)]
    [InlineData(1.2345678900, 1.2, 1)]
    [InlineData(1.2345678900, 1, 0)]
    public void Test_RoundTruncate_1(decimal input, decimal result, int precision)
    {
        Assert.Equal(result, input.RoundTruncate(precision));
    }

    [Theory]
    [InlineData(3.4611, 3.46, 2)]
    [InlineData(-3.4611, -3.46, 2)]
    [InlineData(3.4679, 3.46, 2)]
    [InlineData(-3.4679, -3.46, 2)]
    public void Test_RoundTruncate_2(decimal input, decimal result, int precision)
    {
        Assert.Equal(result, input.RoundTruncate(precision));
    }

    [Theory]
    [InlineData(-1.129, -1.12, 2)]
    [InlineData(-1.120, -1.12, 2)]
    [InlineData(-1.125, -1.12, 2)]
    [InlineData(-1.1255, -1.12, 2)]
    [InlineData(-1.1254, -1.12, 2)]
    [InlineData(0.0001, 0, 3)]
    [InlineData(-0.0001, 0, 3)]
    [InlineData(0.0000, 0, 3)]
    [InlineData(1.12, 1.1, 1)]
    [InlineData(1.15, 1.1, 1)]
    [InlineData(1.19, 1.1, 1)]
    [InlineData(1.111, 1.1, 1)]
    [InlineData(1.199, 1.1, 1)]
    [InlineData(1.2, 1.2, 1)]
    [InlineData(0.14, 0.1, 1)]
    [InlineData(-0.05, 0, 1)]
    [InlineData(-0.049, 0, 1)]
    [InlineData(-0.051, 0, 1)]
    [InlineData(-0.14, -0.1, 1)]
    [InlineData(-0.15, -0.1, 1)]
    [InlineData(-0.16, -0.1, 1)]
    [InlineData(-0.19, -0.1, 1)]
    [InlineData(-0.199, -0.1, 1)]
    [InlineData(-0.101, -0.1, 1)]
    [InlineData(-0.099, 0, 1)]
    [InlineData(-0.001, 0, 1)]
    [InlineData(1.99, 1, 0)]
    [InlineData(1.01, 1, 0)]
    [InlineData(-1.01, -1, 0)]
    [InlineData(-1.99, -1, 0)]
    public void Test_RoundTruncate_3(decimal input, decimal result, int precision)
    {
        Assert.Equal(result, input.RoundTruncate(precision));
    }
}