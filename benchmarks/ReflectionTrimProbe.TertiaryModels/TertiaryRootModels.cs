namespace ReflectionTrimProbe.TertiaryModels;

using ReflectionTrimProbe.PluginModels;

/// <summary>
/// 第三个外部程序集中的字段根描述夹具。
/// </summary>
public sealed class TertiaryFieldsRootModel
{
    public int PreservedField = 1;
}

/// <summary>
/// 第三个外部程序集中的显式属性根描述夹具。
/// </summary>
public sealed class TertiaryPropertiesRootModel
{
    public int PreservedProperty { get; set; }
}

/// <summary>
/// 第三个外部程序集中的空根描述夹具。
/// </summary>
public sealed class TertiaryNothingRootModel
{
    public int TrimmedField;

    public int TrimmedProperty { get; set; }

    public int TrimmedMethod(int value) => value + 1;
}

/// <summary>
/// 第三个外部程序集中的跨程序集嵌套 root 夹具。
/// </summary>
public sealed class CrossAssemblyNestedRootModel
{
    /// <summary>
    /// 继承第二程序集基类并持有第二程序集负载类型的嵌套类型。
    /// </summary>
    public sealed class PreservedNested : CompositeCrossAssemblyBase
    {
        public int PreservedNestedField = 1;

        public CompositeCrossAssemblyPayload PreservedPayload = new();

        public int TrimmedNestedField = 1;
    }

    /// <summary>
    /// 用于确认 nothing 根不会被跨程序集引用连带保留成员的嵌套类型。
    /// </summary>
    public sealed class TrimmedNested
    {
        public int TrimmedField = 1;
    }
}
