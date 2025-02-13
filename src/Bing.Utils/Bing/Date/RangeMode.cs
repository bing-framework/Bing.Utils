namespace Bing.Date;

/// <summary>
/// 区间模式
/// </summary>
public enum RangeMode
{
    /// <summary>
    /// 开区间，不包括开始和结束。
    /// </summary>
    Open,

    /// <summary>
    /// 闭区间，包括开始和结束。
    /// </summary>
    Close,

    /// <summary>
    /// 左开右闭区间，包括开始但不包括结束。
    /// </summary>
    OpenClose,

    /// <summary>
    /// 左闭右开区间，包括结束但不包括开始。
    /// </summary>
    CloseOpen
}