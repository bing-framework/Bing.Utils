using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

public static partial class PropertyInfoExtensions
{
    private static class TypedAccessorScope<T, TProperty>
    {
        // 仅保存布尔结果；可回收的类型参数不会把常驻 PropertyInfo 加入委托缓存。
        internal static readonly bool IsCollectible = ReflectionCacheScope.IsCollectible(typeof(T)) ||
                                                      ReflectionCacheScope.IsCollectible(typeof(TProperty));
    }

    private static class TypedAccessorCache<T, TProperty>
    {
        internal static readonly ConditionalWeakTable<PropertyInfo, Lazy<Func<T, TProperty>>> Getters = new();
        internal static readonly ConditionalWeakTable<PropertyInfo, Lazy<Action<T, TProperty>>> Setters = new();
    }

    /// <summary>获取强类型实例属性读取器，值类型属性不经过 object 装箱。</summary>
    /// <typeparam name="T">实例类型，可以是属性声明类型的派生类型。</typeparam>
    /// <typeparam name="TProperty">属性的精确类型，不进行数值转换或类型适配。</typeparam>
    /// <param name="propertyInfo">具有公共 getter 的非索引器实例属性。</param>
    /// <returns>可重复调用的强类型读取委托。</returns>
    /// <exception cref="ArgumentNullException">属性为 null。</exception>
    /// <exception cref="ArgumentException">实例或属性类型不匹配，或属性为静态属性、索引器。</exception>
    /// <exception cref="InvalidOperationException">属性没有 getter。</exception>
    /// <exception cref="MemberAccessException">getter 非公共。</exception>
    /// <exception cref="NotSupportedException">属性为引用返回、指针、开放泛型等不支持的类型。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Func<T, TProperty> GetValueGetter<T, TProperty>(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        // 只有完成校验且元数据可安全持有的条目才会入缓存；命中后无需重复检查元数据。
        if (!TypedAccessorScope<T, TProperty>.IsCollectible &&
            TypedAccessorCache<T, TProperty>.Getters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
        ValidateTypedAccessor<T, TProperty>(propertyInfo, getter: true);
        // PropertyInfo 可能来自常驻基类，仍需检查委托签名中的可回收实例类型。
        if (TypedAccessorScope<T, TProperty>.IsCollectible || ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildTypedGetter<T, TProperty>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypedAccessorCache<T, TProperty>.Getters, propertyInfo);
        return TypedAccessorCache<T, TProperty>.Getters.GetValue(propertyInfo,
            property => CoreCacheMetrics.CreateLazy(() => BuildTypedGetter<T, TProperty>(property))).Value;
    }

    /// <summary>获取强类型实例属性设置器，值类型属性值不经过 object 装箱。</summary>
    /// <typeparam name="T">引用类型的实例类型。结构体变量应使用按引用设置入口。</typeparam>
    /// <typeparam name="TProperty">属性的精确类型，不进行数值转换或类型适配。</typeparam>
    /// <param name="propertyInfo">具有公共 setter 的非索引器实例属性。</param>
    /// <returns>可重复调用的强类型设置委托。</returns>
    /// <exception cref="ArgumentNullException">属性为 null。</exception>
    /// <exception cref="ArgumentException">实例或属性类型不匹配，或属性为静态属性、索引器。</exception>
    /// <exception cref="InvalidOperationException">属性没有 setter 或为 init 属性。</exception>
    /// <exception cref="MemberAccessException">setter 非公共。</exception>
    /// <exception cref="NotSupportedException">实例为值类型，或属性为不支持的类型。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Action<T, TProperty> GetValueSetter<T, TProperty>(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        if (!TypedAccessorScope<T, TProperty>.IsCollectible &&
            TypedAccessorCache<T, TProperty>.Setters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
        if (typeof(T).IsValueType)
            throw new NotSupportedException("值类型属性请使用 GetValueSetterByRef<T>，以便将修改回写到原始实例。");
        ValidateTypedAccessor<T, TProperty>(propertyInfo, getter: false);
        // PropertyInfo 可能来自常驻基类，仍需检查委托签名中的可回收实例类型。
        if (TypedAccessorScope<T, TProperty>.IsCollectible || ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildTypedSetter<T, TProperty>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypedAccessorCache<T, TProperty>.Setters, propertyInfo);
        return TypedAccessorCache<T, TProperty>.Setters.GetValue(propertyInfo,
            property => CoreCacheMetrics.CreateLazy(() => BuildTypedSetter<T, TProperty>(property))).Value;
    }

    private static void ValidateTypedAccessor<T, TProperty>(PropertyInfo property, bool getter)
    {
        if (property == null)
            throw new ArgumentNullException("propertyInfo");
        ValidateAccessor(property, getter, requireInstance: true);
        ValidateBoxablePropertyType(property);
        if (property.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的实例类型。", "propertyInfo");
        if (property.PropertyType != typeof(TProperty))
            throw new ArgumentException("TProperty 必须与属性类型完全一致。", "propertyInfo");
    }

    private static Func<T, TProperty> BuildTypedGetter<T, TProperty>(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var instance = Expression.Parameter(typeof(T), "instance");
        return Expression.Lambda<Func<T, TProperty>>(Expression.Property(instance, property), instance).Compile();
    }

    private static Action<T, TProperty> BuildTypedSetter<T, TProperty>(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var instance = Expression.Parameter(typeof(T), "instance");
        var value = Expression.Parameter(typeof(TProperty), "value");
        return Expression.Lambda<Action<T, TProperty>>(
            Expression.Call(instance, property.SetMethod, value), instance, value).Compile();
    }
}
