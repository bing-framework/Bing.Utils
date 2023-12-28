namespace Bing.Threading;

/// <summary>
/// 提供预先缓存的结果。
/// </summary>
public static class TaskCache
{
    /// <summary>
    /// 预先缓存的返回 true 结果
    /// </summary>
    public static Task<bool> TrueResult { get; }

    /// <summary>
    /// 预先缓存的返回 false 结果
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