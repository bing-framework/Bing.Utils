using Bing.Date;

namespace Bing.IdUtils;

/// <summary>
/// 模型 ID 访问器
/// </summary>
public sealed class ModelIdAccessor
{
    /// <summary>
    /// 对象锁
    /// </summary>
    private readonly object _lockObj = new();

    /// <summary>
    /// 不重复时间戳工厂
    /// </summary>
    private readonly NoRepeatTimeStampFactory _factory = new();

    /// <summary>
    /// 索引
    /// </summary>
    private int Index { get; set; }

    /// <summary>
    /// 当前时间
    /// </summary>
    private DateTime Now { get; set; }

    /// <summary>
    /// 初始化一个<see cref="ModelIdAccessor"/>类型的实例
    /// </summary>
    public ModelIdAccessor() => Now = _factory.GetTimeStamp();

    /// <summary>
    /// 获取下一个索引
    /// </summary>
    public int GetNextIndex()
    {
        int ix;
        lock (_lockObj)
        {
            ix = Index;
            Index += 1;
        }

        return ix;
    }

    /// <summary>
    /// 获取时间点
    /// </summary>
    public DateTime GetTimeSpot() => Now;

    /// <summary>
    /// 刷新时间点
    /// </summary>
    public void RefreshTimeSpot() => Now = _factory.GetTimeStamp();
}