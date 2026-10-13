namespace ReflectionTrimProbe.UnrootedModels;

/// <summary>
/// 仅保留字段的 linker 根描述夹具。
/// </summary>
public sealed class FieldsOnlyRootModel
{
    public int PreservedField = 1;
}

/// <summary>
/// 仅保留属性的 linker 根描述夹具。
/// </summary>
public sealed class PropertiesOnlyRootModel
{
    public int PreservedProperty { get; set; }
}

/// <summary>
/// 仅保留方法的 linker 根描述夹具。
/// </summary>
public sealed class MethodsOnlyRootModel
{
    public int PreservedMethod(int value) => value + 1;
}

/// <summary>
/// 不保留成员的 linker 根描述夹具，用于验证类型存在但成员元数据可被裁剪。
/// </summary>
public sealed class NothingRootModel
{
    public int TrimmedField;

    public int TrimmedProperty { get; set; }

    public int TrimmedMethod(int value) => value + 1;
}
