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
        var task1WasCancelled = false;
        var task2WasCancelled = false;
        Func<CancellationToken, Task<string>> task1 = async (cancellationToken) =>
        {
            try
            {
                await Task.Delay(2000, cancellationToken);
                return "任务1完成";
            }
            catch (OperationCanceledException)
            {
                task1WasCancelled = true;
                throw;
            }
        };
        Func<CancellationToken, Task<string>> task2 = async (cancellationToken) =>
        {
            try
            {
                await Task.Delay(1000, cancellationToken);
                return "任务2完成";
            }
            catch (OperationCanceledException)
            {
                task2WasCancelled = true;
                throw;
            }
        };
        // Act
        var result = await ThreadHelper.NeedOnlyOne(task1, task2);
        // Assert
        Assert.Equal("任务2完成", result);
        // 等待片刻，确保取消操作已传播
        await Task.Delay(100);
        Assert.True(task1WasCancelled);
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
