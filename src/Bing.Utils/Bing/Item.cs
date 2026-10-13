using System.Runtime.Serialization;

namespace Bing;

/// <summary>
/// 列表项
/// </summary>
[Serializable]
[DataContract]
public class Item : IComparable<Item>
{
    /// <summary>
    /// 初始化一个 <see cref="Item"/> 类型的实例。
    /// </summary>
    public Item() { }

    /// <summary>
    /// 初始化一个 <see cref="Item"/> 类型的实例。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="value">值</param>
    /// <param name="sortId">排序号</param>
    /// <param name="group">组</param>
    /// <param name="disabled">true 表示禁用，false 表示启用，<see langword="null"/> 表示未指定。</param>
    public Item(string text, object value, int? sortId = null, string group = null, bool? disabled = null)
    {
        Text = text;
        Value = value;
        SortId = sortId;
        Group = group;
        Disabled = disabled;
    }

    /// <summary>
    /// 获取或设置列表项显示的文本。
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string Text { get; set; }

    /// <summary>
    /// 获取或设置列表项关联的值。
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public object Value { get; set; }

    /// <summary>
    /// 获取或设置列表项的排序号。
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public int? SortId { get; set; }

    /// <summary>
    /// 获取或设置列表项所属的组。
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string Group { get; set; }

    /// <summary>
    /// 禁用
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public bool? Disabled { get; set; }

    /// <summary>
    /// 比较
    /// </summary>
    /// <param name="other">其他列表项</param>
    public int CompareTo(Item other) => string.Compare(Text, other.Text, StringComparison.CurrentCulture);
}
