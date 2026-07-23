namespace Bing.Helpers;

/// <summary>
/// 字典映射选项。
/// </summary>
public sealed class DictionaryMapOptions
{
    /// <summary>
    /// 获取或设置是否忽略字典键和目标属性名称的大小写，默认值为 true。
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// 获取或设置是否忽略未匹配到目标属性的字典键，默认值为 true。
    /// </summary>
    public bool IgnoreUnknownKeys { get; set; } = true;

    /// <summary>
    /// 获取或设置是否忽略值转换失败的项，默认值为 false。
    /// </summary>
    public bool IgnoreConversionErrors { get; set; }

    /// <summary>
    /// 获取或设置字典键到目标属性名称的显式映射表。
    /// </summary>
    /// <remarks>
    /// 映射方向固定为“字典键 -&gt; 目标属性名”。
    /// </remarks>
    public IReadOnlyDictionary<string, string> PropertyMappings { get; set; }
}

/// <summary>
/// 对象成员映射选项。
/// </summary>
public sealed class MapOptions
{
    /// <summary>
    /// 获取或设置是否跳过值为 null 的源成员，默认值为 false。
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// 获取或设置是否将公共实例字段纳入映射，默认值为 false。
    /// </summary>
    public bool IncludeFields { get; set; }

    /// <summary>
    /// 获取或设置成员名称比较是否忽略大小写，默认值为 true。
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// 获取或设置是否在成员类型不一致时尝试转换值，默认值为 true。
    /// </summary>
    public bool ConvertValues { get; set; } = true;

    /// <summary>
    /// 获取或设置是否忽略成员值转换失败，默认值为 false。
    /// </summary>
    /// <remarks>
    /// 为 false 时，转换失败会抛出包含源成员和目标成员上下文的异常；为 true 时跳过失败成员并继续映射。
    /// </remarks>
    public bool IgnoreConversionErrors { get; set; }

    /// <summary>
    /// 获取或设置允许映射的成员名称集合。
    /// </summary>
    /// <remarks>
    /// 集合不为空时，仅映射包含在该集合中的成员。
    /// </remarks>
    public ISet<string> IncludedMembers { get; set; }

    /// <summary>
    /// 获取或设置禁止映射的成员名称集合。
    /// </summary>
    /// <remarks>
    /// 排除集合优先于包含集合。
    /// </remarks>
    public ISet<string> ExcludedMembers { get; set; }
}
