namespace Bing.Operations;

/// <summary>
/// 标识流式操作当前处理阶段。
/// </summary>
public enum OperationStage
{
    /// <summary>
    /// 从输入源读取数据。
    /// </summary>
    Read,

    /// <summary>
    /// 转换或处理已读取的数据。
    /// </summary>
    Transform,

    /// <summary>
    /// 将处理结果写入目标。
    /// </summary>
    Write,

    /// <summary>
    /// 提交已写入的结果。
    /// </summary>
    Commit
}
