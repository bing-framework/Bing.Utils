using System.Globalization;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 系统扩展 - 格式化扩展
/// </summary>
public static partial class BingExtensions
{
    #region Description(获取布尔值描述)

    /// <summary>
    /// 获取布尔值描述
    /// </summary>
    /// <param name="value">值</param>
    public static string Description(this bool value) => value ? "是" : "否";

    /// <summary>
    /// 获取布尔值描述
    /// </summary>
    /// <param name="value">值</param>
    public static string Description(this bool? value) => value == null ? "" : Description(value.Value);

    #endregion

    #region FormatInvariant(格式化字符串，不依赖区域性)

    /// <summary>
    /// 格式化字符串，不依赖区域性
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public static string FormatInvariant(this string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

    #endregion

    #region FormatCurrent(格式化字符串，依赖当前区域性)

    /// <summary>
    /// 格式化字符串，依赖当前区域性
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public static string FormatCurrent(this string format, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    #endregion

    #region FormatCurrentUI(格式化字符串，依赖当前UI区域性)

    /// <summary>
    /// 格式化字符串，依赖当前UI区域性
    /// </summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    // ReSharper disable once InconsistentNaming
    public static string FormatCurrentUI(this string format, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, format, args);

    #endregion
}