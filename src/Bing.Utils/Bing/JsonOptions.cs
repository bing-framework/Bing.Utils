namespace Bing;

/// <summary>
/// Json配置
/// </summary>
public class JsonOptions
{
    /// <summary>
    /// 初始化一个 <see cref="JsonOptions"/> 类型的实例。
    /// </summary>
    public JsonOptions()
    {
        IgnoreNullValues = true;
        IgnoreCase = true;
        IgnoreInterface = true;
    }

    /// <summary>
    /// 获取或设置是否移除双引号,默认值: false
    /// </summary>
    public bool RemoveQuotationMarks { get; set; }

    /// <summary>
    /// 获取或设置是否将双引号转成单引号,默认值: false
    /// </summary>
    public bool ToSingleQuotes { get; set; }

    /// <summary>
    /// 获取或设置是否忽略null值,默认值: true
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// 获取或设置转换为 JSON 时属性名是否使用驼峰形式，默认值为 false。
    /// </summary>
    public bool ToCamelCase { get; set; }

    /// <summary>
    /// 获取或设置反序列化时是否忽略大小写,默认值: true
    /// </summary>
    public bool IgnoreCase { get; set; }

    /// <summary>
    /// 获取或设置序列化时是否按对象的运行时类型处理接口实例，默认值为 true。
    /// </summary>
    public bool IgnoreInterface { get; set; }
}
