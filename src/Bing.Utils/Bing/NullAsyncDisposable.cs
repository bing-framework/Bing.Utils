namespace Bing;

/// <summary>
/// 表示一个空的异步可释放对象，用于实现 <see cref="IAsyncDisposable"/> 接口但不执行任何实际的资源释放操作。
/// 这是 <see cref="NullDisposable"/> 的异步版本，遵循相同的设计模式。
/// </summary>
public sealed class NullAsyncDisposable : IAsyncDisposable
{
    /// <summary>
    /// 获取 <see cref="NullAsyncDisposable"/> 类的单例实例。
    /// 所有代码都应该使用此共享实例而不是创建新实例。
    /// </summary>
    public static NullAsyncDisposable Instance { get; } = new();

    /// <summary>
    /// 初始化一个<see cref="NullAsyncDisposable"/>类型的实例
    /// </summary>
    private NullAsyncDisposable() { }

    /// <summary>
    /// 实现 <see cref="IAsyncDisposable.DisposeAsync"/> 方法，但不执行任何资源释放操作。
    /// </summary>
    /// <returns>表示异步释放操作的 <see cref="ValueTask"/>，此任务已完成且不包含任何工作。</returns>
    /// <remarks>
    /// 此方法总是返回 <c>default</c> 值的 <see cref="ValueTask"/>，这表示一个已完成的无值任务，
    /// 不会分配新的对象，符合高性能的资源管理要求。
    /// </remarks>
    public ValueTask DisposeAsync() => default;
}