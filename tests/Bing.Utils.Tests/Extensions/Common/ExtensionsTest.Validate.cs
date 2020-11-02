using Bing.Extensions;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.Utils.Tests.Extensions
{
    /// <summary>
    /// 系统扩展测试 - 验证扩展
    /// </summary>
    public partial class ExtensionsTest : TestBase
    {
        /// <summary>
        /// 检查空值，不为空则正常执行
        /// </summary>
        [Fact]
        public void Test_CheckNull()
        {
            var test = new object();
            test.CheckNull(nameof(test));
        }
    }
}
