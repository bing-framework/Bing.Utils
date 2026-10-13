using AspectCore.Extensions.Reflection;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 定义在反射操作中使用的选项。
/// </summary>
public enum ReflectionOptions
{
    /// <summary>
    /// 默认选项。
    /// 使用此选项时，反射操作将不自动考虑继承链，只处理当前类型声明的成员。
    /// </summary>
    Default = 0,

    /// <summary>
    /// 继承选项。
    /// 使用此选项时，反射操作将考虑类型的继承链，包括从基类继承的成员。
    /// 这对于需要搜索整个继承层次结构中的成员时特别有用。
    /// </summary>
    Inherit = 1
}

/// <summary>
/// 定义在使用反射搜索成员时如何处理存在歧义的情况的选项。
/// </summary>
public enum ReflectionAmbiguousOptions
{
    /// <summary>
    /// 默认选项。
    /// 使用此选项时，如果存在歧义（例如，多个同名成员满足搜索条件），反射操作将抛出异常。
    /// 这确保了搜索结果的明确性，要求调用者处理可能的歧义。
    /// </summary>
    Default = 0,

    /// <summary>
    /// 忽略歧义选项。
    /// 使用此选项时，反射操作在遇到歧义情况（例如，有多个符合条件的成员）时不会抛出异常，
    /// 而是选择其中一个成员（具体哪一个可能依赖于反射API的实现细节）。
    /// 这对于调用者来说提供了便利，但在某些情况下可能会牺牲结果的准确性。
    /// </summary>
    IgnoreAmbiguous = 1
}

/// <summary>
/// 类型反射器帮助类
/// </summary>
internal static class TypeReflectorHelper
{
    /// <summary>动态代码不可用时使用运行时公开的特性 API，避免触发第三方的 Reflection.Emit。</summary>
    private static bool UsePublicAttributeApi
    {
        get => !ReflectionRuntimeFeatures.IsDynamicCodeSupported;
    }

    /// <summary>
    /// 获取反射器
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICustomAttributeReflectorProvider GetReflector(MemberInfo member)
    {
        ValidateSupportedMember(member);
        if (UsePublicAttributeApi || CustomAttributeReflectorRuntimeTypeHandle is null
            || ReflectionCacheScope.IsCollectible(member))
            return new DirectAttributeProvider(member);
        return member switch
        {
            TypeInfo typeInfo => typeInfo.GetReflector(),
            Type type => type.GetReflector(),
            FieldInfo field => field.GetReflector(),
            PropertyInfo property => property.GetReflector(),
            MethodInfo method => method.GetReflector(),
            ConstructorInfo constructor => constructor.GetReflector(),
            _ => throw new InvalidOperationException("Current MemberInfo cannot be converted to Reflector.")
        };
    }

    /// <summary>
    /// 获取反射器
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICustomAttributeReflectorProvider GetReflector(ParameterInfo parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));
        if (UsePublicAttributeApi || CustomAttributeReflectorRuntimeTypeHandle is null
            || ReflectionCacheScope.IsCollectible(parameter))
            return new DirectAttributeProvider(parameter);
        return parameter.GetReflector();
    }

    /// <summary>在选择反射后端及继承选项前统一校验成员类型。</summary>
    internal static void ValidateSupportedMember(MemberInfo member)
    {
        if (member is null)
            throw new ArgumentNullException(nameof(member));
        if (member is not Type && member is not FieldInfo && member is not PropertyInfo
            && member is not MethodInfo && member is not ConstructorInfo)
            throw new InvalidOperationException("Current MemberInfo cannot be converted to Reflector.");
    }

    // 可回收元数据不能交给第三方静态强键缓存；直接查询的提供器只由本次调用持有。
    internal sealed class DirectAttributeProvider : ICustomAttributeReflectorProvider
    {
        private readonly MemberInfo _member;
        private readonly ParameterInfo _parameter;

        internal DirectAttributeProvider(MemberInfo member) => _member = member;
        internal DirectAttributeProvider(ParameterInfo parameter) => _parameter = parameter;
        public CustomAttributeReflector[] CustomAttributeReflectors => Array.Empty<CustomAttributeReflector>();

        internal bool IsDefined(Type attributeType) => _member != null
            ? Attribute.IsDefined(_member, attributeType, false)
            : Attribute.IsDefined(_parameter, attributeType, false);

        internal Attribute[] GetAttributes(Type attributeType = null)
        {
            if (_member != null)
                return attributeType == null ? Attribute.GetCustomAttributes(_member, false)
                    : Attribute.GetCustomAttributes(_member, attributeType, false);
            return attributeType == null ? Attribute.GetCustomAttributes(_parameter, false)
                : Attribute.GetCustomAttributes(_parameter, attributeType, false);
        }
    }

    /// <summary>
    /// 自定义特性反射器运行时类型处理
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static readonly FieldInfo CustomAttributeReflectorRuntimeTypeHandle = typeof(CustomAttributeReflector).GetField("_tokens", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// 获取运行时类型处理器集合
    /// </summary>
    /// <param name="reflector">自定义特性反射器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<RuntimeTypeHandle> GetRuntimeTypeHandles(CustomAttributeReflector reflector)
    {
        return GetRuntimeTypeHandles(CustomAttributeReflectorRuntimeTypeHandle, reflector);
    }

    /// <summary>
    /// 获取运行时类型处理器集合。
    /// </summary>
    /// <param name="tokensField">第三方反射器的内部类型令牌字段。</param>
    /// <param name="reflector">自定义特性反射器。</param>
    /// <remarks>
    /// <c>_tokens</c> 是第三方库的内部实现细节，运行时升级或访问限制都可能导致读取失败。
    /// 读取失败时退回到公开的特性类型元数据，避免为类型查询执行特性构造函数。
    /// </remarks>
    private static HashSet<RuntimeTypeHandle> GetRuntimeTypeHandles(FieldInfo tokensField, CustomAttributeReflector reflector)
    {
        if (reflector is null)
            throw new ArgumentNullException(nameof(reflector));

        try
        {
            if (tokensField?.GetValue(reflector) is HashSet<RuntimeTypeHandle> handles)
                return handles;
        }
        catch (Exception exception) when (exception is ArgumentException or MemberAccessException or TargetException or NotSupportedException)
        {
            // 私有字段只是性能优化；字段不存在或读取失败时使用公开 API 保证功能可用。
        }

        var result = new HashSet<RuntimeTypeHandle>();
        for (var type = reflector.AttributeType; type != null; type = type.BaseType)
            result.Add(type.TypeHandle);
        return result;
    }
}

/// <summary>
/// 类型反射 操作
/// </summary>
public static partial class TypeReflections
{
    /// <summary>
    /// 校验
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TRequired AttrRequired<TRequired>(this TRequired value, string message) => value ?? throw new ArgumentException(message);

    /// <summary>
    /// 校验
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<TRequired> AttrRequired<TRequired>(this IEnumerable<TRequired> value, string message)
    {
        if (value is null || !value.Any())
            throw new ArgumentException(message);
        return value;
    }

    /// <summary>
    /// 消除歧义，取第一项的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TAttribute AttrDisambiguation<TAttribute>(this IEnumerable<TAttribute> value) =>
        value is null ? default : value.FirstOrDefault();

    #region IsAttributeDefined

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(MemberInfo member) where TAttribute : Attribute => 
        IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute));

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(MemberInfo member, ReflectionOptions options) where TAttribute : Attribute
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateReflectionOptions(options, nameof(options));
        return options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)),
            ReflectionOptions.Inherit => Attribute.IsDefined(member, typeof(TAttribute), true),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => 
        IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute));

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined<TAttribute>(ParameterInfo parameter, ReflectionOptions options) where TAttribute : Attribute
    {
        ValidateReflectionOptions(options, nameof(options));
        return options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)),
            ReflectionOptions.Inherit => Attribute.IsDefined(parameter, typeof(TAttribute), true),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(MemberInfo member, Type attributeType) => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    public static bool IsAttributeDefined(MemberInfo member, Type attributeType, ReflectionOptions options)
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(options, nameof(options));
        return options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => Attribute.IsDefined(member, attributeType, true),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(ParameterInfo parameter, Type attributeType) => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 判断给定的特性是否定义
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="options">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAttributeDefined(ParameterInfo parameter, Type attributeType, ReflectionOptions options)
    {
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(options, nameof(options));
        return options switch
        {
            ReflectionOptions.Default => IsAttributeDefinedImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => Attribute.IsDefined(parameter, attributeType, true),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options, "未定义的反射选项。")
        };
    }

    /// <summary>统一校验查询特性类型，避免不同反射后端产生不同失败契约。</summary>
    private static void ValidateAttributeType(Type attributeType)
    {
        if (attributeType is null)
            throw new ArgumentNullException(nameof(attributeType));
        if (!typeof(Attribute).IsAssignableFrom(attributeType) || attributeType.ContainsGenericParameters)
            throw new ArgumentException("特性类型必须是已封闭的 Attribute 派生类型。", nameof(attributeType));
    }

    /// <summary>统一校验特性读取的继承选项。</summary>
    private static void ValidateReflectionOptions(ReflectionOptions options, string parameterName)
    {
        if (options != ReflectionOptions.Default && options != ReflectionOptions.Inherit)
            throw new ArgumentOutOfRangeException(parameterName, options, "未定义的反射选项。");
    }

    /// <summary>统一校验特性读取的歧义处理选项。</summary>
    private static void ValidateReflectionAmbiguousOptions(ReflectionAmbiguousOptions options, string parameterName)
    {
        if (options != ReflectionAmbiguousOptions.Default && options != ReflectionAmbiguousOptions.IgnoreAmbiguous)
            throw new ArgumentOutOfRangeException(parameterName, options, "未定义的反射歧义选项。");
    }

    /// <summary>
    /// 判断给定的特性是否定义 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static bool IsAttributeDefinedImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        ValidateAttributeType(attributeType);
        if (customAttributeReflectorProvider is TypeReflectorHelper.DirectAttributeProvider direct)
            return direct.IsDefined(attributeType);
        var attributeReflectors = GetAttributeReflectors(customAttributeReflectorProvider);
        var length = attributeReflectors.Length;
        if (length == 0)
            return false;
        var typeHandle = attributeType.TypeHandle;
        for (var i = 0; i < length; i++)
        {
            var attributeReflector = attributeReflectors[i];
            if (attributeReflector != null
                && TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflector).Contains(typeHandle))
                return true;
        }
        return false;
    }

    #endregion

    #region GetAttribute

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(MemberInfo member, Type attributeType) => GetAttributeImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(ParameterInfo parameter, Type attributeType) => GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(MemberInfo member, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributeImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => member.GetCustomAttribute(attributeType, true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(member, attributeType, refOptions).AttrDisambiguation(),
                _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttribute(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => parameter.GetCustomAttribute(attributeType, true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(parameter, attributeType, refOptions).AttrDisambiguation(),
                _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(MemberInfo member) where TAttribute : Attribute => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute));

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute));

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(MemberInfo member, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => member.GetCustomAttribute<TAttribute>(true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(member, refOptions).AttrDisambiguation(),
                _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttribute<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => (TAttribute)GetAttributeImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)),
            ReflectionOptions.Inherit => ambOptions switch
            {
                ReflectionAmbiguousOptions.Default => parameter.GetCustomAttribute<TAttribute>(true),
                ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(parameter, refOptions).AttrDisambiguation(),
                _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 获取特性 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static Attribute GetAttributeImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        ValidateAttributeType(attributeType);
        if (customAttributeReflectorProvider is TypeReflectorHelper.DirectAttributeProvider direct)
            return direct.GetAttributes(attributeType).FirstOrDefault();
        var attributeReflectors = GetAttributeReflectors(customAttributeReflectorProvider);
        var length = attributeReflectors.Length;
        if (length == 0)
            return default;
        var typeHandle = attributeType.TypeHandle;
        for (var i = 0; i < length; i++)
        {
            var attributeReflector = attributeReflectors[i];
            if (attributeReflector != null
                && TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflector).Contains(typeHandle))
                return attributeReflector.Invoke();
        }
        return default;
    }

    #endregion

    #region GetAttributes

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)).Cast<TAttribute>();

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)).Cast<TAttribute>();

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo member, ReflectionOptions refOptions) where TAttribute : Attribute
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), typeof(TAttribute)).Cast<TAttribute>(),
            ReflectionOptions.Inherit => member.GetCustomAttributes<TAttribute>(true),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions) where TAttribute : Attribute
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), typeof(TAttribute)).Cast<TAttribute>(),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes<TAttribute>(true),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, Type attributeType) => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), attributeType);

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, Type attributeType) => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), attributeType);

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, Type attributeType, ReflectionOptions refOptions)
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member), attributeType),
            ReflectionOptions.Inherit => member.GetCustomAttributes(attributeType, true).Cast<Attribute>(),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions)
    {
        ValidateAttributeType(attributeType);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter), attributeType),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes(attributeType, true).Cast<Attribute>(),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 获取特性集合 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <param name="attributeType">特性类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static IEnumerable<Attribute> GetAttributesImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider, Type attributeType)
    {
        ValidateAttributeType(attributeType);
        if (customAttributeReflectorProvider is TypeReflectorHelper.DirectAttributeProvider direct)
            return direct.GetAttributes(attributeType);
        var attributeReflectors = GetAttributeReflectors(customAttributeReflectorProvider);
        var length = attributeReflectors.Length;
        if (length == 0)
            return Enumerable.Empty<Attribute>();
        var typeHandle = attributeType.TypeHandle;
        var holder = new Attribute[length];
        var counter = 0;
        for (var i = 0; i < length; i++)
        {
            var attributeReflector = attributeReflectors[i];
            if (attributeReflector != null
                && TypeReflectorHelper.GetRuntimeTypeHandles(attributeReflector).Contains(typeHandle))
                holder[counter++] = attributeReflector.Invoke();
        }

        if (length == counter)
            return holder;

        var result = new Attribute[counter];
        Array.Copy(holder, result, counter);
        return result;
    }

    #endregion

    #region GetAttributeRequired

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttributeRequired<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttribute<TAttribute>(member).AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttributeRequired<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttribute<TAttribute>(parameter).AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttributeRequired<TAttribute>(MemberInfo member, ReflectionOptions refOptions,
        ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute<TAttribute>(member, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(member, refOptions)
                .AttrDisambiguation(),
            _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
        };
        return val.AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAttribute GetAttributeRequired<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions,
        ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default) where TAttribute : Attribute
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute<TAttribute>(parameter, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes<TAttribute>(parameter, refOptions)
                .AttrDisambiguation(),
            _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
        };
        return val.AttrRequired($"There is no {typeof(TAttribute)} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttributeRequired(MemberInfo member, Type attributeType) => GetAttribute(member, attributeType).AttrRequired($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttributeRequired(ParameterInfo parameter, Type attributeType) => GetAttribute(parameter, attributeType).AttrRequired($"There is no {attributeType} attribute can be found.");

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttributeRequired(MemberInfo member, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute(member, attributeType, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(member, attributeType, refOptions).AttrDisambiguation(),
            _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
        };
        return val.AttrRequired($"There is no {attributeType} attribute can be found.");
    }

    /// <summary>
    /// 从成员信息中获取指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    /// <param name="ambOptions">反射歧义选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Attribute GetAttributeRequired(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions = ReflectionAmbiguousOptions.Default)
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        ValidateReflectionAmbiguousOptions(ambOptions, nameof(ambOptions));
        var val = ambOptions switch
        {
            ReflectionAmbiguousOptions.Default => GetAttribute(parameter, attributeType, refOptions),
            ReflectionAmbiguousOptions.IgnoreAmbiguous => GetAttributes(parameter, attributeType, refOptions).AttrDisambiguation(),
            _ => throw new ArgumentOutOfRangeException(nameof(ambOptions), ambOptions, "未定义的反射歧义选项。")
        };
        return val.AttrRequired($"There is no {attributeType} attribute can be found.");
    }

    #endregion

    #region GetAttributesRequired

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributesRequired<TAttribute>(MemberInfo member) where TAttribute : Attribute => GetAttributes<TAttribute>(member).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributesRequired<TAttribute>(ParameterInfo parameter) where TAttribute : Attribute => GetAttributes<TAttribute>(parameter).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributesRequired<TAttribute>(MemberInfo member, ReflectionOptions refOptions) where TAttribute : Attribute => GetAttributes<TAttribute>(member, refOptions).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<TAttribute> GetAttributesRequired<TAttribute>(ParameterInfo parameter, ReflectionOptions refOptions) where TAttribute : Attribute => GetAttributes<TAttribute>(parameter, refOptions).AttrRequired($"There is no any {typeof(TAttribute)} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributesRequired(MemberInfo member, Type attributeType) => GetAttributes(member, attributeType).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributesRequired(ParameterInfo parameter, Type attributeType) => GetAttributes(parameter, attributeType).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributesRequired(MemberInfo member, Type attributeType, ReflectionOptions refOptions) => GetAttributes(member, attributeType, refOptions).AttrRequired($"There is no any {attributeType} attributes can be found.");

    /// <summary>
    /// 从成员信息中获取一组指定的 <see cref="Attribute"/> 实例，如果获取失败则抛出异常
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="attributeType">特性类型</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributesRequired(ParameterInfo parameter, Type attributeType, ReflectionOptions refOptions) => GetAttributes(parameter, attributeType, refOptions).AttrRequired($"There is no any {attributeType} attributes can be found.");

    #endregion

    #region GetAllAttributes

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member) => GetAttributesImpl(TypeReflectorHelper.GetReflector(member));

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter) => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter));

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="member">成员元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(MemberInfo member, ReflectionOptions refOptions)
    {
        TypeReflectorHelper.ValidateSupportedMember(member);
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(member)),
            ReflectionOptions.Inherit => member.GetCustomAttributes(true).Cast<Attribute>(),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 从成员信息中获取所有 <see cref="Attribute"/> 实例
    /// </summary>
    /// <param name="parameter">参数元数据</param>
    /// <param name="refOptions">反射选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Attribute> GetAttributes(ParameterInfo parameter, ReflectionOptions refOptions)
    {
        ValidateReflectionOptions(refOptions, nameof(refOptions));
        return refOptions switch
        {
            ReflectionOptions.Default => GetAttributesImpl(TypeReflectorHelper.GetReflector(parameter)),
            ReflectionOptions.Inherit => parameter.GetCustomAttributes(true).Cast<Attribute>(),
            _ => throw new ArgumentOutOfRangeException(nameof(refOptions), refOptions, "未定义的反射选项。")
        };
    }

    /// <summary>
    /// 获取所有特性 的实现方式
    /// </summary>
    /// <param name="customAttributeReflectorProvider">自定义特性反射器提供程序</param>
    /// <exception cref="ArgumentNullException"></exception>
    private static IEnumerable<Attribute> GetAttributesImpl(ICustomAttributeReflectorProvider customAttributeReflectorProvider)
    {
        if (customAttributeReflectorProvider is TypeReflectorHelper.DirectAttributeProvider direct)
            return direct.GetAttributes();
        var attributeReflectors = GetAttributeReflectors(customAttributeReflectorProvider);
        var length = attributeReflectors.Length;
        if (length == 0)
            return Enumerable.Empty<Attribute>();
        var attributeArray = new Attribute[length];
        var counter = 0;
        for (var i = 0; i < length; i++)
        {
            var attributeReflector = attributeReflectors[i];
            if (attributeReflector != null)
                attributeArray[counter++] = attributeReflector.Invoke();
        }

        if (counter == length)
            return attributeArray;

        var result = new Attribute[counter];
        Array.Copy(attributeArray, result, counter);
        return result;
    }

    /// <summary>统一读取第三方特性反射器集合，将异常 provider 的 null 数组视为空集合。</summary>
    private static CustomAttributeReflector[] GetAttributeReflectors(ICustomAttributeReflectorProvider customAttributeReflectorProvider)
    {
        if (customAttributeReflectorProvider is null)
            throw new ArgumentNullException(nameof(customAttributeReflectorProvider));
        return customAttributeReflectorProvider.CustomAttributeReflectors ?? Array.Empty<CustomAttributeReflector>();
    }

    #endregion
}
