using System.Threading.Tasks;
using Bing.Threading;

namespace Bing.Utils.Tests.Threading;

/// <summary>
/// 一次性运行器 测试
/// </summary>
public class OneTimeRunnerTest
{
    /// <summary>
    /// 测试 - 当运行的操作在第一次调用时成功执行，但在后续调用时被忽略。
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
    /// 测试 - 运行的操作在多线程环境下也只执行一次。
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