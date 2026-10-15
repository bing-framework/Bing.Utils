using System.ComponentModel;

namespace Bing.Reflection.Tests;

/// <summary>
/// 用于验证成员发现、构造函数调用和方法调用的反射样例。
/// </summary>
public class SampleEntity
{
    /// <summary>
    /// 用于验证公开字段反射的文本，初始值为 <c>publicFieldValue</c>。
    /// </summary>
    public string PublicField = "publicFieldValue";

    /// <summary>
    /// 获取或设置用于构造函数赋值及方法调用的样例名称。
    /// </summary>
    public string ReadWriteProp { get; set; } = string.Empty;

    /// <summary>
    /// 获取由双参数构造函数设置的样例数值。
    /// </summary>
    public int ReadOnlyProp { get; }

    /// <summary>
    /// 获取用于验证表达式体只读属性的固定数值 <c>42</c>。
    /// </summary>
    public int ExpressionProp => 42;

    /// <summary>
    /// 初始化一个 <see cref="SampleEntity"/> 类型的实例。
    /// </summary>
    public SampleEntity() { }

    /// <summary>
    /// 初始化一个 <see cref="SampleEntity"/> 类型的实例。
    /// </summary>
    /// <param name="name">用于初始化读写属性的样例名称。</param>
    public SampleEntity(string name) { ReadWriteProp = name; }

    /// <summary>
    /// 初始化一个 <see cref="SampleEntity"/> 类型的实例。
    /// </summary>
    /// <param name="name">用于初始化读写属性的样例名称。</param>
    /// <param name="value">用于初始化只读属性的样例数值。</param>
    public SampleEntity(string name, int value)
    {
        ReadWriteProp = name;
        ReadOnlyProp = value;
    }

    /// <summary>
    /// 生成包含样例名称的问候语。
    /// </summary>
    /// <returns>使用 <c>Hello</c> 前缀和样例名称组成的问候语。</returns>
    public string Greet() => $"Hello, {ReadWriteProp}";

    /// <summary>
    /// 生成包含样例名称的问候语。
    /// </summary>
    /// <param name="prefix">问候语的前缀。</param>
    /// <returns>使用指定前缀和样例名称组成的问候语。</returns>
    public string Greet(string prefix) => $"{prefix}, {ReadWriteProp}";

    /// <summary>
    /// 原样返回传入的泛型样例值。
    /// </summary>
    /// <typeparam name="T">样例值的类型。</typeparam>
    /// <param name="value">需要原样返回的样例值。</param>
    /// <returns>传入的样例值。</returns>
    public T Echo<T>(T value) => value;
}

/// <summary>
/// 用于验证描述特性读取及无特性回退行为的反射样例。
/// </summary>
[Description("Sample class description")]
public class AnnotatedEntity
{
    /// <summary>
    /// 获取或设置带有描述特性的样例名称。
    /// </summary>
    [Description("Name property description")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置未声明描述特性的样例文本。
    /// </summary>
    public string NoDescription { get; set; } = string.Empty;
}

/// <summary>
/// 用于验证泛型类型构造及实例创建的样例。
/// </summary>
/// <typeparam name="T">实例保存的样例值类型。</typeparam>
public class SampleGeneric<T>
{
    /// <summary>
    /// 获取构造函数传入的泛型样例值。
    /// </summary>
    public T Value { get; }
    /// <summary>
    /// 初始化一个 <see cref="SampleGeneric{T}"/> 类型的实例。
    /// </summary>
    /// <param name="value">实例保存的样例值。</param>
    public SampleGeneric(T value) => Value = value;
}

/// <summary>
/// 用于验证无公开构造函数时实例创建行为的样例。
/// </summary>
public class NoPublicCtorEntity
{
    /// <summary>
    /// 初始化一个 <see cref="NoPublicCtorEntity"/> 类型的实例。
    /// </summary>
    private NoPublicCtorEntity() { }
    /// <summary>
    /// 通过私有构造函数创建样例实例。
    /// </summary>
    /// <returns>新创建的样例实例。</returns>
    public static NoPublicCtorEntity Create() => new();
    /// <summary>
    /// 获取用于标识样例实例的固定文本 <c>created</c>。
    /// </summary>
    public string Data => "created";
}

/// <summary>
/// 用于验证抽象类型及抽象方法反射的样例。
/// </summary>
public abstract class AbstractEntity
{
    /// <summary>
    /// 返回样例名称。
    /// </summary>
    /// <returns>样例名称。</returns>
    public abstract string GetName();
}

/// <summary>
/// 用于验证虚属性反射的样例。
/// </summary>
public class VirtualPropEntity
{
    /// <summary>
    /// 获取或设置用于验证虚属性反射的文本。
    /// </summary>
    public virtual string VirtualProp { get; set; } = string.Empty;
    /// <summary>
    /// 获取或设置用于与虚属性比较的非虚属性文本。
    /// </summary>
    public string NonVirtualProp { get; set; } = string.Empty;
}
