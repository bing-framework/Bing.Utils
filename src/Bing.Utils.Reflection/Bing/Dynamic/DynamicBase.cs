using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bing.Reflection;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Dynamic;

/// <summary>
/// 动态类型抽象基类
/// </summary>
public abstract class DynamicBase
{
    internal const int PropertyCacheCapacity = 1024;

    private sealed class PropertyLookup
    {
        internal readonly ConcurrentDictionary<string, Lazy<PropertyInfo>> Properties = new();
        internal readonly object AdmissionGate = new();
    }

    private static readonly ConditionalWeakTable<Type, PropertyLookup> _propertyCache = new();
    private static readonly ConditionalWeakTable<PropertyInfo, StrongBox<bool>> _initOnlyProperties = new();
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    private readonly Type _type;

    /// <summary>初始化动态类型实例。</summary>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2074", Justification = "动态类型由 Reflection.Emit 先生成公开属性，再进入基类构造函数；该运行时关系无法由静态分析表达。")]
#endif
    protected DynamicBase() => _type = GetType();

    /// <summary>读取指定名称的公开实例属性。</summary>
    public object GetPropertyValue(string name)
    {
        var property = GetProperty(name);
        if (property.GetMethod?.IsPublic != true)
            throw new InvalidOperationException($"属性 '{name}' 不可读取。");
        return property.GetValue(this);
    }

    /// <summary>写入指定名称的公开实例属性；初始化专用属性不可在此入口写入。</summary>
    public void SetPropertyValue(string name, object value)
    {
        var property = GetProperty(name);
        if (property.SetMethod?.IsPublic != true)
            throw new InvalidOperationException($"属性 '{name}' 不可写入。");
        if (_initOnlyProperties.GetValue(property, key => new StrongBox<bool>(
            key.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit"))).Value)
            throw new InvalidOperationException($"属性 '{name}' 只能在初始化时设置。");
        property.SetValue(this, value);
    }

    private PropertyInfo GetProperty(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        if (name.Length == 0)
            throw new ArgumentException("属性名称不能为空。", nameof(name));

        // 长名称保持可查询，但不作为缓存键持有，兼容合法的长元数据名称。
        if (name.Length > 1024)
            return ValidateProperty(_type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance), name);
        if (ReflectionPackageCacheScope.IsCollectible(_type))
            return ValidateProperty(_type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance), name);
        var lookup = _propertyCache.GetValue(_type, _ => new PropertyLookup());
        if (lookup.Properties.TryGetValue(name, out var existing))
        {
            ReflectionCacheMetrics.Record(existing);
            return ValidateProperty(existing.Value, name);
        }

        Lazy<PropertyInfo> lazy = null;
        var bypass = false;
        var type = _type;
        lock (lookup.AdmissionGate)
        {
            if (lookup.Properties.TryGetValue(name, out existing))
            {
                ReflectionCacheMetrics.Record(existing);
                lazy = existing;
            }
            else if (lookup.Properties.Count >= PropertyCacheCapacity)
            {
                ReflectionCacheMetrics.RecordBypass();
                bypass = true;
            }
            else
            {
                lazy = new Lazy<PropertyInfo>(() =>
                    type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                lookup.Properties.TryAdd(name, lazy);
                ReflectionCacheMetrics.RecordMiss();
            }
        }

        var property = bypass
            ? _type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            : lazy.Value;
        return ValidateProperty(property, name);
    }

    private PropertyInfo ValidateProperty(PropertyInfo property, string name)
    {
        if (property is null)
            throw new MissingMemberException(_type.FullName, name);
        if (property.GetIndexParameters().Length != 0)
            throw new ArgumentException($"索引属性 '{name}' 需要索引参数。", nameof(name));
        return property;
    }
}
