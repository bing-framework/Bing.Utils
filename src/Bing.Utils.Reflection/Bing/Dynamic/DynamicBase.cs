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
    private const int SmallInstancePropertyCacheCapacity = 8;

    private sealed class PropertyLookup
    {
        internal readonly ConcurrentDictionary<string, Lazy<PropertyInfo>> Properties = new();
        internal readonly object AdmissionGate = new();
    }

    // 首项固定在容器中，额外属性先进入短链；只有链满后才创建字典。
    private sealed class InstancePropertyEntry
    {
        internal readonly string Name;
        internal readonly PropertyInfo Property;
        internal readonly InstancePropertyEntry Next;
        internal readonly int Count;

        internal InstancePropertyEntry(string name, PropertyInfo property, InstancePropertyEntry next)
        {
            Name = name;
            Property = property;
            Next = next;
            Count = (next?.Count ?? 0) + 1;
        }
    }

    private sealed class InstancePropertyLookup
    {
        internal readonly string FirstName;
        internal readonly PropertyInfo FirstProperty;
        internal InstancePropertyEntry AdditionalHead;
        internal PropertyLookup AdditionalProperties;

        internal InstancePropertyLookup(string name, PropertyInfo property)
        {
            FirstName = name;
            FirstProperty = property;
        }
    }

    private static readonly ConditionalWeakTable<Type, PropertyLookup> _propertyCache = new();
    private static readonly ConditionalWeakTable<PropertyInfo, StrongBox<bool>> _initOnlyProperties = new();
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    private readonly Type _type;
    private readonly bool _useInstanceCache;
    // 可回收类型的元数据只由实例持有，实例释放后不会阻止程序集卸载。
    private InstancePropertyLookup _instancePropertyCache;

    /// <summary>初始化动态类型实例。</summary>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2074", Justification = "动态类型由 Reflection.Emit 先生成公开属性，再进入基类构造函数；该运行时关系无法由静态分析表达。")]
#endif
    protected DynamicBase()
    {
        _type = GetType();
        _useInstanceCache = ReflectionPackageCacheScope.IsCollectible(_type);
    }

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
        if (_useInstanceCache)
            return GetCollectibleProperty(name);
        return GetCachedProperty(_propertyCache.GetValue(_type, static _ => new PropertyLookup()), name, PropertyCacheCapacity);
    }

    private PropertyInfo GetCollectibleProperty(string name)
    {
        var lookup = System.Threading.Volatile.Read(ref _instancePropertyCache);
        var property = default(PropertyInfo);
        var hasResolvedProperty = false;
        if (lookup == null)
        {
            property = ResolveProperty(name);
            hasResolvedProperty = true;
            var candidate = new InstancePropertyLookup(name, property);
            lookup = System.Threading.Interlocked.CompareExchange(ref _instancePropertyCache, candidate, null);
            if (lookup == null)
            {
                ReflectionCacheMetrics.RecordMiss();
                return ValidateProperty(property, name);
            }
        }

        if (TryGetInstanceProperty(lookup, name, out var cachedProperty))
        {
            ReflectionCacheMetrics.RecordHit();
            return ValidateProperty(cachedProperty, name);
        }

        return GetCollectiblePropertyMiss(lookup, name, property, hasResolvedProperty);
    }

    private PropertyInfo GetCachedProperty(PropertyLookup lookup, string name, int capacity)
    {
        if (lookup.Properties.TryGetValue(name, out var existing))
        {
            ReflectionCacheMetrics.Record(existing);
            return ValidateProperty(existing.Value, name);
        }

        return GetCachedPropertyMiss(lookup, name, capacity);
    }

    private PropertyInfo GetCollectiblePropertyMiss(InstancePropertyLookup lookup, string name,
        PropertyInfo property, bool hasResolvedProperty)
    {
        while (true)
        {
            var head = System.Threading.Volatile.Read(ref lookup.AdditionalHead);
            if (TryGetInstanceProperty(lookup, head, name, out var cachedProperty))
            {
                ReflectionCacheMetrics.RecordHit();
                return ValidateProperty(cachedProperty, name);
            }

            if ((head?.Count ?? 0) + 1 >= SmallInstancePropertyCacheCapacity)
            {
                var additionalProperties = GetOrCreateAdditionalProperties(lookup);
                return GetCachedProperty(additionalProperties, name,
                    PropertyCacheCapacity - SmallInstancePropertyCacheCapacity);
            }

            if (!hasResolvedProperty)
            {
                property = ResolveProperty(name);
                hasResolvedProperty = true;
                continue;
            }

            // CAS 失败后回到循环重查，避免并发重复项并保证短链不超过容量。
            var candidate = new InstancePropertyEntry(name, property, head);
            if (System.Threading.Interlocked.CompareExchange(ref lookup.AdditionalHead, candidate, head) == head)
            {
                ReflectionCacheMetrics.RecordMiss();
                return ValidateProperty(property, name);
            }
        }
    }

    private static bool TryGetInstanceProperty(InstancePropertyLookup lookup, string name, out PropertyInfo property)
    {
        var head = System.Threading.Volatile.Read(ref lookup.AdditionalHead);
        return TryGetInstanceProperty(lookup, head, name, out property);
    }

    private static bool TryGetInstanceProperty(InstancePropertyLookup lookup, InstancePropertyEntry head,
        string name, out PropertyInfo property)
    {
        if (string.Equals(lookup.FirstName, name, StringComparison.Ordinal))
        {
            property = lookup.FirstProperty;
            return true;
        }

        for (var entry = head; entry != null; entry = entry.Next)
        {
            if (!string.Equals(entry.Name, name, StringComparison.Ordinal))
                continue;
            property = entry.Property;
            return true;
        }

        property = null;
        return false;
    }

    private static PropertyLookup GetOrCreateAdditionalProperties(InstancePropertyLookup lookup)
    {
        var properties = System.Threading.Volatile.Read(ref lookup.AdditionalProperties);
        if (properties != null)
            return properties;

        var candidate = new PropertyLookup();
        return System.Threading.Interlocked.CompareExchange(ref lookup.AdditionalProperties, candidate, null)
               ?? candidate;
    }

    private PropertyInfo GetCachedPropertyMiss(PropertyLookup lookup, string name, int capacity)
    {
        Lazy<PropertyInfo> lazy = null;
        var bypass = false;
        lock (lookup.AdmissionGate)
        {
            if (lookup.Properties.TryGetValue(name, out var existing))
            {
                ReflectionCacheMetrics.Record(existing);
                lazy = existing;
            }
            else if (lookup.Properties.Count >= capacity)
            {
                ReflectionCacheMetrics.RecordBypass();
                bypass = true;
            }
            else
            {
                lazy = CreatePropertyLazy(_type, name);
                lookup.Properties.TryAdd(name, lazy);
                ReflectionCacheMetrics.RecordMiss();
            }
        }

        var property = bypass
            ? _type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            : lazy.Value;
        return ValidateProperty(property, name);
    }

    private PropertyInfo ResolveProperty(string name) =>
        _type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

    private static Lazy<PropertyInfo> CreatePropertyLazy(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        Type type, string name) =>
        new(() => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance),
            LazyThreadSafetyMode.ExecutionAndPublication);

    private PropertyInfo ValidateProperty(PropertyInfo property, string name)
    {
        if (property is null)
            throw new MissingMemberException(_type.FullName, name);
        if (property.GetIndexParameters().Length != 0)
            throw new ArgumentException($"索引属性 '{name}' 需要索引参数。", nameof(name));
        return property;
    }
}
