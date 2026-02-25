namespace Bing.Helpers;

/// <summary>
/// 测试类：`Common` 路径与重试相关边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Common.PathAndRetry.EdgeContract")]
public class CommonPathAndRetryEdgeContractTest
{
    /// <summary>
    /// 测试用例：`Clamp` 在 `min > max` 时应抛出 `ArgumentException(min)`
    /// </summary>
    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(-5, 2, -1)]
    public void Clamp_MinGreaterThanMax_ThrowsArgumentExceptionWithMinParam(int value, int min, int max)
    {
        var exception = Should.Throw<ArgumentException>(() => Common.Clamp(value, min, max));

        exception.ParamName.ShouldBe("min");
    }

    /// <summary>
    /// 测试用例：`RetryExecute` 在 `maxRetries=0` 且首轮成功时应仅调用一次
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRetryExecuteSuccessWithoutRetryCases))]
    public void RetryExecute_MaxRetriesZero_FirstAttemptSucceeds_InvokesOnce(TimeSpan? delay)
    {
        var count = 0;

        var result = Common.RetryExecute(() =>
        {
            count++;
            return 42;
        }, maxRetries: 0, delay: delay);

        result.ShouldBe(42);
        count.ShouldBe(1);
    }

    /// <summary>
    /// 测试用例：`SafeExecute(Func)` 在异常场景下应返回默认值，且只执行一次
    /// </summary>
    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    [InlineData(-1)]
    public void SafeExecute_WithException_ReturnsDefaultAndInvokesOnce(int defaultValue)
    {
        var count = 0;

        var result = Common.SafeExecute(() =>
        {
            count++;
            throw new InvalidOperationException("boom");
        }, defaultValue: defaultValue);

        result.ShouldBe(defaultValue);
        count.ShouldBe(1);
    }

    /// <summary>
    /// 测试用例：`GetParentDirectory` 在 `depth < 0` 时应抛出 `ArgumentOutOfRangeException(depth)`
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetParentDirectory_NegativeDepth_ThrowsArgumentOutOfRangeExceptionWithParamName(int depth)
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Common.GetParentDirectory(depth, Path.GetTempPath()));

        exception.ParamName.ShouldBe("depth");
    }

    /// <summary>
    /// 测试用例：`GetParentDirectory` 在空白起始路径下应抛出 `ArgumentException(startPath)`
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidStartPathCases))]
    public void GetParentDirectory_BlankStartPath_ThrowsArgumentExceptionWithParamName(string startPath)
    {
        var exception = Should.Throw<ArgumentException>(() => Common.GetParentDirectory(0, startPath));

        exception.ParamName.ShouldBe("startPath");
    }

    /// <summary>
    /// 测试用例：`GetParentDirectory` 在 `depth=0` 且起始路径不存在时，当前行为先抛出目录不存在异常
    /// </summary>
    [Fact]
    public void GetParentDirectory_DepthZero_WithNonExistingStartPath_CurrentlyThrowsDirectoryNotFoundException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"bing-common-missing-{Guid.NewGuid():N}");

        Should.Throw<DirectoryNotFoundException>(() => Common.GetParentDirectory(0, missingPath));
    }

    /// <summary>
    /// 测试用例：`GetParentDirectory` 在深度超过目录层级时，应返回最顶层可访问目录
    /// </summary>
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetParentDirectory_DepthExceedsHierarchy_ReturnsTopMostDirectory(int depth)
    {
        var root = Path.Combine(Path.GetTempPath(), $"bing-common-root-{Guid.NewGuid():N}");
        var nested = Path.Combine(root, "a", "b", "c");
        Directory.CreateDirectory(nested);
        try
        {
            var expected = GetTopMostDirectory(Path.GetFullPath(nested));

            var result = Common.GetParentDirectory(depth, nested);

            result.ShouldBe(expected);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// 测试数据：`maxRetries=0` 且首轮成功场景下的延迟参数
    /// </summary>
    public static IEnumerable<object[]> GetRetryExecuteSuccessWithoutRetryCases()
    {
        yield return new object[] { null };
        yield return new object[] { TimeSpan.Zero };
        yield return new object[] { TimeSpan.FromMilliseconds(10) };
    }

    /// <summary>
    /// 测试数据：空白起始路径输入
    /// </summary>
    public static IEnumerable<object[]> GetInvalidStartPathCases()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { " " };
        yield return new object[] { "\t" };
        yield return new object[] { "\r\n" };
    }

    private static string GetTopMostDirectory(string path)
    {
        var current = path;
        while (true)
        {
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent))
                return current;
            current = parent;
        }
    }
}
