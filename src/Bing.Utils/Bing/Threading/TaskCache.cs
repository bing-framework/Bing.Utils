namespace Bing.Threading;

/// <summary>
/// 异步缓存
/// </summary>
public static class TaskCache
{
    /// <summary>
    /// 返回true结果
    /// </summary>
    public static Task<bool> TrueResult { get; }

    /// <summary>
    /// 返回false结果
    /// </summary>
    public static Task<bool> FalseResult { get; }

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static TaskCache()
    {
        TrueResult = Task.FromResult(true);
        FalseResult = Task.FromResult(false);
    }
}