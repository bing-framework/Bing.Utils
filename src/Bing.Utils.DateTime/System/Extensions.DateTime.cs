namespace System;

/// <summary>
/// 日期时间(<see cref="DateTime"/>) 扩展
/// </summary>
public static class BingDateTimeExtensions
{
    #region Clone(克隆)

    /// <summary>
    /// 复制日期时间值，保留完整的本地夏令时重复时间标记。
    /// </summary>
    /// <param name="dt">待复制的日期时间。</param>
    /// <returns>具有相同 ticks、Kind 和本地时间点选择的值副本。</returns>
    public static DateTime Clone(this DateTime dt) => dt;

    #endregion
}
