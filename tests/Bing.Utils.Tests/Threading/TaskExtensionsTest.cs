using Bing.Threading;

namespace Bing.Utils.Tests.Threading;

/// <summary>
/// 任务(<see cref="Task"/>) 扩展 测试
/// </summary>
public class TaskExtensionsTest
{
    /// <summary>
    /// 测试 - 当任务在超时时间内完成时，WaitResult 方法是否正常运行。
    /// </summary>
    [Fact]
    public void Test_WaitResult_TaskCompletesWithinTimeout_DoesNotThrowException()
    {
        // Arrange
        var task = Task.Delay(500); // 设置一个足够大的超时时间，确保任务能够在超时之前完成。

        // Act and Assert
        task.WaitResult(1000); // 在超时时间内完成，不应该引发异常。
    }

    /// <summary>
    /// 测试 - 当任务未能在超时时间内完成时，WaitResult 方法是否引发 <see cref="TimeoutException"/> 异常。
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
    /// 测试 - 当任务在超时时间内完成时，WaitResult 方法是否返回正确的结果。
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
    /// 测试 - 当传递的任务在超时之前完成时，TimeoutAfter 方法是否返回正确的结果。
    /// </summary>
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
    /// 测试 - 当传递的任务未能在超时时间内完成时，TimeoutAfter 方法是否引发 <see cref="TimeoutException"/> 异常。
    /// </summary>
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