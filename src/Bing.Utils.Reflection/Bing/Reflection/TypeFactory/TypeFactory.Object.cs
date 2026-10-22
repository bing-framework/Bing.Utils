using Bing.Dynamic;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

#if !NETSTANDARD

// 类型工厂 - 创建对象
internal static partial class TypeFactory
{
    /// <summary>
    /// 创建一个动态对象实例
    /// </summary>
    /// <param name="values">属性值字典</param>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("动态对象通过 Reflection.Emit 创建，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("动态对象创建依赖运行时生成的构造函数和属性元数据。")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "目标类型由 TypeFactory 动态生成，并在激活前显式生成公共无参构造函数。")]
#endif
    public static object CreateObject(IDictionary<string, object> values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));
        // 有界单次枚举，避免依赖自定义字典的 Count 或 CopyTo 实现。
        var snapshot = new List<KeyValuePair<string, object>>();
        foreach (var item in values)
        {
            if (snapshot.Count >= MaxProperties)
                throw new ArgumentException($"动态对象最多支持 {MaxProperties} 个属性。", nameof(values));
            snapshot.Add(item);
        }

        var properties = snapshot.ToDictionary(item => item.Key, item => item.Value?.GetType() ?? typeof(object));
        var type = CreateType(properties);
        var @object = (DynamicBase)Activator.CreateInstance(type)!;
        foreach (var item in snapshot)
            @object.SetPropertyValue(item.Key, item.Value);
        return @object;
    }
}

#endif
