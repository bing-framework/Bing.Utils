namespace Bing.Reflection;

/// <summary>
/// 类型访问器，一个高级的 TypeReflections 工具。
/// </summary>
public static partial class TypeVisit
{
#if !NETSTANDARD

    /// <summary>
    /// 使用给定的属性集合创建一个动态类型
    /// </summary>
    /// <param name="properties">属性字典</param>
    public static Type CreateDynamicType(IDictionary<string, Type> properties) => TypeFactory.CreateType(properties);

#endif
}