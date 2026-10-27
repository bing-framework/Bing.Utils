namespace Bing.Diagnostics;

/// <summary>
/// 表示输入数据中的一个格式相关位置。
/// </summary>
public abstract class DataLocation
{
    /// <summary>
    /// 获取位置类型标识，用于日志和序列化分支判断。
    /// </summary>
    public abstract string Kind { get; }

    /// <summary>
    /// 返回适合诊断消息的文字位置。
    /// </summary>
    public abstract override string ToString();
}
