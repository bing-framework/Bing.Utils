namespace Bing.Date.TimeZones;

/// <summary>
/// 指定夏令时回拨造成的本地时间歧义如何解析。
/// </summary>
public enum AmbiguousTimeResolution
{
    /// <summary>
    /// 拒绝无法唯一确定时间点的本地时间。
    /// </summary>
    Reject = 0,

    /// <summary>
    /// 选择对应较早 UTC 时间点的偏移。
    /// </summary>
    Earlier = 1,

    /// <summary>
    /// 选择对应较晚 UTC 时间点的偏移。
    /// </summary>
    Later = 2
}
