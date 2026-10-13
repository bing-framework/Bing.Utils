#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

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
    /// <param name="properties">属性字典；值为 null 的属性类型按 object 处理。</param>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("动态类型使用 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("动态类型创建依赖 DynamicBase 及其运行时属性元数据，裁剪时必须显式保留相关成员。")]
#endif
    public static Type CreateDynamicType(IDictionary<string, Type> properties) => TypeFactory.CreateType(properties);

#endif
}
