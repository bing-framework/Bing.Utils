namespace Bing.Text.Diff;

/// <summary>
/// 行级比较选项；每次调用会在开始时复制这些设置。
/// </summary>
public sealed class TextDiffOptions
{
    /// <summary>
    /// 每份输入的最大逻辑行数，默认 200000，必须为正数。
    /// </summary>
    public int MaxLinesPerInput { get; set; } = 200_000;

    /// <summary>
    /// 每份输入的最大 UTF-16 代码单元数，默认 16000000，必须为正数。
    /// </summary>
    public int MaxCharsPerInput { get; set; } = 16_000_000;

    /// <summary>
    /// 包含扫描、计算与结果构建的协作时间预算，默认 30 秒，必须为有限正值。
    /// </summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 是否使用序号忽略大小写比较；默认 false，输出始终保留原文。
    /// </summary>
    public bool IgnoreCase { get; set; }

    /// <summary>
    /// 是否忽略每行首尾的 Unicode 空白；默认 false，行内空白仍参与比较。
    /// </summary>
    public bool IgnoreLeadingTrailingWhitespace { get; set; }
}
