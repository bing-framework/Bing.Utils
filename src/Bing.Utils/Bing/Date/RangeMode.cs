namespace Bing.Date
{
    /// <summary>
    /// 区间模式
    /// </summary>
    public enum RangeMode
    {
        /// <summary>
        /// 开区间
        /// </summary>
        Open,

        /// <summary>
        /// 闭区间
        /// </summary>
        Close,

        /// <summary>
        /// 左开右闭区间
        /// </summary>
        OpenClose,

        /// <summary>
        /// 左闭右开区间
        /// </summary>
        CloseOpen
    }
}