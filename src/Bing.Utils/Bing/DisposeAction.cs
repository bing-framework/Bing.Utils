namespace Bing;

/// <summary>
/// 释放操作
/// </summary>
public class DisposeAction : IDisposable
{
    /// <summary>
    /// 操作
    /// </summary>
    private readonly Action _action;

    /// <summary>
    /// 初始化一个<see cref="DisposeAction"/>类型的实例
    /// </summary>
    /// <param name="action">操作</param>
    public DisposeAction(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose() => _action();
}

/// <summary>
/// 释放操作
/// </summary>
/// <typeparam name="T">类型</typeparam>
public class DisposeAction<T> : IDisposable
{
    /// <summary>
    /// 操作
    /// </summary>
    private readonly Action<T> _action;

    /// <summary>
    /// 参数
    /// </summary>
    private readonly T _parameter;

    /// <summary>
    /// 初始化一个<see cref="DisposeAction{T}"/>类型的实例
    /// </summary>
    /// <param name="action">操作</param>
    /// <param name="parameter">参数</param>
    public DisposeAction(Action<T> action, T parameter)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _parameter = parameter;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_parameter != null)
            _action(_parameter);
    }
}