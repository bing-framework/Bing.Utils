namespace Bing;

/// <summary>
/// 枚举分组 特性
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field)]
public class EnumGroupAttribute : Attribute
{
    /// <summary>
    /// 初始化一个<see cref="EnumGroupAttribute"/>类型的实例
    /// </summary>
    public EnumGroupAttribute() { }

    /// <summary>
    /// 初始化一个<see cref="EnumGroupAttribute"/>类型的实例
    /// </summary>
    /// <param name="title">标题</param>
    public EnumGroupAttribute(string title) => Title = title;

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }
}