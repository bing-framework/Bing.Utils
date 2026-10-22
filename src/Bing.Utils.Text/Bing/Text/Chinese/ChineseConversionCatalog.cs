using System;
using System.IO;

namespace Bing.Text.Chinese;

/// <summary>
/// 使用调用方提供的固定词表进行简繁中文转换。
/// </summary>
public sealed class ChineseConversionCatalog
{
    /// <summary>
    /// 保存简体到繁体的转换表。
    /// </summary>
    private readonly ChineseConversionTable _simplifiedToTraditional;

    /// <summary>
    /// 保存繁体到简体的转换表。
    /// </summary>
    private readonly ChineseConversionTable _traditionalToSimplified;

    /// <summary>
    /// 初始化一个 <see cref="ChineseConversionCatalog"/> 类型的实例。
    /// </summary>
    /// <param name="simplifiedToTraditional">从当前位置读取的简体到繁体 UTF-8 gzip 词表流。</param>
    /// <param name="traditionalToSimplified">从当前位置读取的繁体到简体 UTF-8 gzip 词表流。</param>
    /// <exception cref="ArgumentNullException">任一词表流为 null。</exception>
    /// <exception cref="ArgumentException">任一词表流不可读。</exception>
    /// <exception cref="InvalidDataException">词表不是有效的 gzip、UTF-8 或 key\tvalue 格式。</exception>
    public ChineseConversionCatalog(Stream simplifiedToTraditional, Stream traditionalToSimplified)
    {
        if (simplifiedToTraditional == null)
            throw new ArgumentNullException(nameof(simplifiedToTraditional));
        if (traditionalToSimplified == null)
            throw new ArgumentNullException(nameof(traditionalToSimplified));
        if (!simplifiedToTraditional.CanRead)
            throw new ArgumentException("简体到繁体词表流必须可读。", nameof(simplifiedToTraditional));
        if (!traditionalToSimplified.CanRead)
            throw new ArgumentException("繁体到简体词表流必须可读。", nameof(traditionalToSimplified));

        _simplifiedToTraditional = ChineseConversionTable.Load(
            simplifiedToTraditional, nameof(simplifiedToTraditional), true);
        _traditionalToSimplified = ChineseConversionTable.Load(
            traditionalToSimplified, nameof(traditionalToSimplified), true);
    }

    /// <summary>
    /// 将简体中文转换为繁体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 或空文本时保持原值。</returns>
    public string ToTraditional(string text) => _simplifiedToTraditional.Convert(text);

    /// <summary>
    /// 将繁体中文转换为简体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 或空文本时保持原值。</returns>
    public string ToSimplified(string text) => _traditionalToSimplified.Convert(text);

    /// <summary>
    /// 使用地区词表创建独立转换目录。
    /// </summary>
    /// <param name="toRegionalTraditional">标准繁体到地区繁体的 UTF-8 gzip 词表流。</param>
    /// <param name="fromRegionalTraditional">地区繁体到标准繁体的 UTF-8 gzip 词表流。</param>
    /// <returns>按基础转换和地区规则顺序处理的目录。</returns>
    /// <exception cref="ArgumentNullException">任一词表流为 null。</exception>
    /// <exception cref="ArgumentException">任一词表流不可读。</exception>
    /// <exception cref="InvalidDataException">词表不是有效的 gzip、UTF-8 或 key\tvalue 格式。</exception>
    /// <remarks>先完成基础简繁转换，再应用地区规则；反向转换按相反顺序处理。输入流保持开放，原目录不变。</remarks>
    public ChineseRegionalConversionCatalog WithRegionalRules(Stream toRegionalTraditional, Stream fromRegionalTraditional) =>
        new ChineseRegionalConversionCatalog(this, toRegionalTraditional, fromRegionalTraditional);
}
