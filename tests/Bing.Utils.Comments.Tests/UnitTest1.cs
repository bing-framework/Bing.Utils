using System.Reflection;
using Bing.Comments;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Comments.Tests
{
    public class UnitTest1 : TestBase
    {
        /// <summary>
        /// 初始化一个<see cref="TestBase"/>类型的实例
        /// </summary>
        public UnitTest1(ITestOutputHelper output) : base(output)
        {
        }

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
