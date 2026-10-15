namespace Bing.Threading;
/// <summary>
/// 任务(<see cref="Task"/>) 扩展 测试
/// </summary>
public class TaskExtensionsTest
{
    /// <summary>
    /// 验证已完成任务在时限内等待时不抛出异常。
    /// </summary>
    [Fact]
    public void Test_WaitResult_TaskCompletesWithinTimeout_DoesNotThrowException()
    {
        // Arrange
        var task = Task.CompletedTask;
        // Act and Assert
        task.WaitResult(1000);
    }
    /// <summary>
    /// 验证等待任务超过时限后抛出超时异常。
    /// </summary>
    [Fact]
    public void Test_WaitResult_TaskDoesNotCompleteWithinTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.Delay(2000); // 设置一个小的超时时间，确保任务不能在超时之前完成。
        // Act and Assert
        Assert.Throws<TimeoutException>(() => task.WaitResult(1000));
    }
    /// <summary>
    /// 验证时限内完成的任务返回原结果。
    /// </summary>
    [Fact]
    public void Test_WaitResult_TaskCompletesWithinTimeout_ReturnsCorrectResult()
    {
        // Arrange
        var expectedResult = 42;
        var task = Task.FromResult(expectedResult);
        // Act
        var result = task.WaitResult(1000); // 设置一个足够大的超时时间，确保任务能够在超时之前完成。
        // Assert
        Assert.Equal(expectedResult, result);
    }
    /// <summary>
    /// 验证异步超时包装返回时限内完成的任务结果。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task Test_TimeoutAfter_CompletesBeforeTimeout_ReturnsCorrectResult()
    {
        // Arrange
        var expectedResult = 42;
        var task = Task.FromResult(expectedResult);
        // Act
        var result = await task.TimeoutAfter(1000); // 设置一个足够大的超时时间，确保任务能够在超时之前完成。
        // Assert
        Assert.Equal(expectedResult, result);
    }
    /// <summary>
    /// 验证异步超时包装在任务超过时限后抛出异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task Test_TimeoutAfter_TimeoutOccurs_ThrowsTimeoutException()
    {
        // Arrange
        var delayMilliseconds = 2000; // 设置一个小的超时时间，确保任务不能在超时之前完成。
        var task = Task.Delay(delayMilliseconds);
        // Act and Assert
        await Assert.ThrowsAsync<TimeoutException>(async () => await task.TimeoutAfter(1000));
    }
}
