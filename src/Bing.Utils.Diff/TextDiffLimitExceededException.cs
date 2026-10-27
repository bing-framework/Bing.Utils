namespace Bing.Text.Diff;

/// <summary>
/// 输入或计算超出配置边界，不产生部分比较结果。
/// </summary>
public sealed class TextDiffLimitExceededException : Exception
{
    /// <summary>
    /// UTF-16 代码单元数超限的稳定错误代码。
    /// </summary>
    public const string CharacterLimitCode = "diff.input_characters";
    /// <summary>
    /// 逻辑行数超限的稳定错误代码。
    /// </summary>
    public const string LineLimitCode = "diff.input_lines";
    /// <summary>
    /// 协作计算预算耗尽的稳定错误代码。
    /// </summary>
    public const string DurationLimitCode = "diff.duration";

    /// <summary>
    /// 创建边界错误。
    /// </summary>
    /// <param name="code">稳定错误代码。</param>
    /// <param name="message">中文错误说明。</param>
    internal TextDiffLimitExceededException(string code, string message) : base(message) => Code = code;

    /// <summary>
    /// 可用于程序分支的错误代码。
    /// </summary>
    public string Code { get; }
}
