using Bing.Helpers;
using Xunit;

namespace Bing.Utils.Tests.Helpers
{
    /// <summary>
    /// 快速路径匹配器测试
    /// </summary>
    public class FastPathMatcherTest
    {
        /// <summary>
        /// 测试 - 匹配正确的模式 - 成功
        /// </summary>
        [Fact]
        public void Test_Match_WithCorrectPattern_ShouldSuccess()
        {
            var path = "http://localhost:5001/api/values";

            var pattern = "http://localhost:5001/api/values";
            var result = FastPathMatcher.Match(pattern, path);
            Assert.True(result);

            pattern = "*//localhost:5001/api/values";
            result = FastPathMatcher.Match(pattern, path);
            Assert.True(result);

            pattern = "**/localhost:5001/api/values";
            result = FastPathMatcher.Match(pattern, path);
            Assert.True(result);

            pattern = "**/localhost:5001/**";
            result = FastPathMatcher.Match(pattern, path);
            Assert.True(result);

            pattern = "**localhost:5001**";
            result = FastPathMatcher.Match(pattern, path);
            Assert.True(result);
        }

        /// <summary>
        /// 测试 - 匹配失败的模式 - 失败
        /// </summary>
        [Fact]
        public void Test_Match_WithWrongPattern_ShouldFail()
        {
            var path = "http://localhost:5001/api/values";

            var pattern = "localhost:5001/api/values";
            var result = FastPathMatcher.Match(pattern, path);
            Assert.False(result);

            pattern = "//localhost:5001/api/values";
            result = FastPathMatcher.Match(pattern, path);
            Assert.False(result);

            pattern = "*localhost:5001/api/values";
            result = FastPathMatcher.Match(pattern, path);
            Assert.False(result);

            pattern = "**/LOCALHOST:5001/**";
            result = FastPathMatcher.Match(pattern, path);
            Assert.False(result);
        }

        /// <summary>
        /// 测试 - 匹配Swagger路径
        /// </summary>
        [Fact]
        public void Test_Match_WithSwaggerPath()
        {
            var pattern = "http://*/swagger/**";
            var paths = new[]
            {
                "http://192.168.0.1:8100/swagger/index.html",
                "http://192.168.0.1:8100/swagger/swagger-ui.css",
                "http://192.168.0.1:8100/swagger/swagger-ui-bundle.js",
                "http://192.168.0.1:8100/swagger/swagger-ui-standalone-preset.js",
                "http://192.168.0.1:8100/swagger/DevOps/swagger.json",
                "http://192.168.0.1:8100/swagger/favicon-32x32.png"
            };
            foreach (var path in paths)
            {
                var result = FastPathMatcher.Match(pattern, path);
                Assert.True(result);
            }
        }

        /// <summary>
        /// 测试 - 匹配Swagger路径
        /// </summary>
        [Fact]
        public void Test_Match_WithSwaggerPath_1()
        {
            var pattern = "**/swagger/**";
            var paths = new[]
            {
                "http://192.168.0.1:8100/swagger/index.html",
                "http://192.168.0.1:8100/swagger/swagger-ui.css",
                "http://192.168.0.1:8100/swagger/swagger-ui-bundle.js",
                "http://192.168.0.1:8100/swagger/swagger-ui-standalone-preset.js",
                "http://192.168.0.1:8100/swagger/DevOps/swagger.json",
                "http://192.168.0.1:8100/swagger/favicon-32x32.png"
            };
            foreach (var path in paths)
            {
                var result = FastPathMatcher.Match(pattern, path);
                Assert.True(result);
            }
        }

        /// <summary>
        /// 测试 - 匹配Swagger路径
        /// </summary>
        [Fact]
        public void Test_Match_WithSwaggerPath_2()
        {
            var pattern = "/swagger/**";
            var paths = new[]
            {
                "/swagger/index.html",
                "/swagger/swagger-ui.css",
                "/swagger/swagger-ui-bundle.js",
                "/swagger/swagger-ui-standalone-preset.js",
                "/swagger/DevOps/swagger.json",
                "/swagger/favicon-32x32.png"
            };
            foreach (var path in paths)
            {
                var result = FastPathMatcher.Match(pattern, path);
                Assert.True(result);
            }
        }

    }
}