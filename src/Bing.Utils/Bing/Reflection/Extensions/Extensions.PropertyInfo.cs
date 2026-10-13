using System.Linq.Expressions;

using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 属性信息(<see cref="PropertyInfo"/>) 扩展
/// </summary>
public static partial class PropertyInfoExtensions
{
    private static int _accessorCompileCount;
    internal static int AccessorCompileCount => System.Threading.Volatile.Read(ref _accessorCompileCount);
    private static readonly ConditionalWeakTable<PropertyInfo, StrongBox<bool>> InitOnlySetters = new();
#if !NET6_0_OR_GREATER
    private static readonly PropertyInfo TypeIsByRefLike = typeof(Type).GetProperty("IsByRefLike");
#endif
#if !NET8_0_OR_GREATER
    private static readonly PropertyInfo TypeIsFunctionPointer = typeof(Type).GetProperty("IsFunctionPointer");
#endif
    /// <summary>按引用修改值类型属性的设置器。</summary>
    public delegate void RefPropertySetter<T>(ref T instance, object value) where T : struct;

    private static class RefSetterCache<T> where T : struct
    {
        internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PropertyInfo, Lazy<RefPropertySetter<T>>> Setters = new();
    }
    /// <summary>
    /// 获取 值获取器
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="propertyInfo">属性</param>
    /// <exception cref="ArgumentException">属性为索引器、静态属性或不属于指定实例类型时抛出。</exception>
    /// <exception cref="InvalidOperationException">属性没有 getter 时抛出。</exception>
    /// <exception cref="MemberAccessException">getter 非公共成员时抛出。</exception>
    /// <exception cref="NotSupportedException">属性类型不能装箱为 <see cref="object"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Func<T, object> GetValueGetter<T>(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ValidateAccessor(propertyInfo, getter: true, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的实例类型。", nameof(propertyInfo));
        if (ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueGetter<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager<T>.PropertyValueGetters, propertyInfo);
        return TypeReflections.TypeCacheManager<T>.PropertyValueGetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Func<T, object>>(() => BuildValueGetter<T>(prop))).Value;
    }

    /// <summary>
    /// 获取 值获取器
    /// </summary>
    /// <param name="propertyInfo">属性</param>
    /// <exception cref="ArgumentException">属性为索引器时抛出。</exception>
    /// <exception cref="InvalidOperationException">属性没有 getter 时抛出。</exception>
    /// <exception cref="MemberAccessException">getter 非公共成员时抛出。</exception>
    /// <exception cref="NotSupportedException">属性类型不能装箱为 <see cref="object"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Func<object, object> GetValueGetter(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ValidateAccessor(propertyInfo, getter: true, requireInstance: false);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType == null)
            throw new ArgumentException("属性必须属于已声明的类型。", nameof(propertyInfo));
        if (ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueGetter(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager.PropertyValueGetters, propertyInfo);
        return TypeReflections.TypeCacheManager.PropertyValueGetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Func<object, object>>(() => BuildValueGetter(prop))).Value;
    }

    /// <summary>
    /// 获取 值设置器
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="propertyInfo">属性</param>
    /// <exception cref="ArgumentException">属性为索引器、静态属性或不属于指定实例类型时抛出。</exception>
    /// <exception cref="InvalidOperationException">属性没有 setter 时抛出。</exception>
    /// <exception cref="MemberAccessException">setter 非公共成员时抛出。</exception>
    /// <exception cref="NotSupportedException">值类型应使用引用语义设置器，或属性类型不能装箱为 <see cref="object"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Action<T, object> GetValueSetter<T>(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        if (typeof(T).IsValueType)
            throw new NotSupportedException("值类型属性请使用 GetValueSetterByRef<T>，以便将修改回写到原始实例。");
        ValidateAccessor(propertyInfo, getter: false, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的实例类型。", nameof(propertyInfo));
        if (ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueSetter<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager<T>.PropertyValueSetters, propertyInfo);
        return TypeReflections.TypeCacheManager<T>.PropertyValueSetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Action<T, object>>(() => BuildValueSetter<T>(prop))).Value;
    }

    /// <summary>
    /// 获取 值设置器
    /// </summary>
    /// <param name="propertyInfo">属性</param>
    /// <exception cref="ArgumentException">属性为索引器时抛出。</exception>
    /// <exception cref="InvalidOperationException">属性没有 setter 时抛出。</exception>
    /// <exception cref="MemberAccessException">setter 非公共成员时抛出。</exception>
    /// <exception cref="NotSupportedException">属性类型不能装箱为 <see cref="object"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static Action<object, object> GetValueSetter(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ValidateAccessor(propertyInfo, getter: false, requireInstance: false);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType == null)
            throw new ArgumentException("属性必须属于已声明的类型。", nameof(propertyInfo));
        if (ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueSetter(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager.PropertyValueSetters, propertyInfo);
        return TypeReflections.TypeCacheManager.PropertyValueSetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Action<object, object>>(() => BuildValueSetter(prop))).Value;
    }

    /// <summary>获取能够将修改写回值类型变量的属性设置器。</summary>
    /// <exception cref="NotSupportedException">属性类型不能装箱为 <see cref="object"/> 时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("属性访问器通过 Expression.Compile 生成委托，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
#endif
    public static RefPropertySetter<T> GetValueSetterByRef<T>(this PropertyInfo propertyInfo) where T : struct
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ValidateAccessor(propertyInfo, getter: false, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的值类型。", nameof(propertyInfo));
        if (ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueSetterByRef<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(RefSetterCache<T>.Setters, propertyInfo);
        return RefSetterCache<T>.Setters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<RefPropertySetter<T>>(() => BuildValueSetterByRef<T>(prop))).Value;
    }

    private static Func<T, object> BuildValueGetter<T>(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var instance = Expression.Parameter(typeof(T), "i");
        var propertyExpression = Expression.Property(instance, property);
        var convert = Expression.Convert(propertyExpression, typeof(object));
        return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
    }

    private static Func<object, object> BuildValueGetter(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var getMethod = property.GetGetMethod();
        if (IsStatic(property))
        {
            var staticCall = Expression.Call(null, getMethod);
            var castToObject = Expression.Convert(staticCall, typeof(object));
            return (Func<object, object>)Expression.Lambda(
                castToObject, Expression.Parameter(typeof(object), "obj")).Compile();
        }

        var instance = Expression.Parameter(typeof(object), "obj");
        var getterCall = Expression.Call(
            property.DeclaringType!.IsValueType
                ? Expression.Unbox(instance, property.DeclaringType)
                : Expression.Convert(instance, property.DeclaringType), getMethod);
        var castToObjectResult = Expression.Convert(getterCall, typeof(object));
        return (Func<object, object>)Expression.Lambda(castToObjectResult, instance).Compile();
    }

    private static Action<T, object> BuildValueSetter<T>(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var setMethod = property.GetSetMethod();
        var instance = Expression.Parameter(typeof(T), "i");
        var argument = Expression.Parameter(typeof(object), "a");
        var setterCall = Expression.Call(instance, setMethod,
            Expression.Convert(argument, property.PropertyType));
        return Expression.Lambda<Action<T, object>>(setterCall, instance, argument).Compile();
    }

    private static Action<object, object> BuildValueSetter(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var setMethod = property.GetSetMethod();
        var instance = Expression.Parameter(typeof(object), "o");
        var value = Expression.Parameter(typeof(object), "value");
        if (IsStatic(property))
        {
            var staticCall = Expression.Call(null, setMethod,
                Expression.Convert(value, property.PropertyType));
            return Expression.Lambda<Action<object, object>>(staticCall, instance, value).Compile();
        }

        var setterCall = Expression.Call(
            property.DeclaringType!.IsValueType
                ? Expression.Unbox(instance, property.DeclaringType)
                : Expression.Convert(instance, property.DeclaringType),
            setMethod, Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<object, object>>(setterCall, instance, value).Compile();
    }

    private static RefPropertySetter<T> BuildValueSetterByRef<T>(PropertyInfo property) where T : struct
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        if (property.DeclaringType != typeof(T))
        {
            return (ref T instance, object value) =>
            {
                object boxed = instance;
                property.SetValue(boxed, value);
                instance = (T)boxed;
            };
        }

        var instanceExpression = Expression.Parameter(typeof(T).MakeByRefType(), "instance");
        var valueExpression = Expression.Parameter(typeof(object), "value");
        var call = Expression.Call(instanceExpression, property.GetSetMethod(),
            Expression.Convert(valueExpression, property.PropertyType));
        return Expression.Lambda<RefPropertySetter<T>>(call, instanceExpression, valueExpression).Compile();
    }

    private static void ValidateAccessor(PropertyInfo property, bool getter, bool requireInstance)
    {
        if (IsIndexerProperty(property))
            throw new ArgumentException("索引器不能使用无参数属性委托。", nameof(property));
        var accessor = getter ? property.GetMethod : property.SetMethod;
        if (accessor == null)
            throw new InvalidOperationException($"属性“{property.Name}”没有{(getter ? "getter" : "setter")}。");
        if (!getter && IsInitOnlySetter(property))
            throw new InvalidOperationException($"属性“{property.Name}”只能在初始化时设置。");
        if (!accessor.IsPublic)
            throw new MemberAccessException($"属性“{property.Name}”的{(getter ? "getter" : "setter")}不是公共成员。");
        if (requireInstance && accessor.IsStatic)
            throw new ArgumentException("泛型属性委托仅支持实例属性。", nameof(property));
    }

    /// <summary>拒绝无法安全装箱为 object 的属性类型，避免生成无效表达式并写入委托缓存。</summary>
    private static void ValidateBoxablePropertyType(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (!IsUnsupportedBoxedPropertyType(propertyType))
            return;
        throw new NotSupportedException($"属性“{property.Name}”的类型“{propertyType}”不能通过 object 属性委托访问。");
    }

    private static bool IsUnsupportedBoxedPropertyType(Type type) =>
        type == typeof(void) || type.IsByRef || type.IsPointer || type.ContainsGenericParameters
#if NET6_0_OR_GREATER
        || type.IsByRefLike
#else
        || TypeIsByRefLike?.GetValue(type) is true
#endif
#if NET8_0_OR_GREATER
        || type.IsFunctionPointer;
#else
        || TypeIsFunctionPointer?.GetValue(type) is true;
#endif

    internal static bool IsInitOnlySetter(PropertyInfo property)
    {
        if (property == null)
            return false;

        // 可回收程序集中的属性不进入共享缓存，即使当前缓存使用弱键，也保持与其他属性访问器一致的旁路策略。
        if (ReflectionCacheScope.IsCollectible(property))
            return HasInitOnlySetterMarker(property);

        return InitOnlySetters.GetValue(property,
            key => new StrongBox<bool>(HasInitOnlySetterMarker(key))).Value;
    }

    private static bool HasInitOnlySetterMarker(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    /// <summary>
    /// 判断属性是否静态
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsStatic(this PropertyInfo property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));
        return (property.GetMethod ?? property.SetMethod)?.IsStatic ?? false;
    }

    /// <summary>
    /// 判断属性是否为索引器属性
    /// </summary>
    /// <param name="propertyInfo">属性信息</param>
    /// <returns>如果是索引器属性返回 true，否则返回 false</returns>
    private static bool IsIndexerProperty(PropertyInfo propertyInfo)
    {
        // 索引器属性的特征：
        // 1. 属性名为 "Item"
        // 2. 有参数（索引参数）
        return propertyInfo.GetIndexParameters().Length > 0;
    }
}
