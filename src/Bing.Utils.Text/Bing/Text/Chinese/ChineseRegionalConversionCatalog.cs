using System;
using System.IO;

namespace Bing.Text.Chinese;

/// <summary>
/// 使用外置地区词表转换简体与地区繁体中文。
/// </summary>
public sealed class ChineseRegionalConversionCatalog
{
    /// <summary>
    /// 基础简繁转换目录。
    /// </summary>
    private readonly ChineseConversionCatalog _baseCatalog;

    /// <summary>
    /// 标准繁体到地区繁体的转换表。
    /// </summary>
    private readonly ChineseConversionTable _toRegionalTraditional;

    /// <summary>
    /// 地区繁体到标准繁体的转换表。
    /// </summary>
    private readonly ChineseConversionTable _fromRegionalTraditional;

    /// <summary>
    /// 初始化一个 <see cref="ChineseRegionalConversionCatalog"/> 类型的实例。
    /// </summary>
    /// <param name="baseCatalog">基础简繁转换目录。</param>
    /// <param name="toRegionalTraditional">标准繁体到地区繁体的 UTF-8 gzip 词表流。</param>
    /// <param name="fromRegionalTraditional">地区繁体到标准繁体的 UTF-8 gzip 词表流。</param>
    internal ChineseRegionalConversionCatalog(ChineseConversionCatalog baseCatalog,
        Stream toRegionalTraditional, Stream fromRegionalTraditional)
    {
        if (toRegionalTraditional == null)
            throw new ArgumentNullException(nameof(toRegionalTraditional));
        if (fromRegionalTraditional == null)
            throw new ArgumentNullException(nameof(fromRegionalTraditional));
        if (!toRegionalTraditional.CanRead)
            throw new ArgumentException("地区正向词表流必须可读。", nameof(toRegionalTraditional));
        if (!fromRegionalTraditional.CanRead)
            throw new ArgumentException("地区反向词表流必须可读。", nameof(fromRegionalTraditional));

        _baseCatalog = baseCatalog;
        _toRegionalTraditional = ChineseConversionTable.Load(
            toRegionalTraditional, nameof(toRegionalTraditional), true);
        _fromRegionalTraditional = ChineseConversionTable.Load(
            fromRegionalTraditional, nameof(fromRegionalTraditional), true);
    }

    /// <summary>
    /// 将简体中文转换为地区繁体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 或空文本时保持原值。</returns>
    public string ToTraditional(string text) =>
        _toRegionalTraditional.Convert(_baseCatalog.ToTraditional(text));

    /// <summary>
    /// 将地区繁体中文转换为简体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 或空文本时保持原值。</returns>
    public string ToSimplified(string text) =>
        _baseCatalog.ToSimplified(_fromRegionalTraditional.Convert(text));
}
