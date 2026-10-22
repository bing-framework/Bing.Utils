using System;
using System.IO;
using System.Reflection;

namespace Bing.Text.Chinese;

/// <summary>
/// 使用固定词表转换简体与繁体中文。
/// </summary>
public static class ChineseConverter
{
    /// <summary>
    /// 首次转换为繁体时加载的词表。
    /// </summary>
    private static readonly Lazy<ChineseConversionTable> SimplifiedToTraditional =
        new Lazy<ChineseConversionTable>(() => Load("s2t.gz"));

    /// <summary>
    /// 首次转换为简体时加载的词表。
    /// </summary>
    private static readonly Lazy<ChineseConversionTable> TraditionalToSimplified =
        new Lazy<ChineseConversionTable>(() => Load("t2s.gz"));

    /// <summary>
    /// 将简体中文转换为繁体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 时返回 null。</returns>
    /// <remarks>优先匹配最长词组，再使用单字映射。采用 OpenCC 标准词表的首个候选，不包含区域变体规则。</remarks>
    public static string ToTraditional(string text) => SimplifiedToTraditional.Value.Convert(text);

    /// <summary>
    /// 将繁体中文转换为简体中文。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <returns>转换结果；输入为 null 时返回 null。</returns>
    /// <remarks>优先匹配最长词组，再使用单字映射。采用 OpenCC 标准词表的首个候选，不包含区域变体规则。</remarks>
    public static string ToSimplified(string text) => TraditionalToSimplified.Value.Convert(text);

    /// <summary>
    /// 从内置压缩资源构建词组前缀树。
    /// </summary>
    /// <param name="name">资源文件名。</param>
    /// <returns>构建完成的转换词表。</returns>
    private static ChineseConversionTable Load(string name)
    {
        var resource = "Bing.Text.Chinese.Data." + name;
        var stream = typeof(ChineseConverter).GetTypeInfo().Assembly.GetManifestResourceStream(resource);
        if (stream == null)
            throw new InvalidOperationException("Missing Chinese conversion resource: " + resource);

        using (stream)
        return ChineseConversionTable.Load(stream, name, false);
    }
}
