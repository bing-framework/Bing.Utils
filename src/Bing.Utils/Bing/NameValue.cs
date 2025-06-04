namespace Bing;

/// <summary>
/// 表示 名称-值 对的数据结构，使用 <see cref="string"/> 类型作为值类型。
/// 常用于下拉列表、键值对集合、选项列表等场景。
/// </summary>
[Serializable]
public class NameValue : NameValue<string>
{
    /// <summary>
    /// 初始化一个<see cref="NameValue"/>类型的实例
    /// </summary>
    public NameValue() { }

    /// <summary>
    /// 初始化一个<see cref="NameValue"/>类型的实例
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="value">值</param>
    public NameValue(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// 表示 名称-值 对的泛型数据结构，支持任意类型的值。
/// 常用于需要将显示名称与任意类型值关联的场景。
/// </summary>
/// <typeparam name="T">值的类型，可以是任何类型</typeparam>
[Serializable]
public class NameValue<T>
{
    /// <summary>
    /// 初始化一个<see cref="NameValue{T}"/>类型的实例
    /// </summary>
    public NameValue() { }

    /// <summary>
    /// 初始化一个<see cref="NameValue{T}"/>类型的实例
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="value">值</param>
    public NameValue(string name, T value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// 获取或设置名称，通常表示显示给用户看的文本
    /// </summary>
    /// <remarks>
    /// 在用户界面元素中（如下拉列表），此属性通常用作显示标签。
    /// </remarks>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 获取或设置值，通常表示程序内部使用的数据
    /// </summary>
    /// <remarks>
    /// 在用户界面元素中（如下拉列表），此属性通常用作选中后的实际值，
    /// 可用于数据绑定和后续的数据处理操作。
    /// </remarks>
    public T Value { get; set; } = default!;
}