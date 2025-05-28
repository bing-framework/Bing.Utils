namespace Bing;

/// <summary>
/// 表示一个空的可释放对象，用于实现 <see cref="IDisposable"/> 接口但不执行任何实际的资源释放操作。
/// </summary>
public sealed class NullDisposable : IDisposable
{
    /// <summary>
    /// 获取 <see cref="NullDisposable"/> 类的单例实例。
    /// 所有代码都应该使用此共享实例而不是创建新实例。
    /// </summary>
    public static NullDisposable Instance { get; } = new();

    /// <summary>
    /// 初始化一个<see cref="NullDisposable"/>类型的实例
    /// </summary>
    private NullDisposable() { }

    /// <summary>
    /// 实现 <see cref="IDisposable.Dispose"/> 方法，但不执行任何资源释放操作。
    /// </summary>
    /// <remarks>
    /// 此方法是故意留空的，因为 NullDisposable 的设计目的就是提供一个不执行任何操作的 Dispose 方法。
    /// 当需要满足 IDisposable 接口约定但实际上没有资源需要释放时，可以使用此类。
    /// </remarks>
    public void Dispose() { }
}