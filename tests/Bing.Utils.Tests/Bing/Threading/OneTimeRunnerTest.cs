namespace Bing.Threading;
/// <summary>
/// 一次性运行器 测试
/// </summary>
public class OneTimeRunnerTest
{
    /// <summary>
    /// 验证一次性运行器仅在首次调用时执行操作。
    /// </summary>
    [Fact]
    public void Test_Run_ActionExecutedOnce_SuccessfulOnFirstCall_IgnoredOnSubsequentCalls()
    {
        // Arrange
        var oneTimeRunner = new OneTimeRunner();
        var executionCount = 0;
        // Act
        oneTimeRunner.Run(() => { executionCount++; });
        // Assert
        Assert.Equal(1, executionCount);
        // Act again
        oneTimeRunner.Run(() => { executionCount++; });
        // Assert again
        Assert.Equal(1, executionCount);
    }
    /// <summary>
    /// 验证一次性运行器在并发调用时仅执行一次操作。
    /// </summary>
    [Fact]
    public void Test_Run_MultipleThreads_ActionExecutedOnce()
    {
        // Arrange
        var oneTimeRunner = new OneTimeRunner();
        var executionCount = 0;
        var threadCount = 10;
        // Act
        Parallel.For(0, threadCount, _ =>
        {
            oneTimeRunner.Run(() => { executionCount++; });
        });
        // Assert
        Assert.Equal(1, executionCount);
    }
}
