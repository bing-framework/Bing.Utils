namespace Bing.Reflection;

/// <summary>
/// 成员路径访问选项。
/// </summary>
public sealed class MemberPathOptions
{
    /// <summary>
    /// 获取或设置成员名称比较是否忽略大小写，默认值为 false。
    /// </summary>
    public bool IgnoreCase { get; set; }

    /// <summary>
    /// 获取或设置是否允许访问实例字段，默认值为 true。
    /// </summary>
    public bool IncludeFields { get; set; } = true;

    /// <summary>
    /// 获取或设置是否允许访问非公共实例成员，默认值为 false。
    /// </summary>
    public bool IncludeNonPublic { get; set; }

    /// <summary>
    /// 获取或设置写入成员前是否允许转换值，默认值为 true。
    /// </summary>
    public bool ConvertAssignedValue { get; set; } = true;
}
