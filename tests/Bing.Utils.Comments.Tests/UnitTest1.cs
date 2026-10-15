using System.Reflection;
using Bing.Comments;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Comments.Tests
{
    /// <summary>
    /// 验证注释读取器能从反射成员创建注释结果。
    /// </summary>
    public class UnitTest1 : TestBase
    {
        /// <summary>
        /// 初始化一个 <see cref="UnitTest1"/> 类型的实例。
        /// </summary>
        /// <param name="output">用于记录测试输出的助手。</param>
        public UnitTest1(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// 验证 <see cref="CsCommentReader"/> 能读取反射成员的注释摘要和参数说明。
        /// </summary>
        [Fact]
        public void Test_GetComments()
        {
            var method = typeof(CsCommentReader).GetMethod("Create", new[] { typeof(MemberInfo) });
            var comment = CsCommentReader.Create(method);
            Output.WriteLine($"summary:{comment.Summary}");
            foreach (var param in comment.Param)
                Output.WriteLine($"param:{param}");
        }
    }
}