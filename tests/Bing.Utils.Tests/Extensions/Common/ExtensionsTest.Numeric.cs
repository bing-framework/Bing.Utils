using Bing.Extensions;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Utils.Tests.Extensions
{
    /// <summary>
    /// 系统扩展测试 - 数字
    /// </summary>
    public partial class ExtensionsTest
    {
        /// <summary>
        /// 测试 - 保留小数位数
        /// </summary>
        /// <param name="input">输入值</param>
        /// <param name="result">结果</param>
        /// <param name="digits">小数位数</param>
        [Theory]
        [InlineData(10.3f, 10.30f, 2)]
        [InlineData(10.37f, 10.37f, 2)]
        [InlineData(10.374f, 10.37f, 2)]
        [InlineData(10.376f, 10.38f, 2)]
        [InlineData(10.375f, 10.38f, 2)]
        [InlineData(10.375f, 10.375f, 3)]
        [InlineData(10.3756f, 10.376f, 3)]
        public void Test_KeepDigits(float input, float result, int digits)
        {
            Assert.Equal(result, input.KeepDigits(digits));
        }
    }
}
