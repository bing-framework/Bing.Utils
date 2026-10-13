namespace ReflectionTrimProbe.Models;

/// <summary>
/// 由独立 linker root 保留的跨程序集反射模型。
/// </summary>
public sealed class UnrootedModel
{
    private int _reflectedValue = 41;

    /// <summary>
    /// 创建模型实例；其私有属性仅通过运行时元数据访问。
    /// </summary>
    public UnrootedModel()
    {
    }

    /// <summary>
    /// 仅由非公开字符串路径访问，正常代码不直接引用该属性。
    /// </summary>
    private int ReflectedValue
    {
        get => _reflectedValue;
        set => _reflectedValue = value;
    }
}
