﻿namespace Bing.Threading;

/// <summary>
/// 一次性运行器，确保指定的操作只运行一次。<br />
/// <see cref="OneTimeRunner"/> 通常作为类的 私有、静态、只读 对象实例，确保在类的声明周期中，某个操作只执行一次。<br />
/// <example>
/// <code>
/// private static readonly OneTimeRunner runner = new OneTimeRunner();
/// </code>
/// </example>
/// </summary>
public class OneTimeRunner
{
    /// <summary>
    /// 是否已经运行过
    /// </summary>
    private volatile bool _runBefore;

    /// <summary>
    /// 运行指定的操作，确保操作只运行一次。
    /// </summary>
    /// <param name="action">操作</param>
    public void Run(Action action)
    {
        if (_runBefore)
            return;
        lock (this)
        {
            if (_runBefore)
                return;
            action();
            _runBefore = true;
        }
    }
}