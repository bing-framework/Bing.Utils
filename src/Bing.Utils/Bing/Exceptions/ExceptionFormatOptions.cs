namespace Bing.Exceptions;

/// <summary>
/// 配置异常诊断文本的遍历和输出预算。
/// </summary>
/// <remarks>预算按 UTF-16 字符计数，包含换行与截断标记。该上限约束输出，不限制异常属性 getter（含运行时 StackTrace 字符串生成）的总分配；隐藏堆栈不代表消息已脱敏。</remarks>
public sealed class ExceptionFormatOptions
{
    /// <summary>
    /// 获取或设置最大路径深度，根节点为 1，默认 64。
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// 获取或设置最多访问的不同异常节点数，默认 256；重复引用只输出标记，不占节点预算。
    /// </summary>
    public int MaxExceptionCount { get; set; } = 256;

    /// <summary>
    /// 获取或设置最大输出字符数，至少为 64，默认 32768。
    /// </summary>
    public int MaxOutputLength { get; set; } = 32768;

    /// <summary>
    /// 获取或设置最大缩进层级，每层两个空格，默认 8，允许为 0。
    /// </summary>
    public int MaxIndentLevel { get; set; } = 8;

    /// <summary>
    /// 获取或设置是否按原分支顺序展开聚合异常，默认 false。
    /// </summary>
    public bool IncludeAggregateExceptions { get; set; }

    /// <summary>
    /// 获取或设置是否隐藏所有节点的堆栈，默认 false。
    /// </summary>
    public bool HideStackTrace { get; set; }

    /// <summary>
    /// 读取并校验本次调用使用的独立配置快照。
    /// </summary>
    /// <returns>不会受后续外部修改影响的配置副本。</returns>
    internal ExceptionFormatOptions Snapshot()
    {
        var snapshot = new ExceptionFormatOptions
        {
            MaxDepth = MaxDepth,
            MaxExceptionCount = MaxExceptionCount,
            MaxOutputLength = MaxOutputLength,
            MaxIndentLevel = MaxIndentLevel,
            IncludeAggregateExceptions = IncludeAggregateExceptions,
            HideStackTrace = HideStackTrace
        };
        if (snapshot.MaxDepth < 1) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (snapshot.MaxExceptionCount < 1) throw new ArgumentOutOfRangeException(nameof(MaxExceptionCount));
        if (snapshot.MaxOutputLength < 64) throw new ArgumentOutOfRangeException(nameof(MaxOutputLength));
        if (snapshot.MaxIndentLevel < 0) throw new ArgumentOutOfRangeException(nameof(MaxIndentLevel));
        return snapshot;
    }
}
