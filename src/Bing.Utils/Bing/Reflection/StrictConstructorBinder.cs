using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;
/// <summary>
/// 提供严格构造函数绑定和弱键缓存。
/// </summary>
internal static class StrictConstructorBinder
{
    /// <summary>
    /// 每种类型允许缓存的构造函数签名上限。
    /// </summary>
    internal const int ConstructorCacheCapacity = 512;
    /// <summary>
    /// 已执行构造函数扫描的累计次数。
    /// </summary>
    private static int _constructorScanCount;
    /// <summary>
    /// 获取已执行构造函数扫描的累计次数。
    /// </summary>
    internal static int ConstructorScanCount => System.Threading.Volatile.Read(ref _constructorScanCount);
    /// <summary>
    /// 已构造快速调用器的累计数量。
    /// </summary>
    private static int _constructorInvokerBuildCount;
    /// <summary>
    /// 获取已构造快速调用器的累计数量。
    /// </summary>
    internal static int ConstructorInvokerBuildCount => System.Threading.Volatile.Read(ref _constructorInvokerBuildCount);
    /// <summary>
    /// 按目标类型保存的构造函数查找缓存。
    /// </summary>
    private static readonly ConditionalWeakTable<Type, ConstructorLookup> ConstructorLookups = new();
    /// <summary>
    /// 按构造函数保存的已编译调用委托缓存。
    /// </summary>
    private static readonly ConditionalWeakTable<ConstructorInfo, Lazy<Func<object[], object>>> ConstructorInvokers = new();
    /// <summary>
    /// 单个目标类型的构造函数查找状态。
    /// </summary>
    private sealed class ConstructorLookup
    {
        /// <summary>
        /// 按参数类型签名缓存的构造函数。
        /// </summary>
        internal readonly ConcurrentDictionary<ConstructorSignature, Lazy<ConstructorInfo>> Constructors = new();
        /// <summary>
        /// 限制缓存条目准入的同步锁。
        /// </summary>
        internal readonly object AdmissionGate = new();
        /// <summary>
        /// 当前绑定结果对应的快速调用路径。
        /// </summary>
        internal ConstructorFastPath FastPath;
    }
    /// <summary>
    /// 按实参运行时类型复用构造函数调用器的快速路径。
    /// </summary>
    private sealed class ConstructorFastPath
    {
        /// <summary>
        /// 构造函数参数对应的运行时类型序列。
        /// </summary>
        private readonly Type[] _argumentTypes;
        /// <summary>
        /// 已选构造函数的调用委托。
        /// </summary>
        internal readonly Func<object[], object> Invoker;
        /// <summary>
        /// 初始化一个 <see cref="ConstructorFastPath"/> 类型的实例。
        /// </summary>
        /// <param name="argumentTypes">构造实参的运行时类型序列。</param>
        /// <param name="invoker">已选构造函数的调用委托。</param>
        internal ConstructorFastPath(Type[] argumentTypes, Func<object[], object> invoker)
        {
            _argumentTypes = argumentTypes;
            Invoker = invoker;
        }
        /// <summary>
        /// 判断实参是否与快速路径的类型序列匹配。
        /// </summary>
        /// <param name="arguments">待检查的构造实参。</param>
        /// <returns>所有实参类型均匹配时为 true，否则为 false。</returns>
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
    /// <summary>
    /// 以构造实参类型序列表示的缓存键。
    /// </summary>
    private sealed class ConstructorSignature : IEquatable<ConstructorSignature>
    {
        /// <summary>
        /// 构造实参对应的运行时类型序列。
        /// </summary>
        private readonly Type[] _argumentTypes;
        /// <summary>
        /// 初始化一个 <see cref="ConstructorSignature"/> 类型的实例。
        /// </summary>
        /// <param name="argumentTypes">构造实参的运行时类型序列。</param>
        internal ConstructorSignature(Type[] argumentTypes) => _argumentTypes = argumentTypes;
        /// <inheritdoc />
        public bool Equals(ConstructorSignature other) => other != null
            && _argumentTypes.Length == other._argumentTypes.Length
            && _argumentTypes.SequenceEqual(other._argumentTypes);
        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as ConstructorSignature);
        /// <inheritdoc />
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
    /// <exception cref="ArgumentException">目标类型不可实例化，或不存在兼容构造函数时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法确定唯一优先项的兼容构造函数时抛出。</exception>
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

    /// <summary>
    /// 编译构造函数调用委托。
    /// </summary>
    /// <param name="constructor">要调用的构造函数。</param>
    /// <returns>可使用参数数组调用构造函数的委托。</returns>
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
        {
            // Invoke 负责把 ref/out 参数回写原数组；拆除其包装异常以保持普通构造委托的异常契约。
            return values =>
            {
                try
                {
                    return constructor.Invoke(values);
                }
                catch (TargetInvocationException exception) when (exception.InnerException != null)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    throw;
                }
            };
        }
        var values = parameters.Select((parameter, index) => Expression.Convert(
            Expression.ArrayIndex(arguments, Expression.Constant(index)), parameter.ParameterType));
        var body = Expression.Convert(Expression.New(constructor, values), typeof(object));
        return Expression.Lambda<Func<object[], object>>(body, arguments).Compile();
    }

    /// <summary>
    /// 创建来自可回收程序集的类型实例。
    /// </summary>
    /// <param name="type">要创建实例的类型。</param>
    /// <param name="args">构造函数参数。</param>
    /// <returns>创建的实例。</returns>
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
    /// <exception cref="ArgumentException">不存在兼容构造函数时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法确定唯一优先项的兼容构造函数时抛出。</exception>
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
    /// <summary>
    /// 获取缓存的构造函数，并移除引用可回收程序集的缓存项。
    /// </summary>
    /// <param name="lookup">目标类型的构造函数查找缓存。</param>
    /// <param name="key">构造函数签名键。</param>
    /// <param name="lazy">延迟解析的构造函数。</param>
    /// <returns>与签名匹配的构造函数。</returns>
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
    /// <summary>
    /// 根据实参类型解析唯一兼容的公共构造函数。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <param name="argumentTypes">构造实参的运行时类型序列。</param>
    /// <returns>唯一兼容的构造函数。</returns>
    /// <exception cref="ArgumentException">不存在兼容构造函数时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法确定唯一优先项的兼容构造函数时抛出。</exception>
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
    /// 判断一个构造函数是否比另一个候选更具体。
    /// </summary>
    /// <param name="candidateParameterTypes">当前候选构造函数的参数类型。</param>
    /// <param name="otherParameterTypes">用于比较的候选构造函数参数类型。</param>
    /// <param name="argumentTypes">构造实参的运行时类型。</param>
    /// <returns>当前候选在所有不同参数位置均不更宽泛，且至少一处更具体时为 true，否则为 false。</returns>
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

            candidateType = candidateType.IsByRef ? candidateType.GetElementType() : candidateType;
            otherType = otherType.IsByRef ? otherType.GetElementType() : otherType;
            if (candidateType == otherType)
                continue;
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
            var parameter = parameters[index];
            // out 参数允许 null 占位；ref/in 的输入仍按元素类型检查。
            if (parameter.IsOut && parameter.ParameterType.IsByRef && argumentTypes[index] == null)
                continue;
            if (!IsArgumentCompatible(parameter.ParameterType, argumentTypes[index]))
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
        if (parameterType.IsByRef)
        {
            // 元数据查询可显式传入 T&；实例化传入的是装箱后的 T。
            if (argumentType?.IsByRef == true)
                return parameterType == argumentType;
            parameterType = parameterType.GetElementType();
        }
        if (argumentType == null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
        return parameterType.IsAssignableFrom(argumentType)
               || Nullable.GetUnderlyingType(parameterType) == argumentType;
    }
    /// <summary>
    /// 验证类型可通过公共构造函数创建实例。
    /// </summary>
    /// <param name="type">待验证的类型。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当类型是接口、抽象类、开放泛型、指针、引用或类似引用的类型时抛出。</exception>
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
    /// <summary>
    /// 判断类型是否具有类似引用的存储限制。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <returns>类型为类似引用的类型时为 true，否则为 false。</returns>
    private static bool IsByRefLikeType(Type type)
    {
#if NET6_0_OR_GREATER
        return type.IsByRefLike;
#else
        return typeof(Type).GetProperty("IsByRefLike")?.GetValue(type) is true;
#endif
    }
    /// <summary>
    /// 根据实参类型或 null 模式取得唯一兼容构造函数。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <param name="argumentTypes">构造实参的运行时类型序列。</param>
    /// <returns>唯一兼容的构造函数。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">目标类型不可实例化，或不存在兼容构造函数时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法确定唯一优先项的兼容构造函数时抛出。</exception>
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
