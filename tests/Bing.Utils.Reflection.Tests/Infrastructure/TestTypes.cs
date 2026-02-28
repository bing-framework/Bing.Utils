using System.ComponentModel;

namespace Bing.Reflection.Tests;

/// <summary>
/// 测试用实体：包含公开字段、读写属性、只读属性、无参/有参构造函数和多种方法
/// </summary>
public class SampleEntity
{
    /// <summary>公开字段</summary>
    public string PublicField = "publicFieldValue";

    /// <summary>读写属性</summary>
    public string ReadWriteProp { get; set; } = string.Empty;

    /// <summary>只读属性（仅 getter）</summary>
    public int ReadOnlyProp { get; }

    /// <summary>表达式体只读属性</summary>
    public int ExpressionProp => 42;

    /// <summary>无参构造函数</summary>
    public SampleEntity() { }

    /// <summary>单字符串参数构造函数</summary>
    public SampleEntity(string name) { ReadWriteProp = name; }

    /// <summary>双参数构造函数</summary>
    public SampleEntity(string name, int value)
    {
        ReadWriteProp = name;
        ReadOnlyProp = value;
    }

    /// <summary>无参方法</summary>
    public string Greet() => $"Hello, {ReadWriteProp}";

    /// <summary>带参方法</summary>
    public string Greet(string prefix) => $"{prefix}, {ReadWriteProp}";

    /// <summary>泛型方法</summary>
    public T Echo<T>(T value) => value;
}

/// <summary>
/// 带 Description / DisplayName Attribute 的测试类
/// </summary>
[Description("Sample class description")]
public class AnnotatedEntity
{
    /// <summary>有 Description 的属性</summary>
    [Description("Name property description")]
    public string Name { get; set; } = string.Empty;

    /// <summary>无 Description 的属性</summary>
    public string NoDescription { get; set; } = string.Empty;
}

/// <summary>
/// 泛型测试类
/// </summary>
public class SampleGeneric<T>
{
    public T Value { get; }
    public SampleGeneric(T value) => Value = value;
}

/// <summary>
/// 无公开构造函数的类（仅私有构造）
/// </summary>
public class NoPublicCtorEntity
{
    private NoPublicCtorEntity() { }
    public static NoPublicCtorEntity Create() => new();
    public string Data => "created";
}

/// <summary>
/// 抽象类（不可直接实例化）
/// </summary>
public abstract class AbstractEntity
{
    public abstract string GetName();
}

/// <summary>
/// 虚方法测试类
/// </summary>
public class VirtualPropEntity
{
    public virtual string VirtualProp { get; set; } = string.Empty;
    public string NonVirtualProp { get; set; } = string.Empty;
}
