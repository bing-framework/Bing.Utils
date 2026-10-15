using Xunit.Abstractions;

namespace Bing.Utils.Comments.Tests
{
    /// <summary>
    /// 提供测试输出的基类。
    /// </summary>
    public abstract class TestBase
    {
        /// <summary>
        /// 获取当前测试使用的输出助手。
        /// </summary>
        protected ITestOutputHelper Output { get; }

        /// <summary>
        /// 初始化一个 <see cref="TestBase"/> 类型的实例。
        /// </summary>
        /// <param name="output">当前测试的输出助手。</param>
        protected TestBase(ITestOutputHelper output) => Output = output;
    }
}
