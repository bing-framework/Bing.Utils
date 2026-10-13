using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>核心包和反射子包共用的严格构造绑定及弱键缓存。</summary>
internal static class StrictConstructorBinder
{
    internal const int ConstructorCacheCapacity = 512;
    private static int _constructorScanCount;
    internal static int ConstructorScanCount => System.Threading.Volatile.Read(ref _constructorScanCount);
    private static int _constructorInvokerBuildCount;
    internal static int ConstructorInvokerBuildCount => System.Threading.Volatile.Read(ref _constructorInvokerBuildCount);
    private static readonly ConditionalWeakTable<Type, ConstructorLookup> ConstructorLookups = new();
    private static readonly ConditionalWeakTable<ConstructorInfo, Lazy<Func<object[], object>>> ConstructorInvokers = new();

    private sealed class ConstructorLookup
    {
        internal readonly ConcurrentDictionary<ConstructorSignature, Lazy<ConstructorInfo>> Constructors = new();
        internal readonly object AdmissionGate = new();
        internal ConstructorFastPath FastPath;
    }

    private sealed class ConstructorFastPath
    {
        private readonly Type[] _argumentTypes;
        internal readonly Func<object[], object> Invoker;

        internal ConstructorFastPath(Type[] argumentTypes, Func<object[], object> invoker)
        {
            _argumentTypes = argumentTypes;
            Invoker = invoker;
        }

        internal bool Matches(object[] arguments)
        {
            if (_argumentTypes.Length != arguments.Length)
                return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (_argumentTypes[index] != arguments[index]?.GetType())
                    return false;
            }
            return true;
        }
    }

    private sealed class ConstructorSignature : IEquatable<ConstructorSignature>
    {
        private readonly Type[] _argumentTypes;

        internal ConstructorSignature(Type[] argumentTypes) => _argumentTypes = argumentTypes;

        public bool Equals(ConstructorSignature other) => other != null
            && _argumentTypes.Length == other._argumentTypes.Length
            && _argumentTypes.SequenceEqual(other._argumentTypes);

        public override bool Equals(object obj) => Equals(obj as ConstructorSignature);

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var argumentType in _argumentTypes)
                hash = unchecked(hash * 31 + (argumentType == null ? 0 : RuntimeHelpers.GetHashCode(argumentType)));
            return hash;
        }
    }
    /// <summary>
    /// 严格创建单个实例。
    /// </summary>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="args">构造函数参数。</param>
    /// <returns>创建的实例。</returns>
    /// <exception cref="ArgumentException">当目标类型不可实例化，或不存在唯一兼容构造函数时抛出。</exception>
    /// <exception cref="InvalidOperationException">当构造函数执行失败时抛出。</exception>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("构造函数按运行时元数据绑定并编译委托，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    internal static object CreateInstance(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, object[] args)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        EnsureCreatableType(type);
        args ??= Array.Empty<object>();
        if (ReflectionCacheScope.IsCollectible(type))
            return CreateCollectibleInstance(type, args);
        var lookup = ConstructorLookups.GetValue(type, _ => new ConstructorLookup());
        var fastPath = System.Threading.Volatile.Read(ref lookup.FastPath);
        Type[] argumentTypes = null;
        ConstructorInfo constructor = null;
        if (fastPath == null || !fastPath.Matches(args))
        {
            argumentTypes = new Type[args.Length];
            for (var index = 0; index < args.Length; index++)
                argumentTypes[index] = args[index]?.GetType();
            constructor = FindCompatibleConstructor(type, argumentTypes, lookup);
        }
        else
            CoreCacheMetrics.RecordHit();

        try
        {
            if (constructor == null)
                return fastPath.Invoker(args);

            var collectibleConstructor = ReflectionCacheScope.IsCollectible(constructor);
            Func<object[], object> invoker;
            if (collectibleConstructor)
            {
                // 构造函数签名可能引用 collectible 程序集；委托不能进入长期缓存。
                CoreCacheMetrics.RecordBypass();
                invoker = CompileConstructor(constructor);
            }
            else
            {
                invoker = ConstructorInvokers.GetValue(constructor, key => CoreCacheMetrics.CreateLazy(
                    () => CompileConstructor(key))).Value;
            }

            if (!collectibleConstructor && !argumentTypes.Any(ReflectionCacheScope.IsCollectible))
                System.Threading.Volatile.Write(ref lookup.FastPath, new ConstructorFastPath(argumentTypes, invoker));
            return invoker(args);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"调用类型“{type.FullName}”的构造函数时发生异常", exception);
        }
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("构造函数委托依赖运行时构造函数元数据，裁剪时必须保留目标构造函数。")]
#endif
    private static Func<object[], object> CompileConstructor(ConstructorInfo constructor)
    {
        System.Threading.Interlocked.Increment(ref _constructorInvokerBuildCount);
        var arguments = Expression.Parameter(typeof(object[]), "arguments");
        var parameters = constructor.GetParameters();
        if (parameters.Any(parameter => parameter.ParameterType.IsByRef))
            return constructor.Invoke;
        var values = parameters.Select((parameter, index) => Expression.Convert(
            Expression.ArrayIndex(arguments, Expression.Constant(index)), parameter.ParameterType));
        var body = Expression.Convert(Expression.New(constructor, values), typeof(object));
        return Expression.Lambda<Func<object[], object>>(body, arguments).Compile();
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("构造函数按运行时元数据绑定并编译委托，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    private static object CreateCollectibleInstance(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, object[] args)
    {
        CoreCacheMetrics.RecordBypass();
        var argumentTypes = args.Select(argument => argument?.GetType()).ToArray();
        var constructor = ResolveCompatibleConstructor(type, argumentTypes);
        try
        {
            return CompileConstructor(constructor)(args);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"调用类型“{type.FullName}”的构造函数时发生异常", exception);
        }
    }

    /// <summary>
    /// 查找唯一的兼容构造函数。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <param name="argumentTypes">构造函数参数类型。</param>
    /// <param name="lookup">目标类型的构造函数缓存。</param>
    /// <returns>唯一兼容的构造函数。</returns>
    /// <exception cref="ArgumentException">当不存在或存在多个兼容构造函数时抛出。</exception>
    private static ConstructorInfo FindCompatibleConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, Type[] argumentTypes, ConstructorLookup lookup)
    {
        if (argumentTypes.Any(ReflectionCacheScope.IsCollectible))
        {
            CoreCacheMetrics.RecordBypass();
            return ResolveCompatibleConstructor(type, argumentTypes);
        }
        var key = new ConstructorSignature(argumentTypes);
        if (lookup.Constructors.TryGetValue(key, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            return GetCachedConstructor(lookup, key, existing);
        }

        Lazy<ConstructorInfo> lazy = null;
        var bypass = false;
        lock (lookup.AdmissionGate)
        {
            if (lookup.Constructors.TryGetValue(key, out existing))
            {
                CoreCacheMetrics.Record(existing);
                lazy = existing;
            }
            else if (lookup.Constructors.Count >= ConstructorCacheCapacity)
            {
                CoreCacheMetrics.RecordBypass();
                bypass = true;
            }
            else
            {
                lazy = CoreCacheMetrics.CreateLazy(() => ResolveCompatibleConstructor(type, argumentTypes));
                lookup.Constructors.TryAdd(key, lazy);
                CoreCacheMetrics.RecordMiss();
            }
        }

        if (bypass)
            return ResolveCompatibleConstructor(type, argumentTypes);
        return GetCachedConstructor(lookup, key, lazy);
    }

    private static ConstructorInfo GetCachedConstructor(
        ConstructorLookup lookup,
        ConstructorSignature key,
        Lazy<ConstructorInfo> lazy)
    {
        var constructor = lazy.Value;
        if (!ReflectionCacheScope.IsCollectible(constructor))
            return constructor;

        // 解析结果本身带有 collectible 签名时，立即移除缓存项，避免非 collectible 宿主类型持有它。
        ((ICollection<KeyValuePair<ConstructorSignature, Lazy<ConstructorInfo>>>)lookup.Constructors)
            .Remove(new KeyValuePair<ConstructorSignature, Lazy<ConstructorInfo>>(key, lazy));
        CoreCacheMetrics.RecordBypass();
        return constructor;
    }

    private static ConstructorInfo ResolveCompatibleConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, Type[] argumentTypes)
    {
        System.Threading.Interlocked.Increment(ref _constructorScanCount);
        var candidates = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(constructor => IsConstructorCompatible(constructor, argumentTypes))
            .ToArray();
        if (candidates.Length == 0)
            throw new ArgumentException($"类型“{type.FullName}”不存在与给定参数兼容的公共构造函数", "args");

        var candidateParameterTypes = candidates.Select(constructor => constructor.GetParameters()
            .Select(parameter => parameter.ParameterType).ToArray()).ToArray();
        var bestCandidateIndex = -1;
        for (var candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
        {
            var isDominated = false;
            for (var otherIndex = 0; otherIndex < candidates.Length; otherIndex++)
            {
                if (candidateIndex != otherIndex && IsStrictlyBetterCandidate(
                        candidateParameterTypes[otherIndex], candidateParameterTypes[candidateIndex], argumentTypes))
                {
                    isDominated = true;
                    break;
                }
            }

            if (isDominated)
                continue;
            if (bestCandidateIndex >= 0)
                throw new AmbiguousMatchException($"类型“{type.FullName}”存在多个与给定参数兼容的公共构造函数");
            bestCandidateIndex = candidateIndex;
        }

        if (bestCandidateIndex < 0)
            throw new AmbiguousMatchException($"类型“{type.FullName}”存在多个与给定参数兼容的公共构造函数");
        return candidates[bestCandidateIndex];
    }

    /// <summary>
    /// 判断一个构造函数是否在每个参数位置都不比另一个候选宽泛，并且至少一个位置更具体。
    /// </summary>
    private static bool IsStrictlyBetterCandidate(Type[] candidateParameterTypes, Type[] otherParameterTypes,
        Type[] argumentTypes)
    {
        var hasMoreSpecificParameter = false;

        for (var index = 0; index < argumentTypes.Length; index++)
        {
            var candidateType = candidateParameterTypes[index];
            var otherType = otherParameterTypes[index];
            if (candidateType == otherType)
                continue;

            var argumentType = argumentTypes[index];
            if (argumentType != null)
            {
                if (candidateType == argumentType)
                {
                    hasMoreSpecificParameter = true;
                    continue;
                }
                if (otherType == argumentType)
                    return false;
            }

            if (otherType.IsAssignableFrom(candidateType))
            {
                hasMoreSpecificParameter = true;
                continue;
            }
            if (candidateType.IsAssignableFrom(otherType))
                return false;

            // 不相关的参数类型没有可证明的优先级，避免依赖反射枚举顺序作选择。
            return false;
        }

        return hasMoreSpecificParameter;
    }

    /// <summary>
    /// 判断构造函数是否与给定实参兼容。
    /// </summary>
    /// <param name="constructor">构造函数元数据。</param>
    /// <param name="argumentTypes">构造函数实参的运行时类型。</param>
    /// <returns>实参数量和类型均兼容时返回 true；否则返回 false。</returns>
    private static bool IsConstructorCompatible(ConstructorInfo constructor, Type[] argumentTypes)
    {
        var parameters = constructor.GetParameters();
        if (parameters.Length != argumentTypes.Length)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (!IsArgumentCompatible(parameters[index].ParameterType, argumentTypes[index]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断单个构造函数实参是否可赋值给参数类型。
    /// </summary>
    /// <param name="parameterType">构造函数参数类型。</param>
    /// <param name="argumentType">构造函数实参的运行时类型。</param>
    /// <returns>实参可赋值时返回 true；否则返回 false。</returns>
    private static bool IsArgumentCompatible(Type parameterType, Type argumentType)
    {
        if (argumentType == null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
        return parameterType.IsAssignableFrom(argumentType)
               || Nullable.GetUnderlyingType(parameterType) == argumentType;
    }

    /// <summary>
    /// 验证类型可通过公共构造函数创建实例。
    /// </summary>
    /// <param name="type">待验证的类型。</param>
    /// <exception cref="ArgumentException">当类型为接口、抽象类、开放泛型或指针类型时抛出。</exception>
    internal static void EnsureCreatableType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (type.IsInterface || type.IsAbstract || type.ContainsGenericParameters || type.IsPointer
            || type.IsByRef || type == typeof(void) || IsByRefLikeType(type))
            throw new ArgumentException($"类型“{type.FullName}”不能通过公共构造函数创建实例", nameof(type));
    }

    private static bool IsByRefLikeType(Type type)
    {
#if NET6_0_OR_GREATER
        return type.IsByRefLike;
#else
        return typeof(Type).GetProperty("IsByRefLike")?.GetValue(type) is true;
#endif
    }

    /// <summary>根据实参类型或 null 模式取得唯一兼容构造函数。</summary>
    internal static ConstructorInfo FindConstructor(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type type, Type[] argumentTypes)
    {
        EnsureCreatableType(type);
        if (ReflectionCacheScope.IsCollectible(type))
            return ResolveCompatibleConstructor(type, argumentTypes);
        return FindCompatibleConstructor(type, argumentTypes,
            ConstructorLookups.GetValue(type, _ => new ConstructorLookup()));
    }

}
