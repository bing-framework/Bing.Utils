namespace Bing.Date;

/// <summary>
/// 区间模式，用于判断范围包含边界的方式。
/// </summary>
public enum RangeMode
{
    /// <summary>
    /// 开区间：(start, end)，不包含起始与结束。
    /// </summary>
    Open,

    /// <summary>
    /// 闭区间：[start, end]，包含起始与结束。
    /// </summary>
    Close,

    /// <summary>
    /// >左闭右开区间：[start, end)，包含起始但不包含结束。
    /// </summary>
    OpenClose,

    /// <summary>
    /// 左开右闭区间：(start, end]，包含结束但不包含起始。
    /// </summary>
    CloseOpen
}