namespace Bing.Helpers;
/// <summary>
/// 快速路径匹配器测试
/// </summary>
[Trait("Bing.Helpers", "FastPathMatcher")]
public class FastPathMatcherTest : TestBase
{
    /// <inheritdoc />
    public FastPathMatcherTest(ITestOutputHelper output) : base(output)
    {
    }
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
    #region 基本匹配测试
    /// <summary>
    /// 测试 - 精确匹配
    /// </summary>
    [Theory]
    [InlineData("/api/users", "/api/users", true)]
    [InlineData("/api/users", "/api/orders", false)]
    [InlineData("", "", true)]
    [InlineData("/", "/", true)]
    [InlineData("/api", "/api/", false)]
    public void Match_ExactPattern_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    /// <summary>
    /// 测试 - null 和空值处理
    /// </summary>
    [Fact]
    public void Match_NullAndEmptyValues_HandlesCorrectly()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.Match(null, "/api/users"));
        FastPathMatcher.Match("/api/users", null).ShouldBeFalse();
        FastPathMatcher.Match("", "").ShouldBeTrue();
        FastPathMatcher.Match("*", "").ShouldBeTrue();
        FastPathMatcher.Match("**", "").ShouldBeTrue();
        FastPathMatcher.Match("?", "").ShouldBeFalse();
        Output.WriteLine("null 和空值处理测试通过");
    }
    #endregion
    #region 单级通配符测试
    /// <summary>
    /// 测试 - 单级通配符 (*)
    /// </summary>
    [Theory]
    [InlineData("*", "hello", true)]
    [InlineData("*", "hello/world", false)]
    [InlineData("/api/*", "/api/users", true)]
    [InlineData("/api/*", "/api/users/123", false)]
    [InlineData("*/users", "api/users", true)]
    [InlineData("*/users", "v1/api/users", false)]
    [InlineData("http://*", "http://localhost", true)]
    [InlineData("http://*", "https://localhost", false)]
    public void Match_SingleWildcard_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"单级通配符 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 多级通配符测试
    /// <summary>
    /// 测试 - 多级通配符 (**)
    /// </summary>
    [Theory]
    [InlineData("**", "any/path/here", true)]
    [InlineData("**", "", true)]
    [InlineData("/api/**", "/api/users/123/profile", true)]
    [InlineData("/api/**", "/api", false)]
    [InlineData("/api/**", "/v1/api/users", false)]
    [InlineData("**/swagger/**", "http://localhost/swagger/index.html", true)]
    [InlineData("**/swagger/**", "swagger/ui/index.html", false)]
    [InlineData("**/*.html", "path/to/file.html", true)]
    [InlineData("**/*.html", "path/to/file.css", false)]
    public void Match_MultipleWildcard_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"多级通配符 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 单字符通配符测试
    /// <summary>
    /// 测试 - 单字符通配符 (?)
    /// </summary>
    [Theory]
    [InlineData("?", "a", true)]
    [InlineData("?", "ab", false)]
    [InlineData("?", "/", false)]
    [InlineData("/api/user?", "/api/users", true)]
    [InlineData("/api/user?", "/api/user", false)]
    [InlineData("/api/???", "/api/123", true)]
    [InlineData("/api/???", "/api/1234", false)]
    [InlineData("file?.txt", "file1.txt", true)]
    [InlineData("file?.txt", "file12.txt", false)]
    public void Match_QuestionWildcard_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"单字符通配符 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 组合通配符测试
    /// <summary>
    /// 测试 - 组合通配符
    /// </summary>
    [Theory]
    [InlineData("/api/*/users/**", "/api/v1/users/123/profile", true)]
    [InlineData("/api/*/users/**", "/api/v1/orders/123", false)]
    [InlineData("http://*/swagger/**", "http://localhost:8080/swagger/index.html", true)]
    [InlineData("**/file?.txt", "path/to/file1.txt", true)]
    [InlineData("**/file?.txt", "path/to/file12.txt", false)]
    [InlineData("**/*/?", "path/to/a", true)]
    [InlineData("**/*/?", "path/to/ab", false)]
    public void Match_CombinedWildcards_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"组合通配符 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 实际场景测试
    /// <summary>
    /// 测试 - Swagger 路径匹配
    /// </summary>
    [Fact]
    public void Match_SwaggerPaths_WorksCorrectly()
    {
        // Arrange
        var patterns = new[]
        {
            "http://*/swagger/**",
            "**/swagger/**",
            "/swagger/**"
        };
        var paths = new[]
        {
            "http://192.168.0.1:8100/swagger/index.html",
            "http://localhost/swagger/swagger-ui.css",
            "/swagger/swagger-ui-bundle.js",
            "api/swagger/swagger-ui-standalone-preset.js",
            "/swagger/DevOps/swagger.json",
            "http://api.example.com/swagger/favicon-32x32.png"
        };
        // Act & Assert
        foreach (var path in paths)
        {
            var matched = false;
            foreach (var pattern in patterns)
            {
                if (FastPathMatcher.Match(pattern, path))
                {
                    matched = true;
                    Output.WriteLine($"路径 '{path}' 匹配模式 '{pattern}'");
                    break;
                }
            }
            matched.ShouldBeTrue($"路径 '{path}' 应该匹配至少一个 Swagger 模式");
        }
    }
    /// <summary>
    /// 测试 - API 路径过滤
    /// </summary>
    [Fact]
    public void Match_ApiPathFiltering_WorksCorrectly()
    {
        // Arrange
        var allowedPatterns = new[]
        {
            "/api/public/**",
            "/api/auth/login",
            "/api/auth/register",
            "/health/**",
            "/swagger/**"
        };
        var testCases = new[]
        {
            ("/api/public/users", true),
            ("/api/public/posts/123", true),
            ("/api/auth/login", true),
            ("/api/auth/logout", false),
            ("/api/private/users", false),
            ("/health/check", true),
            ("/swagger/index.html", true),
            ("/admin/users", false)
        };
        // Act & Assert
        foreach (var (path, shouldMatch) in testCases)
        {
            var isAllowed = FastPathMatcher.MatchAny(allowedPatterns, path);
            isAllowed.ShouldBe(shouldMatch, $"路径 '{path}' 的访问权限判断错误");
            Output.WriteLine($"路径 '{path}' - 允许访问: {isAllowed}");
        }
    }
    /// <summary>
    /// 测试 - 文件路径匹配
    /// </summary>
    [Theory]
    [InlineData("**/*.js", "src/components/App.js", true)]
    [InlineData("**/*.js", "src/components/App.ts", false)]
    [InlineData("src/**/*.test.js", "src/utils/helper.test.js", true)]
    [InlineData("src/**/*.test.js", "test/utils/helper.test.js", false)]
    [InlineData("**/node_modules/**", "project/node_modules/react/index.js", true)]
    [InlineData("**/node_modules/**", "project/src/index.js", false)]
    public void Match_FilePaths_ReturnsExpectedResult(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"文件路径 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 批量匹配测试
    /// <summary>
    /// 测试 - MatchAny 方法
    /// </summary>
    [Fact]
    public void MatchAny_MultiplePatterns_ReturnsExpectedResult()
    {
        // Arrange
        var patterns = new[] { "/api/users/*", "/api/orders/*", "/swagger/**" };
        // Act & Assert
        FastPathMatcher.MatchAny(patterns, "/api/users/123").ShouldBeTrue();
        FastPathMatcher.MatchAny(patterns, "/api/orders/456").ShouldBeTrue();
        FastPathMatcher.MatchAny(patterns, "/swagger/index.html").ShouldBeTrue();
        FastPathMatcher.MatchAny(patterns, "/api/products/789").ShouldBeFalse();
        FastPathMatcher.MatchAny(patterns, null).ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.MatchAny(null, "/api/users"));
    }
    /// <summary>
    /// 测试 - MatchAll 方法
    /// </summary>
    [Fact]
    public void MatchAll_MultiplePatterns_ReturnsExpectedResult()
    {
        // Arrange
        var patterns = new[] { "http://**", "http://localhost/swagger/**" };
        // Act & Assert
        FastPathMatcher.MatchAll(patterns, "http://localhost/swagger/index").ShouldBeTrue();
        FastPathMatcher.MatchAll(patterns, "http://localhost/swagger/index.html").ShouldBeTrue();
        FastPathMatcher.MatchAll(patterns, "https://localhost/swagger/index.html").ShouldBeFalse();
        FastPathMatcher.MatchAll(patterns, "http://localhost/api/users").ShouldBeFalse();
        FastPathMatcher.MatchAll(patterns, null).ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.MatchAll(null, "/api/users"));
    }
    /// <summary>
    /// 测试 - GetMatchedPattern 方法
    /// </summary>
    [Fact]
    public void GetMatchedPattern_MultiplePatterns_ReturnsFirstMatch()
    {
        // Arrange
        var patterns = new[] { "/api/users/*", "/api/*", "/swagger/**" };
        // Act & Assert
        FastPathMatcher.GetMatchedPattern(patterns, "/api/users/123").ShouldBe("/api/users/*");
        FastPathMatcher.GetMatchedPattern(patterns, "/api/orders/456").ShouldBeNull();
        FastPathMatcher.GetMatchedPattern(patterns, "/swagger/index.html").ShouldBe("/swagger/**");
        FastPathMatcher.GetMatchedPattern(patterns, "/unknown/path").ShouldBeNull();
        FastPathMatcher.GetMatchedPattern(patterns, null).ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.GetMatchedPattern(null, "/api/users"));
    }
    #endregion
    #region 模式验证测试
    /// <summary>
    /// 测试 - IsValidPattern 方法
    /// </summary>
    [Theory]
    [InlineData("/api/users", true)]
    [InlineData("/api/*", true)]
    [InlineData("/api/**", true)]
    [InlineData("/api/?", true)]
    [InlineData("/api/***", false)]
    [InlineData("****", false)]
    [InlineData(null, false)]
    [InlineData("", true)]
    public void IsValidPattern_DifferentPatterns_ReturnsExpectedResult(string pattern, bool expected)
    {
        // Act
        var result = FastPathMatcher.IsValidPattern(pattern);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"模式 '{pattern}' 验证结果: {result}");
    }
    #endregion
    #region 边界条件和性能测试
    /// <summary>
    /// 测试 - 长路径处理
    /// </summary>
    [Fact]
    public void Match_LongPaths_HandlesCorrectly()
    {
        // Arrange
        var longSegment = new string('a', 1000);
        var longPath = $"/api/{longSegment}/users/{longSegment}/profile";
        var pattern = "/api/*/users/*/profile";
        // Act
        var result = FastPathMatcher.Match(pattern, longPath);
        // Assert
        result.ShouldBeTrue();
        Output.WriteLine($"长路径测试通过，路径长度: {longPath.Length}");
    }
    /// <summary>
    /// 测试 - 性能测试
    /// </summary>
    [Fact]
    public void Match_PerformanceTest_CompletesQuickly()
    {
        // Arrange
        const int iterations = 10000;
        var pattern = "/api/**/users/*/profile";
        var path = "/api/v1/internal/users/123/profile";
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                FastPathMatcher.Match(pattern, path).ShouldBeTrue();
            }
        }, TimeSpan.FromSeconds(1));
        Output.WriteLine($"性能测试完成: {iterations} 次匹配在 1 秒内完成");
    }
    /// <summary>
    /// 测试 - 深层嵌套路径
    /// </summary>
    [Fact]
    public void Match_DeeplyNestedPaths_HandlesCorrectly()
    {
        // Arrange
        var deepPath = "/level1/level2/level3/level4/level5/level6/level7/level8/level9/level10/file.txt";
        var patterns = new[]
        {
            "**/*.txt",
            "**/level5/**",
            "/level1/**/file.txt",
            "/level1/level2/level3/**/*.txt"
        };
        // Act & Assert
        foreach (var pattern in patterns)
        {
            FastPathMatcher.Match(pattern, deepPath).ShouldBeTrue($"模式 '{pattern}' 应该匹配深层路径");
        }
        Output.WriteLine("深层嵌套路径测试通过");
    }
    /// <summary>
    /// 测试 - 特殊字符处理
    /// </summary>
    [Theory]
    [InlineData("/api/users-admin", "/api/users-admin", true)]
    [InlineData("/api/users_internal", "/api/users_internal", true)]
    [InlineData("/api/users.json", "/api/users.json", true)]
    [InlineData("/api/users@domain", "/api/users@domain", true)]
    [InlineData("/api/users%20encoded", "/api/users%20encoded", true)]
    public void Match_SpecialCharacters_HandlesCorrectly(string pattern, string path, bool expected)
    {
        // Act
        var result = FastPathMatcher.Match(pattern, path);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"特殊字符 - 模式: '{pattern}', 路径: '{path}', 结果: {result}");
    }
    #endregion
    #region 错误处理测试
    /// <summary>
    /// 测试 - 异常处理
    /// </summary>
    [Fact]
    public void ExceptionHandling_InvalidInputs_ThrowsExpectedExceptions()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.Match(null, "/path"));
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.MatchAny(null, "/path"));
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.MatchAll(null, "/path"));
        Should.Throw<ArgumentNullException>(() => FastPathMatcher.GetMatchedPattern(null, "/path"));
        Output.WriteLine("异常处理测试通过");
    }
    #endregion
    #region 回归测试
    /// <summary>
    /// 测试 - 原有测试用例的向后兼容性
    /// </summary>
    [Fact]
    public void BackwardCompatibility_OriginalTestCases_StillWork()
    {
        // Arrange
        var path = "http://localhost:5001/api/values";
        // Act & Assert - 原有的成功案例
        FastPathMatcher.Match("http://localhost:5001/api/values", path).ShouldBeTrue();
        FastPathMatcher.Match("*//localhost:5001/api/values", path).ShouldBeTrue();
        FastPathMatcher.Match("**/localhost:5001/api/values", path).ShouldBeTrue();
        FastPathMatcher.Match("**/localhost:5001/**", path).ShouldBeTrue();
        FastPathMatcher.Match("**localhost:5001**", path).ShouldBeTrue();
        // Act & Assert - 原有的失败案例
        FastPathMatcher.Match("localhost:5001/api/values", path).ShouldBeFalse();
        FastPathMatcher.Match("//localhost:5001/api/values", path).ShouldBeFalse();
        FastPathMatcher.Match("*localhost:5001/api/values", path).ShouldBeFalse();
        FastPathMatcher.Match("**/LOCALHOST:5001/**", path).ShouldBeFalse(); // 大小写敏感
        Output.WriteLine("向后兼容性测试通过");
    }
    #endregion
}
