using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Bing.Reflection;

/// <summary>
/// 方法参数传递方式。
/// </summary>
public enum ParameterPassingKind
{
    /// <summary>
    /// 按值传递。
    /// </summary>
    Value,

    /// <summary>
    /// 按引用传递。
    /// </summary>
    Ref,

    /// <summary>
    /// 输出参数。
    /// </summary>
    Out,

    /// <summary>只读引用参数（in，包括元数据上标记为输入的引用参数）。</summary>
    In
}

/// <summary>
/// 方法参数签名。
/// </summary>
public sealed class MethodParameterSignature
{
    /// <summary>
    /// 初始化方法参数签名。
    /// </summary>
    /// <param name="parameterType">参数元素类型。</param>
    /// <param name="passingKind">参数传递方式。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="parameterType"/> 为 null 时抛出。</exception>
    public MethodParameterSignature(Type parameterType, ParameterPassingKind passingKind = ParameterPassingKind.Value)
    {
        ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
        if (passingKind != ParameterPassingKind.Value && passingKind != ParameterPassingKind.Ref
            && passingKind != ParameterPassingKind.Out && passingKind != ParameterPassingKind.In)
            throw new ArgumentOutOfRangeException(nameof(passingKind), passingKind, "无效的参数传递方式。");
        PassingKind = passingKind;
    }

    /// <summary>
    /// 获取参数元素类型。
    /// </summary>
    public Type ParameterType { get; }

    /// <summary>
    /// 获取参数传递方式。
    /// </summary>
    public ParameterPassingKind PassingKind { get; }
}

/// <summary>
/// 类型访问器。
/// </summary>
public static partial class TypeVisit
{
    internal const int NamedMethodCacheCapacity = 1024;

    private sealed class NamedMethodLookup
    {
        internal readonly ConcurrentDictionary<string, Lazy<MethodInfo>> Methods = new();
        internal readonly object AdmissionGate = new();
    }

    private static readonly ConditionalWeakTable<Type, NamedMethodLookup> NamedMethodCache = new();
    /// <summary>
    /// 判断候选方法是否与给定方法的签名兼容。
    /// </summary>
    /// <param name="candidate">待比较的候选方法。</param>
    /// <param name="method">作为签名依据的方法。</param>
    /// <returns>方法名称、静态性、泛型参数数量、返回类型和参数类型均兼容时返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="candidate"/> 或 <paramref name="method"/> 为 null 时抛出。</exception>
    public static bool IsSignatureCompatible(MethodInfo candidate, MethodInfo method)
    {
        if (candidate == null)
            throw new ArgumentNullException(nameof(candidate));
        if (method == null)
            throw new ArgumentNullException(nameof(method));
        if (!string.Equals(candidate.Name, method.Name, StringComparison.Ordinal)
            || candidate.IsStatic != method.IsStatic
            || candidate.GetGenericArguments().Length != method.GetGenericArguments().Length
            || !AreSignatureTypesEquivalent(candidate.ReturnType, method.ReturnType)
            || !HaveSameCustomModifiers(candidate.ReturnParameter, method.ReturnParameter))
            return false;

        var candidateParameters = candidate.GetParameters();
        var parameters = method.GetParameters();
        if (candidateParameters.Length != parameters.Length)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (GetPassingKind(candidateParameters[index]) != GetPassingKind(parameters[index])
                || !HaveSameCustomModifiers(candidateParameters[index], parameters[index])
                || !AreSignatureTypesEquivalent(candidateParameters[index].ParameterType, parameters[index].ParameterType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 在目标类型中查找唯一的签名兼容方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="method">作为签名依据的方法。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="method"/> 为 null 时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到签名兼容的方法时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个签名兼容方法时抛出。</exception>
    /// <remarks>该入口按运行时元数据扫描候选方法；裁剪应用必须保留目标类型的方法和接口元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethod(Type type, MethodInfo method)
        => GetMethodBySignature(type, method);

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    private static MethodInfo ResolveMethodBySignature(Type type, MethodInfo method)
    {
        System.Threading.Interlocked.Increment(ref _methodSignatureScanCount);
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var candidates = GetMethodCandidates(type, DefaultMethodBindingFlags)
            .Where(candidate => IsSignatureCompatible(candidate, method))
            .ToArray();
        if (candidates.Length == 0)
            throw new MissingMethodException($"类型“{type.FullName}”中未找到与方法“{GetFullyQualifiedName(method)}”签名兼容的方法。");
        if (candidates.Length > 1)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个与方法“{GetFullyQualifiedName(method)}”签名兼容的候选项。");
        return candidates[0];
    }

    /// <summary>
    /// 按名称和普通参数类型查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="parameterTypes">参数类型。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterTypes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空或参数类型包含 null 时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    /// <remarks>该入口按运行时元数据扫描候选方法；裁剪应用必须保留目标类型的方法和接口元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes) =>
        FindMethod(type, name, DefaultMethodBindingFlags, parameterTypes);

    /// <summary>
    /// 按名称、绑定标志和普通参数类型查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="parameterTypes">参数类型。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterTypes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空或参数类型包含 null 时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    /// <remarks>显式绑定标志可能包含非公开方法；裁剪时必须保留请求范围内的方法元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留请求范围内的方法元数据。")]
#endif
    public static MethodInfo FindMethod(Type type, string name, BindingFlags bindingFlags, params Type[] parameterTypes)
    {
        if (parameterTypes == null)
            throw new ArgumentNullException(nameof(parameterTypes));
        if (parameterTypes.Any(parameterType => parameterType == null))
            throw new ArgumentException("参数类型不能包含 null", nameof(parameterTypes));

        return FindMethod(type, name, bindingFlags, 0,
            parameterTypes.Select(parameterType => new MethodParameterSignature(parameterType)).ToArray());
    }

    /// <summary>
    /// 按名称、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法定义。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、泛型参数数量或参数签名无效时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法定义时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    /// <remarks>该入口按运行时元数据扫描候选方法；裁剪应用必须保留目标类型的方法和接口元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethod(Type type, string name, int genericParameterCount, params MethodParameterSignature[] parameterSignatures) =>
        FindMethod(type, name, DefaultMethodBindingFlags, genericParameterCount, parameterSignatures);

    /// <summary>
    /// 按名称、返回类型和普通参数签名查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="returnType">返回类型。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/>、<paramref name="returnType"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、参数签名或返回类型无效时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethodByReturnType(Type type, string name, Type returnType,
        params MethodParameterSignature[] parameterSignatures) =>
        FindMethodByReturnType(type, name, returnType, DefaultMethodBindingFlags, 0, parameterSignatures);

    /// <summary>
    /// 按名称、返回类型、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="returnType">返回类型。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/>、<paramref name="returnType"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、泛型参数数量或参数签名无效时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethodByReturnType(Type type, string name, Type returnType,
        BindingFlags bindingFlags, int genericParameterCount, params MethodParameterSignature[] parameterSignatures)
    {
        if (returnType == null)
            throw new ArgumentNullException(nameof(returnType));
        return FindNamedMethod(type, name, bindingFlags, genericParameterCount, returnType, parameterSignatures);
    }

    /// <summary>
    /// 按名称、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法定义。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、泛型参数数量或参数签名无效时抛出。</exception>
    /// <exception cref="MissingMethodException">未找到匹配的方法定义时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    /// <remarks>显式绑定标志可能包含非公开方法；裁剪时必须保留请求范围内的方法元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留请求范围内的方法元数据。")]
#endif
    public static MethodInfo FindMethod(Type type, string name, BindingFlags bindingFlags, int genericParameterCount,
        params MethodParameterSignature[] parameterSignatures)
    {
        return FindNamedMethod(type, name, bindingFlags, genericParameterCount, null, parameterSignatures);
    }

    private static MethodInfo FindNamedMethod(Type type, string name, BindingFlags bindingFlags,
        int genericParameterCount, Type returnType, MethodParameterSignature[] parameterSignatures)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (name == null || name.Length == 0 || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("方法名称不能为空", nameof(name));
        if (genericParameterCount < 0)
            throw new ArgumentException("泛型参数数量不能小于 0", nameof(genericParameterCount));
        if (parameterSignatures == null)
            throw new ArgumentNullException(nameof(parameterSignatures));
        if (parameterSignatures.Any(signature => signature == null))
            throw new ArgumentException("参数签名不能包含 null", nameof(parameterSignatures));

        var signatures = parameterSignatures.ToArray();
        if (ReflectionPackageCacheScope.IsCollectible(type) || name.Length > 1024
            || (returnType != null && ReflectionPackageCacheScope.IsCollectible(returnType))
            || signatures.Any(signature => ReflectionPackageCacheScope.IsCollectible(signature.ParameterType)))
        {
            ReflectionCacheMetrics.RecordBypass();
            return ResolveNamedMethod(type, name, bindingFlags, genericParameterCount, returnType, signatures);
        }

        var normalizedName = (bindingFlags & BindingFlags.IgnoreCase) != 0 ? name.ToUpperInvariant() : name;
        var returnIdentity = returnType == null ? "<any>" : CreateTypeIdentity(returnType);
        var key = $"{(int)bindingFlags}:{genericParameterCount}:{normalizedName.Length}:{normalizedName}:" +
                  $"r:{returnIdentity.Length}:{returnIdentity}:" +
                  string.Concat(signatures.Select(signature =>
                  {
                      var identity = CreateTypeIdentity(signature.ParameterType);
                      return $"{(int)signature.PassingKind}:{identity.Length}:{identity};";
                  }));
        if (key.Length > 8192)
        {
            ReflectionCacheMetrics.RecordStructuralBypass();
            ReflectionCacheMetrics.RecordBypass();
            return ResolveNamedMethod(type, name, bindingFlags, genericParameterCount, returnType, signatures);
        }
        var lookup = NamedMethodCache.GetValue(type, _ => new NamedMethodLookup());
        if (lookup.Methods.TryGetValue(key, out var existing))
        {
            ReflectionCacheMetrics.Record(existing);
            return GetCachedNamedMethod(lookup, key, existing);
        }

        Lazy<MethodInfo> lazy = null;
        var bypass = false;
        lock (lookup.AdmissionGate)
        {
            if (lookup.Methods.TryGetValue(key, out existing))
            {
                ReflectionCacheMetrics.Record(existing);
                lazy = existing;
            }
            else if (lookup.Methods.Count >= NamedMethodCacheCapacity)
            {
                ReflectionCacheMetrics.RecordStructuralBypass();
                ReflectionCacheMetrics.RecordBypass();
                bypass = true;
            }
            else
            {
                lazy = ReflectionCacheMetrics.CreateLazy(() =>
                    ResolveNamedMethod(type, name, bindingFlags, genericParameterCount, returnType, signatures));
                lookup.Methods.TryAdd(key, lazy);
                ReflectionCacheMetrics.RecordMiss();
            }
        }

        if (bypass)
            return ResolveNamedMethod(type, name, bindingFlags, genericParameterCount, returnType, signatures);
        return GetCachedNamedMethod(lookup, key, lazy);
    }

    private static MethodInfo GetCachedNamedMethod(NamedMethodLookup lookup, string key, Lazy<MethodInfo> lazy)
    {
        var method = lazy.Value;
        if (!ReflectionPackageCacheScope.IsCollectible(method))
            return method;

        // 方法返回类型或签名可能引用 collectible 元数据；解析结果不能被非 collectible 宿主的强缓存持有。
        ((ICollection<KeyValuePair<string, Lazy<MethodInfo>>>)lookup.Methods)
            .Remove(new KeyValuePair<string, Lazy<MethodInfo>>(key, lazy));
        ReflectionCacheMetrics.RecordBypass();
        return method;
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("命名方法按运行时绑定标志扫描，裁剪时必须保留请求范围内的方法和接口元数据。")]
#endif
    private static MethodInfo ResolveNamedMethod(Type type, string name, BindingFlags bindingFlags,
        int genericParameterCount, Type returnType, MethodParameterSignature[] parameterSignatures)
    {
        var comparison = (bindingFlags & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var candidates = GetMethodCandidates(type, bindingFlags)
            .Where(candidate => string.Equals(candidate.Name, name, comparison))
            .Where(candidate => candidate.GetGenericArguments().Length == genericParameterCount)
            .Where(candidate => IsParameterSignatureCompatible(candidate, returnType, parameterSignatures))
            .ToArray();
        if (candidates.Length == 0)
            throw new MissingMethodException($"类型“{type.FullName}”中未找到名称为“{name}”的匹配方法。");
        if (candidates.Length > 1)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个名称为“{name}”的匹配方法。");
        return candidates[0];
    }

    /// <summary>
    /// 判断两个类型是否在方法签名中等价。
    /// </summary>
    /// <param name="left">第一个类型。</param>
    /// <param name="right">第二个类型。</param>
    /// <returns>两个类型在泛型参数、数组、引用参数和泛型实参层面等价时返回 true；否则返回 false。</returns>
    private static bool AreSignatureTypesEquivalent(Type left, Type right)
    {
        if (left.IsGenericParameter || right.IsGenericParameter)
        {
            return left.IsGenericParameter
                   && right.IsGenericParameter
                   && left.GenericParameterPosition == right.GenericParameterPosition
                   && left.DeclaringMethod != null == (right.DeclaringMethod != null);
        }

        if (left.IsByRef || right.IsByRef)
        {
            return left.IsByRef && right.IsByRef
                                && AreSignatureTypesEquivalent(left.GetElementType()!, right.GetElementType()!);
        }

        if (left.IsArray || right.IsArray)
        {
            return left.IsArray && right.IsArray
                                && left.GetArrayRank() == right.GetArrayRank()
                                && IsVectorArray(left) == IsVectorArray(right)
                                && AreSignatureTypesEquivalent(left.GetElementType()!, right.GetElementType()!);
        }

        if (left.IsGenericType || right.IsGenericType)
        {
            if (!left.IsGenericType || !right.IsGenericType
                                    || left.GetGenericTypeDefinition() != right.GetGenericTypeDefinition())
                return false;

            var leftArguments = left.GetGenericArguments();
            var rightArguments = right.GetGenericArguments();
            return leftArguments.Length == rightArguments.Length
                   && leftArguments.Zip(rightArguments, AreSignatureTypesEquivalent).All(result => result);
        }

        return left == right;
    }

    /// <summary>
    /// 默认方法查找绑定标志。
    /// </summary>
    private const BindingFlags DefaultMethodBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// 获取符合绑定标志的方法候选项。
    /// </summary>
    /// <param name="type">待查找类型。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <returns>去重后的方法候选项。</returns>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法候选项按运行时绑定标志扫描，裁剪时必须保留请求范围内的方法和接口元数据。")]
#endif
    private static IEnumerable<MethodInfo> GetMethodCandidates(Type type, BindingFlags bindingFlags)
    {
        if (!type.IsInterface || (bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
            return type.GetMethods(bindingFlags);

        var interfaces = new[] { type }.Concat(type.GetInterfaces()).Distinct().OrderBy(item => item.FullName, StringComparer.Ordinal);
        return interfaces.SelectMany(interfaceType => interfaceType.GetMethods(bindingFlags | BindingFlags.DeclaredOnly))
            .GroupBy(CreateMethodIdentity, StringComparer.Ordinal)
            .Select(group => group.OrderBy(method => method.DeclaringType?.FullName, StringComparer.Ordinal).First());
    }

    /// <summary>
    /// 判断候选方法的参数和可选返回类型是否与请求签名兼容。
    /// </summary>
    /// <param name="method">候选方法。</param>
    /// <param name="returnType">请求返回类型；为 null 时不限制返回类型。</param>
    /// <param name="parameterSignatures">请求参数签名。</param>
    /// <returns>签名兼容时返回 true；否则返回 false。</returns>
    private static bool IsParameterSignatureCompatible(MethodInfo method, Type returnType,
        IReadOnlyList<MethodParameterSignature> parameterSignatures)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != parameterSignatures.Count)
            return false;

        var genericBindings = new Dictionary<int, Type>();
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            var signature = parameterSignatures[index];
            if (GetPassingKind(parameter) != signature.PassingKind)
                return false;

            var parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;
            if (!TryMatchSignatureType(parameterType, signature.ParameterType, genericBindings))
                return false;
        }

        if (returnType == null)
            return true;

        return TryMatchSignatureType(method.ReturnType, returnType, genericBindings);
    }

    /// <summary>
    /// 获取参数传递方式。
    /// </summary>
    /// <param name="parameter">参数元数据。</param>
    /// <returns>参数传递方式。</returns>
    private static ParameterPassingKind GetPassingKind(ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsByRef)
            return ParameterPassingKind.Value;
        return parameter.IsOut ? ParameterPassingKind.Out
            : parameter.IsIn || HasReadOnlyRequiredModifier(parameter)
                ? ParameterPassingKind.In
                : ParameterPassingKind.Ref;
    }

    private static bool HasReadOnlyRequiredModifier(ParameterInfo parameter) =>
        parameter.GetRequiredCustomModifiers().Any(modifier =>
            string.Equals(modifier.FullName,
                "System.Runtime.CompilerServices.IsReadOnlyAttribute",
                StringComparison.Ordinal));

    /// <summary>
    /// 将候选参数类型与请求类型进行严格结构匹配。
    /// </summary>
    /// <param name="candidateType">候选方法声明类型。</param>
    /// <param name="requestedType">请求类型。</param>
    /// <param name="genericBindings">方法泛型参数绑定。</param>
    /// <returns>类型结构匹配时返回 true；否则返回 false。</returns>
    private static bool TryMatchSignatureType(Type candidateType, Type requestedType, IDictionary<int, Type> genericBindings)
    {
        if (candidateType.IsByRef || requestedType.IsByRef)
        {
            return candidateType.IsByRef && requestedType.IsByRef
                                 && TryMatchSignatureType(candidateType.GetElementType()!, requestedType.GetElementType()!, genericBindings);
        }

        if (candidateType.IsGenericParameter && candidateType.DeclaringMethod != null)
        {
            if (genericBindings.TryGetValue(candidateType.GenericParameterPosition, out var existing))
                return AreSignatureTypesEquivalent(existing, requestedType);
            genericBindings[candidateType.GenericParameterPosition] = requestedType;
            return true;
        }

        if (candidateType.IsArray || requestedType.IsArray)
        {
            return candidateType.IsArray && requestedType.IsArray
                                         && candidateType.GetArrayRank() == requestedType.GetArrayRank()
                                         && IsVectorArray(candidateType) == IsVectorArray(requestedType)
                                         && TryMatchSignatureType(candidateType.GetElementType(), requestedType.GetElementType(), genericBindings);
        }

        if (candidateType.IsGenericType || requestedType.IsGenericType)
        {
            if (!candidateType.IsGenericType || !requestedType.IsGenericType
                                             || candidateType.GetGenericTypeDefinition() != requestedType.GetGenericTypeDefinition())
                return false;
            var candidateArguments = candidateType.GetGenericArguments();
            var requestedArguments = requestedType.GetGenericArguments();
            return candidateArguments.Length == requestedArguments.Length
                   && candidateArguments.Zip(requestedArguments, (candidate, requested) => TryMatchSignatureType(candidate, requested, genericBindings)).All(result => result);
        }

        return AreSignatureTypesEquivalent(candidateType, requestedType);
    }

    /// <summary>
    /// 创建方法身份，用于合并菱形接口继承产生的重复成员。
    /// </summary>
    /// <param name="method">方法元数据。</param>
    /// <returns>方法结构身份。</returns>
    private static string CreateMethodIdentity(MethodInfo method)
    {
        return string.Concat(method.Name, "|", method.IsStatic, "|", method.GetGenericArguments().Length, "|",
            CreateTypeIdentity(method.ReturnType), ":", CreateModifierIdentity(method.ReturnParameter), "|",
            string.Join(";", method.GetParameters().Select(parameter =>
                $"{(int)GetPassingKind(parameter)}:{CreateTypeIdentity(parameter.ParameterType)}:{CreateModifierIdentity(parameter)}")));
    }

    private static bool HaveSameCustomModifiers(ParameterInfo left, ParameterInfo right) =>
        left.GetRequiredCustomModifiers().SequenceEqual(right.GetRequiredCustomModifiers())
        && left.GetOptionalCustomModifiers().SequenceEqual(right.GetOptionalCustomModifiers());

    private static string CreateModifierIdentity(ParameterInfo parameter) =>
        "required[" + string.Join(",", parameter.GetRequiredCustomModifiers().Select(CreateTypeIdentity))
        + "]optional[" + string.Join(",", parameter.GetOptionalCustomModifiers().Select(CreateTypeIdentity)) + "]";

    private static string CreateTypeIdentity(Type type)
    {
        if (type.IsGenericParameter)
            return $"!{(type.DeclaringMethod == null ? "T" : "M")}{type.GenericParameterPosition}";
        if (type.HasElementType)
            return $"{CreateTypeIdentity(type.GetElementType()!)}:{(type.IsArray ? IsVectorArray(type) ? "szarray" : $"array{type.GetArrayRank()}" : type.IsByRef ? "ref" : "ptr")}";
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
            return $"{CreateTypeIdentity(type.GetGenericTypeDefinition())}[{string.Join(",", type.GetGenericArguments().Select(CreateTypeIdentity))}]";
        return $"{type.AssemblyQualifiedName}@{type.TypeHandle.Value.ToInt64()}";
    }

    private static bool IsVectorArray(Type type) => type.IsArray && type.GetArrayRank() == 1
        && type == type.GetElementType()!.MakeArrayType();
}

/// <summary>
/// 类型元数据访问器扩展。
/// </summary>
public static partial class TypeMetaVisitExtensions
{
    /// <summary>
    /// 判断候选方法是否与给定方法的签名兼容。
    /// </summary>
    /// <param name="candidate">待比较的候选方法。</param>
    /// <param name="method">作为签名依据的方法。</param>
    /// <returns>方法签名兼容时返回 true；否则返回 false。</returns>
    public static bool IsSignatureCompatible(this MethodInfo candidate, MethodInfo method) =>
        TypeVisit.IsSignatureCompatible(candidate, method);

    /// <summary>
    /// 在目标类型中查找唯一的签名兼容方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="method">作为签名依据的方法。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="MissingMethodException">未找到签名兼容的方法时抛出。</exception>
    /// <remarks>该入口按运行时元数据扫描候选方法；裁剪应用必须保留目标类型的方法和接口元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethod(this Type type, MethodInfo method) => TypeVisit.FindMethod(type, method);

    /// <summary>
    /// 按名称和普通参数类型查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="parameterTypes">参数类型。</param>
    /// <returns>唯一匹配的方法。</returns>
    /// <exception cref="MissingMethodException">未找到匹配的方法时抛出。</exception>
    /// <remarks>该入口按运行时元数据扫描候选方法；裁剪应用必须保留目标类型的方法和接口元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethod(this Type type, string name, params Type[] parameterTypes) => TypeVisit.FindMethod(type, name, parameterTypes);

    /// <summary>
    /// 按名称、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法定义。</returns>
    /// <exception cref="MissingMethodException">未找到匹配的方法定义时抛出。</exception>
    /// <remarks>显式绑定标志可能包含非公开方法；裁剪时必须保留请求范围内的方法元数据。</remarks>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留请求范围内的方法元数据。")]
#endif
    public static MethodInfo FindMethod(this Type type, string name, BindingFlags bindingFlags, int genericParameterCount,
        params MethodParameterSignature[] parameterSignatures) => TypeVisit.FindMethod(type, name, bindingFlags, genericParameterCount, parameterSignatures);

    /// <summary>
    /// 按名称、返回类型和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="returnType">返回类型。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法。</returns>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethodByReturnType(this Type type, string name, Type returnType,
        params MethodParameterSignature[] parameterSignatures) =>
        TypeVisit.FindMethodByReturnType(type, name, returnType, parameterSignatures);

    /// <summary>
    /// 按名称、返回类型、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="returnType">返回类型。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法。</returns>
#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("方法签名按运行时元数据扫描，裁剪时必须保留目标类型的方法和接口元数据。")]
#endif
    public static MethodInfo FindMethodByReturnType(this Type type, string name, Type returnType,
        BindingFlags bindingFlags, int genericParameterCount, params MethodParameterSignature[] parameterSignatures) =>
        TypeVisit.FindMethodByReturnType(type, name, returnType, bindingFlags, genericParameterCount, parameterSignatures);
}
