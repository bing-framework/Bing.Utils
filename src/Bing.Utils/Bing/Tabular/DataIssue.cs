using Bing.Diagnostics;
using Bing.Operations;

namespace Bing.Tabular;

/// <summary>
/// 表示表格映射过程中的问题级别。
/// </summary>
public enum DataIssueSeverity
{
    /// <summary>提示信息。</summary>
    Info,
    /// <summary>可继续处理的警告。</summary>
    Warning,
    /// <summary>导致当前行映射失败的错误。</summary>
    Error
}

/// <summary>
/// 表示一个带有稳定错误码和源位置的表格数据问题。
/// </summary>
public sealed class DataIssue
{
    /// <summary>
    /// 初始化数据问题。
    /// </summary>
    public DataIssue(string code, DataIssueSeverity severity, OperationStage stage, string message,
        DataLocation location = null, string sourceColumnName = null, int? sourceOrdinal = null,
        string targetMember = null, string exceptionType = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("问题编码不能为空。", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("问题消息不能为空。", nameof(message));
        if (!Enum.IsDefined(typeof(DataIssueSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        if (!Enum.IsDefined(typeof(OperationStage), stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal));

        Code = code;
        Severity = severity;
        Stage = stage;
        Message = message;
        Location = location;
        SourceColumnName = sourceColumnName;
        SourceOrdinal = sourceOrdinal;
        TargetMember = targetMember;
        ExceptionType = exceptionType;
    }

    /// <summary>获取稳定问题编码。</summary>
    public string Code { get; }
    /// <summary>获取问题级别。</summary>
    public DataIssueSeverity Severity { get; }
    /// <summary>获取发生问题的操作阶段。</summary>
    public OperationStage Stage { get; }
    /// <summary>获取面向调用方的消息。</summary>
    public string Message { get; }
    /// <summary>获取源数据位置。</summary>
    public DataLocation Location { get; }
    /// <summary>获取源字段名称。</summary>
    public string SourceColumnName { get; }
    /// <summary>获取零基源字段序号；按序号配置的必需列缺失时，保留配置中的期望序号。</summary>
    public int? SourceOrdinal { get; }
    /// <summary>获取目标成员名称。</summary>
    public string TargetMember { get; }
    /// <summary>获取底层异常类型名称。</summary>
    public string ExceptionType { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}
