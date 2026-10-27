using System.Threading;
namespace Bing.Threading;
/// <summary>
/// 线程帮助类 测试
/// </summary>
public class ThreadHelperTest
{
    /// <summary>
    /// 验证返回首个成功结果并取消其余任务。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task NeedOnlyOne_Should_Return_First_Completed_Task_Result()
    {
        // Arrange
        var task1Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task1CancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task2Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task2Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task2WasCancelled = false;
        Func<CancellationToken, Task<string>> task1 = async (cancellationToken) =>
        {
            task1Started.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return "任务1完成";
            }
            catch (OperationCanceledException)
            {
                task1CancellationObserved.TrySetResult(true);
                throw;
            }
        };
        Func<CancellationToken, Task<string>> task2 = async (cancellationToken) =>
        {
            task2Started.TrySetResult(true);
            await task2Release.Task.ConfigureAwait(false);
            task2WasCancelled = cancellationToken.IsCancellationRequested;
            return "任务2完成";
        };
        // Act
        var operation = ThreadHelper.NeedOnlyOne(task1, task2);
        await Task.WhenAll(task1Started.Task, task2Started.Task);
        task2Release.SetResult(true);
        var result = await operation;

        // 超时仅用于限制失败测试的等待，不参与任务先后协调。
        var cancellationWait = await Task.WhenAny(task1CancellationObserved.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        // Assert
        Assert.Equal("任务2完成", result);
        Assert.Same(task1CancellationObserved.Task, cancellationWait);
        Assert.True(await task1CancellationObserved.Task);
        Assert.False(task2WasCancelled);
    }
    /// <summary>
    /// 验证全部任务取消或失败后汇总异常。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task NeedOnlyOne_Should_Throw_Exception_When_All_Tasks_Cancelled_Or_Faulted()
    {
        // Arrange
        Func<CancellationToken, Task<string>> task1 = async (cancellationToken) =>
        {
            await Task.Delay(500, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        };
        Func<CancellationToken, Task<string>> task2 = async (cancellationToken) =>
        {
            await Task.Delay(500, cancellationToken);
            throw new Exception("任务2出错");
        };
        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(() => ThreadHelper.NeedOnlyOne(task1, task2));
        Assert.Contains(exception.InnerExceptions, ex => ex is OperationCanceledException);
        Assert.Contains(exception.InnerExceptions, ex => ex.Message == "任务2出错");
    }
    /// <summary>
    /// 验证竞速执行拒绝空或无元素的函数集合。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task NeedOnlyOne_Should_Throw_Exception_When_Functions_Null_Or_Empty()
    {
        // Arrange
        Func<CancellationToken, Task<string>>[] functions = null;
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThreadHelper.NeedOnlyOne(functions));
        // Empty array
        functions = Array.Empty<Func<CancellationToken, Task<string>>>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => ThreadHelper.NeedOnlyOne(functions));
    }
    /// <summary>
    /// 验证先完成的任务失败后返回后续成功结果。
    /// </summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [Fact]
    public async Task NeedOnlyOne_Should_Return_Next_Completed_Task_Result_When_First_Faulted()
    {
        // Arrange
        Func<CancellationToken, Task<string>> task1 = async (cancellationToken) =>
        {
            await Task.Delay(500, cancellationToken);
            throw new Exception("任务1失败");
        };
        Func<CancellationToken, Task<string>> task2 = async (cancellationToken) =>
        {
            await Task.Delay(1000, cancellationToken);
            return "任务2完成";
        };
        // Act
        var result = await ThreadHelper.NeedOnlyOne(task1, task2);
        // Assert
        Assert.Equal("任务2完成", result);
    }
}
