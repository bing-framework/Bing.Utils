using System.Linq.Expressions;

using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;
/// <summary>
/// <see cref="PropertyInfo"/> 扩展。
/// </summary>
public static partial class PropertyInfoExtensions
{
    /// <summary>
    /// 记录泛型实例类型是否来自可回收程序集。
    /// </summary>
    /// <typeparam name="T">属性访问器所属的实例类型。</typeparam>
    private static class ObjectAccessorScope<T>
    {
        /// <summary>
        /// 获取当前类型是否来自可回收程序集。
        /// </summary>
        internal static readonly bool IsCollectible = ReflectionCacheScope.IsCollectible(typeof(T));
    }
    /// <summary>
    /// 已编译属性访问器的累计数量。
    /// </summary>
    private static int _accessorCompileCount;
    /// <summary>
    /// 获取已编译的属性访问器数量。
    /// </summary>
    internal static int AccessorCompileCount => System.Threading.Volatile.Read(ref _accessorCompileCount);
    /// <summary>
    /// 仅初始化属性 setter 的检测结果缓存。
    /// </summary>
    private static readonly ConditionalWeakTable<PropertyInfo, StrongBox<bool>> InitOnlySetters = new();
#if !NET6_0_OR_GREATER
    /// <summary>
    /// 兼容目标框架获取 <c>IsByRefLike</c> 属性的元数据。
    /// </summary>
    private static readonly PropertyInfo TypeIsByRefLike = typeof(Type).GetProperty("IsByRefLike");
#endif
#if !NET8_0_OR_GREATER
    /// <summary>
    /// 兼容目标框架获取 <c>IsFunctionPointer</c> 属性的元数据。
    /// </summary>
    private static readonly PropertyInfo TypeIsFunctionPointer = typeof(Type).GetProperty("IsFunctionPointer");
#endif
    /// <summary>
    /// 按引用修改值类型属性的设置器委托。
    /// </summary>
    /// <typeparam name="T">属性所属的值类型。</typeparam>
    /// <param name="instance">要修改属性的原始值类型实例。</param>
    /// <param name="value">要赋给属性的值。</param>
    public delegate void RefPropertySetter<T>(ref T instance, object value) where T : struct;
    /// <summary>
    /// 缓存值类型属性的按引用设置器。
    /// </summary>
    /// <typeparam name="T">属性所属的值类型。</typeparam>
    private static class RefSetterCache<T> where T : struct
    {
        /// <summary>
        /// 按属性缓存已编译的按引用设置器。
        /// </summary>
        internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PropertyInfo, Lazy<RefPropertySetter<T>>> Setters = new();
    }
    /// <summary>
    /// 获取属性值读取器。
    /// </summary>
    /// <typeparam name="T">属性所属的实例类型。</typeparam>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>可读取属性值的委托。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyInfo"/> 为 null 时抛出。</exception>
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
        if (!ObjectAccessorScope<T>.IsCollectible &&
            TypeReflections.TypeCacheManager<T>.PropertyValueGetters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
        ValidateAccessor(propertyInfo, getter: true, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的实例类型。", nameof(propertyInfo));
        if (ObjectAccessorScope<T>.IsCollectible || ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueGetter<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager<T>.PropertyValueGetters, propertyInfo);
        return TypeReflections.TypeCacheManager<T>.PropertyValueGetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Func<T, object>>(() => BuildValueGetter<T>(prop))).Value;
    }
    /// <summary>
    /// 获取属性值读取器。
    /// </summary>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>可读取属性值的委托。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyInfo"/> 为 null 时抛出。</exception>
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
        if (TypeReflections.TypeCacheManager.PropertyValueGetters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
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
    /// 获取属性值设置器。
    /// </summary>
    /// <typeparam name="T">属性所属的实例类型。</typeparam>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>可设置属性值的委托。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyInfo"/> 为 null 时抛出。</exception>
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
        if (!ObjectAccessorScope<T>.IsCollectible &&
            TypeReflections.TypeCacheManager<T>.PropertyValueSetters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
        ValidateAccessor(propertyInfo, getter: false, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的实例类型。", nameof(propertyInfo));
        if (ObjectAccessorScope<T>.IsCollectible || ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueSetter<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(TypeReflections.TypeCacheManager<T>.PropertyValueSetters, propertyInfo);
        return TypeReflections.TypeCacheManager<T>.PropertyValueSetters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<Action<T, object>>(() => BuildValueSetter<T>(prop))).Value;
    }
    /// <summary>
    /// 获取属性值设置器。
    /// </summary>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>可设置属性值的委托。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyInfo"/> 为 null 时抛出。</exception>
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
        if (TypeReflections.TypeCacheManager.PropertyValueSetters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
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
    /// <summary>
    /// 获取按引用修改值类型属性的设置器。
    /// </summary>
    /// <typeparam name="T">属性所属的值类型。</typeparam>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>可将修改写回原始值类型变量的委托。</returns>
    /// <remarks>
    /// 接口属性与结构体声明的属性使用相同的直接赋值转换。用户 setter 异常原样传播，已发生的字段修改保留。
    /// 默认接口实现及泛型变体接口通过装箱实例调用，并在退出时回写结构体。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="propertyInfo"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">属性为索引器、静态属性或不属于指定值类型时抛出。</exception>
    /// <exception cref="InvalidOperationException">属性没有 setter 或 setter 仅允许在初始化时调用时抛出。</exception>
    /// <exception cref="MemberAccessException">setter 非公共成员时抛出。</exception>
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
        if (!ObjectAccessorScope<T>.IsCollectible &&
            RefSetterCache<T>.Setters.TryGetValue(propertyInfo, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return existing.Value;
        }
        ValidateAccessor(propertyInfo, getter: false, requireInstance: true);
        ValidateBoxablePropertyType(propertyInfo);
        if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(T)) != true)
            throw new ArgumentException("属性不属于指定的值类型。", nameof(propertyInfo));
        if (ObjectAccessorScope<T>.IsCollectible || ReflectionCacheScope.IsCollectible(propertyInfo))
            return BuildValueSetterByRef<T>(propertyInfo);
        CoreCacheMetrics.RecordLookup(RefSetterCache<T>.Setters, propertyInfo);
        return RefSetterCache<T>.Setters.GetValue(propertyInfo,
            prop => CoreCacheMetrics.CreateLazy<RefPropertySetter<T>>(() => BuildValueSetterByRef<T>(prop))).Value;
    }
    /// <summary>
    /// 编译属性值读取器。
    /// </summary>
    /// <typeparam name="T">属性所属的实例类型。</typeparam>
    /// <param name="property">属性元数据。</param>
    /// <returns>已编译的属性值读取器。</returns>
    private static Func<T, object> BuildValueGetter<T>(PropertyInfo property)
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var instance = Expression.Parameter(typeof(T), "i");
        var propertyExpression = Expression.Property(instance, property);
        var convert = Expression.Convert(propertyExpression, typeof(object));
        return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
    }
    /// <summary>
    /// 编译属性值读取器。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>已编译的属性值读取器。</returns>
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
    /// <summary>
    /// 编译属性值设置器。
    /// </summary>
    /// <typeparam name="T">属性所属的实例类型。</typeparam>
    /// <param name="property">属性元数据。</param>
    /// <returns>已编译的属性值设置器。</returns>
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
    /// <summary>
    /// 编译属性值设置器。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>已编译的属性值设置器。</returns>
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
    /// <summary>
    /// 编译值类型属性的按引用设置器。
    /// </summary>
    /// <typeparam name="T">属性所属的值类型。</typeparam>
    /// <param name="property">属性元数据。</param>
    /// <returns>已编译的按引用设置器。</returns>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("按引用属性设置器通过 Expression.Compile 生成委托。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("按引用属性设置器依赖属性访问器及接口实现映射元数据。")]
#endif
    private static RefPropertySetter<T> BuildValueSetterByRef<T>(PropertyInfo property) where T : struct
    {
        System.Threading.Interlocked.Increment(ref _accessorCompileCount);
        var instanceExpression = Expression.Parameter(typeof(T).MakeByRefType(), "instance");
        var valueExpression = Expression.Parameter(typeof(object), "value");
        var setter = property.GetSetMethod();
        var convertedValue = Expression.Convert(valueExpression, property.PropertyType);
        if (!RequiresBoxedRefSetter<T>(setter))
        {
            // 接口在结构体上有实际实现时，生成 constrained 调用，直接修改原变量。
            var call = Expression.Call(instanceExpression, setter, convertedValue);
            return Expression.Lambda<RefPropertySetter<T>>(call, instanceExpression, valueExpression).Compile();
        }

        // 默认接口实现接收装箱实例；用 finally 保留 setter 抛异常之前的字段修改。
        var boxed = Expression.Variable(typeof(object), "boxed");
        var boxedCall = Expression.Call(Expression.Convert(boxed, setter.DeclaringType!), setter, convertedValue);
        var body = Expression.Block(new[] { boxed },
            Expression.Assign(boxed, Expression.Convert(instanceExpression, typeof(object))),
            Expression.TryFinally(boxedCall, Expression.Block(
                Expression.Assign(instanceExpression, Expression.Convert(boxed, typeof(T))), Expression.Empty())));
        return Expression.Lambda<RefPropertySetter<T>>(body, instanceExpression, valueExpression).Compile();
    }
    /// <summary>
    /// 判断接口属性设置器是否需要装箱调用。
    /// </summary>
    /// <typeparam name="T">属性所属的值类型。</typeparam>
    /// <param name="setter">属性设置器方法。</param>
    /// <returns>需要装箱调用时为 true，否则为 false。</returns>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("接口实现映射在运行时确定，裁剪时必须保留接口及实现方法元数据。")]
#endif
    private static bool RequiresBoxedRefSetter<T>(MethodInfo setter) where T : struct
    {
        var contract = setter.DeclaringType!;
        if (!contract.IsInterface)
            return false;
        // 可赋值的泛型变体接口未必直接出现在实现列表中，不能按精确接口取得映射。
        if (!typeof(T).GetInterfaces().Contains(contract))
            return true;

        var map = typeof(T).GetInterfaceMap(contract);
        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            if (map.InterfaceMethods[index].Equals(setter))
                return map.TargetMethods[index].DeclaringType?.IsInterface != false;
        }
        return true;
    }
    /// <summary>
    /// 验证属性访问器满足委托生成要求。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <param name="getter">是否验证 getter。</param>
    /// <param name="requireInstance">是否要求实例访问器。</param>
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
    /// <summary>
    /// 验证属性类型可以安全装箱为 <see cref="object"/>。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    private static void ValidateBoxablePropertyType(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        if (!IsUnsupportedBoxedPropertyType(propertyType))
            return;
        throw new NotSupportedException($"属性“{property.Name}”的类型“{propertyType}”不能通过 object 属性委托访问。");
    }
    /// <summary>
    /// 判断属性类型是否不能安全装箱为 <see cref="object"/>。
    /// </summary>
    /// <param name="type">属性类型。</param>
    /// <returns>类型不支持装箱委托时为 true，否则为 false。</returns>
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
    /// <summary>
    /// 判断属性 setter 是否仅允许在初始化时赋值。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>属性具有仅初始化 setter 时为 true；属性为 null 或具有普通 setter 时为 false。</returns>
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
    /// <summary>
    /// 检查属性 setter 是否包含仅初始化标记。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>包含 <c>IsExternalInit</c> 修饰符时为 true，否则为 false。</returns>
    private static bool HasInitOnlySetterMarker(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;
    /// <summary>
    /// 判断属性是否为静态属性。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <returns>属性为静态属性时为 true，否则为 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 null 时抛出。</exception>
    public static bool IsStatic(this PropertyInfo property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));
        return (property.GetMethod ?? property.SetMethod)?.IsStatic ?? false;
    }
    /// <summary>
    /// 判断属性是否为索引器属性。
    /// </summary>
    /// <param name="propertyInfo">属性元数据。</param>
    /// <returns>属性为索引器时为 true，否则为 false。</returns>
    private static bool IsIndexerProperty(PropertyInfo propertyInfo)
    {
        // 索引器属性的特征：
        // 1. 属性名为 "Item"
        // 2. 有参数（索引参数）
        return propertyInfo.GetIndexParameters().Length > 0;
    }
}
