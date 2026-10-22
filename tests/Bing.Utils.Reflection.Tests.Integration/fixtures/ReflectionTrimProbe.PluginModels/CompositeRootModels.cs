namespace ReflectionTrimProbe.PluginModels;

/// <summary>
/// 第二个外部程序集中的字段根描述夹具。
/// </summary>
public sealed class CompositeFieldsRootModel
{
    public int PreservedField = 1;
}

/// <summary>
/// 第二个外部程序集中的显式属性根描述夹具。
/// </summary>
public sealed class CompositePropertiesRootModel
{
    public int PreservedProperty { get; set; }
}

/// <summary>
/// 第二个外部程序集中的方法根描述夹具。
/// </summary>
public sealed class CompositeMethodsRootModel
{
    public int PreservedMethod(int value) => value + 1;
}

/// <summary>
/// 第二个外部程序集中的空根描述夹具。
/// </summary>
public sealed class CompositeNothingRootModel
{
    public int TrimmedField;

    public int TrimmedProperty { get; set; }

    public int TrimmedMethod(int value) => value + 1;
}

/// <summary>
/// 第二个外部程序集中的泛型定义根描述夹具。
/// </summary>
public sealed class CompositeGenericRootModel<T>
{
    public T PreservedField = default!;

    public T PreservedProperty { get; set; } = default!;

    public T PreservedMethod(T value) => value;
}

/// <summary>
/// 第二个外部程序集中的受约束泛型根描述夹具。
/// </summary>
public sealed class CompositeConstrainedGenericRootModel<T>
    where T : class, new()
{
    public T PreservedProperty { get; set; } = new();

    public T PreservedMethod(T value) => value;

    public int TrimmedMethod(int value) => value + 1;
}

/// <summary>
/// 第二个外部程序集中的嵌套类型根描述夹具。
/// </summary>
public sealed class CompositeNestedRootModel
{
    public sealed class PreservedNested
    {
        public int PreservedField = 1;
    }
}

/// <summary>
/// 供第三个外部程序集中的嵌套 root 继承的跨程序集基类。
/// </summary>
public class CompositeCrossAssemblyBase
{
    public int PreservedBaseField = 1;

    public int TrimmedBaseField = 1;
}

/// <summary>
/// 供第三个外部程序集中的嵌套 root 持有的跨程序集负载类型。
/// </summary>
public sealed class CompositeCrossAssemblyPayload
{
    public int PreservedPayloadField = 1;

    public int TrimmedPayloadField = 1;
}

/// <summary>
/// 第二个外部程序集中的构造函数和重载签名根描述夹具。
/// </summary>
public sealed class CompositeSignatureRootModel
{
    public CompositeSignatureRootModel()
    {
    }

    public CompositeSignatureRootModel(int value)
    {
        Value = value;
    }

    public CompositeSignatureRootModel(string value)
    {
        Value = value.Length;
    }

    public int Value { get; }

    public int PreservedMethod(int value) => value + 1;

    public string PreservedMethod(string value) => value;
}
