using System.Reflection;

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
    Out
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
            || !AreSignatureTypesEquivalent(candidate.ReturnType, method.ReturnType))
            return false;

        var candidateParameters = candidate.GetParameters();
        var parameters = method.GetParameters();
        if (candidateParameters.Length != parameters.Length)
            return false;

        for (var index = 0; index < parameters.Length; index++)
        {
            if (candidateParameters[index].IsOut != parameters[index].IsOut
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
    /// <returns>唯一匹配的方法；未匹配时返回 null。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="method"/> 为 null 时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个签名兼容方法时抛出。</exception>
    public static MethodInfo FindMethod(Type type, MethodInfo method)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var candidates = GetMethodCandidates(type, DefaultMethodBindingFlags)
            .Where(candidate => IsSignatureCompatible(candidate, method))
            .ToArray();
        if (candidates.Length == 0)
            return null;
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
    /// <returns>唯一匹配的方法；未匹配时返回 null。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterTypes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空或参数类型包含 null 时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    public static MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes) =>
        FindMethod(type, name, DefaultMethodBindingFlags, parameterTypes);

    /// <summary>
    /// 按名称、绑定标志和普通参数类型查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="parameterTypes">参数类型。</param>
    /// <returns>唯一匹配的方法；未匹配时返回 null。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterTypes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空或参数类型包含 null 时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
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
    /// <returns>唯一匹配的方法定义；未匹配时返回 null。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、泛型参数数量或参数签名无效时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    public static MethodInfo FindMethod(Type type, string name, int genericParameterCount, params MethodParameterSignature[] parameterSignatures) =>
        FindMethod(type, name, DefaultMethodBindingFlags, genericParameterCount, parameterSignatures);

    /// <summary>
    /// 按名称、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法定义；未匹配时返回 null。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="parameterSignatures"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当名称、泛型参数数量或参数签名无效时抛出。</exception>
    /// <exception cref="AmbiguousMatchException">当存在多个匹配方法时抛出。</exception>
    public static MethodInfo FindMethod(Type type, string name, BindingFlags bindingFlags, int genericParameterCount,
        params MethodParameterSignature[] parameterSignatures)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("方法名称不能为空", nameof(name));
        if (genericParameterCount < 0)
            throw new ArgumentException("泛型参数数量不能小于 0", nameof(genericParameterCount));
        if (parameterSignatures == null)
            throw new ArgumentNullException(nameof(parameterSignatures));
        if (parameterSignatures.Any(signature => signature == null))
            throw new ArgumentException("参数签名不能包含 null", nameof(parameterSignatures));

        var comparison = (bindingFlags & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var candidates = GetMethodCandidates(type, bindingFlags)
            .Where(candidate => string.Equals(candidate.Name, name, comparison))
            .Where(candidate => candidate.GetGenericArguments().Length == genericParameterCount)
            .Where(candidate => IsParameterSignatureCompatible(candidate, parameterSignatures))
            .ToArray();
        if (candidates.Length == 0)
            return null;
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
    private const BindingFlags DefaultMethodBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// 获取符合绑定标志的方法候选项。
    /// </summary>
    /// <param name="type">待查找类型。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <returns>去重后的方法候选项。</returns>
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
    /// 判断方法参数是否与请求参数签名兼容。
    /// </summary>
    /// <param name="method">候选方法。</param>
    /// <param name="parameterSignatures">请求参数签名。</param>
    /// <returns>参数数量、传递方式和类型均匹配时返回 true；否则返回 false。</returns>
    private static bool IsParameterSignatureCompatible(MethodInfo method, IReadOnlyList<MethodParameterSignature> parameterSignatures)
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

        return true;
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
        return parameter.IsOut ? ParameterPassingKind.Out : ParameterPassingKind.Ref;
    }

    /// <summary>
    /// 将候选参数类型与请求类型进行严格结构匹配。
    /// </summary>
    /// <param name="candidateType">候选方法声明类型。</param>
    /// <param name="requestedType">请求类型。</param>
    /// <param name="genericBindings">方法泛型参数绑定。</param>
    /// <returns>类型结构匹配时返回 true；否则返回 false。</returns>
    private static bool TryMatchSignatureType(Type candidateType, Type requestedType, IDictionary<int, Type> genericBindings)
    {
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
            method.ReturnType.AssemblyQualifiedName, "|",
            string.Join(";", method.GetParameters().Select(parameter => $"{parameter.IsOut}:{parameter.ParameterType.AssemblyQualifiedName}")));
    }
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
    /// <returns>唯一匹配的方法；未匹配时返回 null。</returns>
    public static MethodInfo FindMethod(this Type type, MethodInfo method) => TypeVisit.FindMethod(type, method);

    /// <summary>
    /// 按名称和普通参数类型查找非泛型、按值传递的方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="parameterTypes">参数类型。</param>
    /// <returns>唯一匹配的方法；未匹配时返回 null。</returns>
    public static MethodInfo FindMethod(this Type type, string name, params Type[] parameterTypes) => TypeVisit.FindMethod(type, name, parameterTypes);

    /// <summary>
    /// 按名称、绑定标志、泛型参数数量和参数签名查找方法。
    /// </summary>
    /// <param name="type">待查找的目标类型。</param>
    /// <param name="name">方法名称。</param>
    /// <param name="bindingFlags">方法查找绑定标志。</param>
    /// <param name="genericParameterCount">方法泛型参数数量。</param>
    /// <param name="parameterSignatures">参数签名。</param>
    /// <returns>唯一匹配的方法定义；未匹配时返回 null。</returns>
    public static MethodInfo FindMethod(this Type type, string name, BindingFlags bindingFlags, int genericParameterCount,
        params MethodParameterSignature[] parameterSignatures) => TypeVisit.FindMethod(type, name, bindingFlags, genericParameterCount, parameterSignatures);
}
