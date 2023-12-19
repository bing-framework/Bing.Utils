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
    public static object CreateObject(IDictionary<string, object> values)
    {
        var properties = values.ToDictionary(_ => _.Key, _ => _.Value.GetType());
        var type = CreateType(properties);
        var @object = (DynamicBase)Activator.CreateInstance(type);
        foreach (var item in values) 
            @object?.SetPropertyValue(item.Key, item.Value);
        return @object;
    }
}

#endif