using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ReflectionTrimProbe.UnrootedModelsHost;

public static class Program
{
    private const string TypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedBoundaryModel";
    private const string CompositeAssemblyName = "ReflectionTrimProbe.PluginModels";
    private const string TertiaryAssemblyName = "ReflectionTrimProbe.TertiaryModels";
    private const string QuaternaryAssemblyName = "ReflectionTrimProbe.QuaternaryModels";
    private const string QuinaryAssemblyName = "ReflectionTrimProbe.QuinaryModels";

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察裁剪后的成员边界。")]
    public static int Main(string[] args)
    {
        if (args.Length > 2
            || (args.Length > 0
                && args[0] is not ("assembly-trimmed" or "member-trimmed" or "metadata-preserved" or "--root-modes"))
            || (args.Length == 2
                && (args[0] != "--root-modes"
                    || args[1] is not ("assembly-trimmed" or "member-trimmed" or "metadata-preserved"))))
        {
            Console.Error.WriteLine("Usage: ReflectionTrimProbe.UnrootedModelsHost [<assembly-trimmed|member-trimmed|metadata-preserved>] | --root-modes [<expected-boundary>]");
            return 2;
        }

        // 只通过程序集名称和类型名称触发加载，故意不静态引用业务类型，让裁剪器独立决定成员元数据保留范围。
        var assembly = Assembly.Load(new AssemblyName("ReflectionTrimProbe.UnrootedModels"));
        var assemblyName = assembly.GetName().Name;
#pragma warning disable IL2026, IL2075 // 探针故意观察未保留类型的运行时成员边界。
        var type = assembly.GetType(TypeName, throwOnError: false);
        var memberCount = type?.GetMembers(
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly).Length ?? 0;
#pragma warning restore IL2026, IL2075
        var boundary = type is null
            ? "assembly-trimmed"
            : memberCount == 0
                ? "member-trimmed"
                : "metadata-preserved";

        Console.WriteLine($"reflection-plugin-trim-host:{assemblyName}");
        Console.WriteLine($"reflection-plugin-trim-boundary:{boundary}");
        var rootModesRequested = args.Any(argument => argument == "--root-modes");
        if (rootModesRequested)
        {
            VerifyRootModeMatrix(assembly);
            VerifyCompositeRootModeMatrix();
            VerifyTertiaryRootModeMatrix();
            VerifyQuaternaryRootModeMatrix();
            VerifyQuinaryRootModeMatrix();
        }

        var expectedBoundary = args.FirstOrDefault(argument => argument is "assembly-trimmed" or "member-trimmed" or "metadata-preserved");
        return string.Equals(assemblyName, "ReflectionTrimProbe.UnrootedModels", StringComparison.Ordinal)
            && (expectedBoundary is null || string.Equals(boundary, expectedBoundary, StringComparison.Ordinal))
            ? 0
            : 1;
    }

#pragma warning disable IL2026, IL2070, IL2075 // 探针故意观察不同 preserve 模式下的运行时成员边界。
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察 preserve 模式下的成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察 preserve 模式下的成员边界。")]
    private static void VerifyRootModeMatrix(Assembly assembly)
    {
        const string modelNamespace = "ReflectionTrimProbe.UnrootedModels";
        VerifyRootMode(assembly, modelNamespace, "FieldsOnlyRootModel", "PreservedField");
        VerifyRootMode(assembly, modelNamespace, "PropertiesOnlyRootModel", "PreservedProperty");
        VerifyRootMode(assembly, modelNamespace, "MethodsOnlyRootModel", "PreservedMethod");
        VerifyRootMode(assembly, modelNamespace, "NothingRootModel", null, "TrimmedField", "TrimmedProperty", "TrimmedMethod");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过第二程序集的类型名称观察 linker 根组合边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察第二程序集的 preserve 模式。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察第二程序集的 preserve 模式。")]
    private static void VerifyCompositeRootModeMatrix()
    {
        var assembly = Assembly.Load(new AssemblyName(CompositeAssemblyName));
        const string modelNamespace = "ReflectionTrimProbe.PluginModels";
        VerifyRootMode(assembly, modelNamespace, "CompositeFieldsRootModel", "PreservedField");
        VerifyRootMode(assembly, modelNamespace, "CompositePropertiesRootModel", "PreservedProperty");
        VerifyRootMode(assembly, modelNamespace, "CompositeMethodsRootModel", "PreservedMethod");
        VerifyRootMode(assembly, modelNamespace, "CompositeNothingRootModel", null, "TrimmedField", "TrimmedProperty", "TrimmedMethod");
        VerifyRootMode(assembly, modelNamespace, "CompositeGenericRootModel`1", "PreservedField");
        VerifyMember(assembly, modelNamespace, "CompositeGenericRootModel`1", "PreservedProperty");
        VerifyMember(assembly, modelNamespace, "CompositeGenericRootModel`1", "PreservedMethod");
        VerifyConstrainedGenericRootMode(assembly, modelNamespace);
        VerifyRootMode(assembly, modelNamespace, "CompositeNestedRootModel+PreservedNested", "PreservedField");
        VerifyRootMode(assembly, modelNamespace, "CompositeCrossAssemblyBase", "PreservedBaseField", "TrimmedBaseField");
        VerifyRootMode(assembly, modelNamespace, "CompositeCrossAssemblyPayload", "PreservedPayloadField", "TrimmedPayloadField");
        VerifySignatureRootMode(assembly, modelNamespace);
        Console.WriteLine("reflection-plugin-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察受约束泛型 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察受约束泛型成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察受约束泛型成员边界。")]
    private static void VerifyConstrainedGenericRootMode(Assembly assembly, string modelNamespace)
    {
        var type = assembly.GetType(
            $"{modelNamespace}.CompositeConstrainedGenericRootModel`1",
            throwOnError: false)
            ?? throw new InvalidOperationException("Constrained generic root type was removed.");
        var genericParameter = type.GetGenericArguments().Single();
        const GenericParameterAttributes requiredConstraints =
            GenericParameterAttributes.ReferenceTypeConstraint
            | GenericParameterAttributes.DefaultConstructorConstraint;
        if ((genericParameter.GenericParameterAttributes & requiredConstraints) != requiredConstraints)
            throw new InvalidOperationException("Constrained generic parameter metadata was removed.");

        VerifyMember(assembly, modelNamespace, "CompositeConstrainedGenericRootModel`1", "PreservedProperty");
        VerifyMember(assembly, modelNamespace, "CompositeConstrainedGenericRootModel`1", "PreservedMethod");
        if (FindMember(type, "TrimmedMethod", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected constrained generic trimmed method is present.");

        Console.WriteLine("reflection-plugin-constrained-generic-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过第三程序集的类型名称观察多插件 linker 根组合边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察第三程序集的 preserve 模式。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察第三程序集的 preserve 模式。")]
    private static void VerifyTertiaryRootModeMatrix()
    {
        var assembly = Assembly.Load(new AssemblyName(TertiaryAssemblyName));
        const string modelNamespace = "ReflectionTrimProbe.TertiaryModels";
        VerifyRootMode(assembly, modelNamespace, "TertiaryFieldsRootModel", "PreservedField");
        VerifyRootMode(assembly, modelNamespace, "TertiaryPropertiesRootModel", "PreservedProperty");
        VerifyRootMode(assembly, modelNamespace, "TertiaryNothingRootModel", null, "TrimmedField", "TrimmedProperty", "TrimmedMethod");
        VerifyCrossAssemblyNestedRootMode(assembly);
        Console.WriteLine("reflection-plugin-tertiary-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过第四程序集的类型名称观察多插件 linker 根组合边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察第四程序集的 preserve 模式。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察第四程序集的成员边界。")]
    private static void VerifyQuaternaryRootModeMatrix()
    {
        var assembly = Assembly.Load(new AssemblyName(QuaternaryAssemblyName));
        const string modelNamespace = "ReflectionTrimProbe.QuaternaryModels";
        VerifyRootMode(assembly, modelNamespace, "QuaternaryFieldsRootModel", "PreservedField");
        VerifyRootMode(assembly, modelNamespace, "QuaternaryMethodsRootModel", "PreservedMethod", "TrimmedMethod");
        VerifyRootMode(assembly, modelNamespace, "QuaternaryNestedRootModel+PreservedNested", "PreservedField", "TrimmedField");
        VerifyRootMode(assembly, modelNamespace, "QuaternaryNothingRootModel", null, "TrimmedField", "TrimmedProperty", "TrimmedMethod");
        VerifyQuaternaryCrossAssemblyRootMode(assembly);
        Console.WriteLine("reflection-plugin-quaternary-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察第四程序集的跨程序集 root 边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察第四程序集的跨程序集成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察第四程序集的跨程序集成员边界。")]
    private static void VerifyQuaternaryCrossAssemblyRootMode(Assembly quaternaryAssembly)
    {
        const string quaternaryNamespace = "ReflectionTrimProbe.QuaternaryModels";
        const string tertiaryNamespace = "ReflectionTrimProbe.TertiaryModels";
        var preservedNestedType = quaternaryAssembly.GetType(
            $"{quaternaryNamespace}.CrossAssemblyRootModel+PreservedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Quaternary cross-assembly preserved nested type was removed.");

        var preservedPayloadField = preservedNestedType.GetField(
            "PreservedTertiaryPayload",
            PublicInstanceFlags)
            ?? throw new InvalidOperationException("Expected preserved TertiaryModels field was removed.");
        var payloadType = preservedPayloadField.FieldType;
        if (!string.Equals(payloadType.Assembly.GetName().Name, TertiaryAssemblyName, StringComparison.Ordinal)
            || !string.Equals(payloadType.FullName, $"{tertiaryNamespace}.TertiaryFieldsRootModel", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Quaternary cross-assembly root resolved an unexpected TertiaryModels field type.");
        }

        if (FindMember(preservedNestedType, "PreservedNestedField", PublicInstanceFlags) is null)
            throw new InvalidOperationException("Expected preserved Quaternary nested field was removed.");

        var preservedMethod = preservedNestedType.GetMethods(PublicInstanceFlags)
            .FirstOrDefault(method => string.Equals(method.Name, "PreservedMethod", StringComparison.Ordinal)
                && method.ReturnType == typeof(int)
                && HasParameterTypes(method, typeof(int)));
        if (preservedMethod is null)
            throw new InvalidOperationException("Expected preserved Quaternary method signature was removed.");

        if (FindMember(preservedNestedType, "TrimmedNestedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed Quaternary nested field is present.");
        if (FindMember(preservedNestedType, "TrimmedMethod", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed Quaternary method is present.");

        var trimmedNestedType = quaternaryAssembly.GetType(
            $"{quaternaryNamespace}.CrossAssemblyRootModel+TrimmedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Quaternary cross-assembly nothing nested type was removed.");
        if (FindMember(trimmedNestedType, "TrimmedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected Quaternary nothing nested field is present.");
        if (FindMember(trimmedNestedType, "TrimmedMethod", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected Quaternary nothing nested method is present.");

        Console.WriteLine("reflection-plugin-quaternary-cross-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察第五程序集的跨分支 root 边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察第五程序集的跨分支成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察第五程序集的跨分支字段和成员边界。")]
    private static void VerifyQuinaryRootModeMatrix()
    {
        var assembly = Assembly.Load(new AssemblyName(QuinaryAssemblyName));
        const string modelNamespace = "ReflectionTrimProbe.QuinaryModels";
        var preservedNestedType = assembly.GetType(
            $"{modelNamespace}.CrossBranchRootModel+PreservedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Quinary cross-branch preserved nested type was removed.");

        var quaternaryPayloadField = preservedNestedType.GetField(
            "PreservedQuaternaryPayload",
            PublicInstanceFlags)
            ?? throw new InvalidOperationException("Expected preserved QuaternaryModels branch field was removed.");
        if (!string.Equals(quaternaryPayloadField.FieldType.Assembly.GetName().Name, QuaternaryAssemblyName, StringComparison.Ordinal)
            || !string.Equals(
                quaternaryPayloadField.FieldType.FullName,
                "ReflectionTrimProbe.QuaternaryModels.QuaternaryFieldsRootModel",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Quinary cross-branch root resolved an unexpected QuaternaryModels field type.");
        }

        var tertiaryPayloadField = preservedNestedType.GetField(
            "PreservedTertiaryPayload",
            PublicInstanceFlags)
            ?? throw new InvalidOperationException("Expected preserved TertiaryModels branch field was removed.");
        if (!string.Equals(tertiaryPayloadField.FieldType.Assembly.GetName().Name, TertiaryAssemblyName, StringComparison.Ordinal)
            || !string.Equals(
                tertiaryPayloadField.FieldType.FullName,
                "ReflectionTrimProbe.TertiaryModels.TertiaryFieldsRootModel",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Quinary cross-branch root resolved an unexpected TertiaryModels field type.");
        }

        if (FindMember(preservedNestedType, "PreservedNestedField", PublicInstanceFlags) is null)
            throw new InvalidOperationException("Expected preserved Quinary nested field was removed.");

        var preservedMethod = preservedNestedType.GetMethods(PublicInstanceFlags)
            .FirstOrDefault(method => string.Equals(method.Name, "PreservedMethod", StringComparison.Ordinal)
                && method.ReturnType == typeof(int)
                && HasParameterTypes(method, typeof(int)));
        if (preservedMethod is null)
            throw new InvalidOperationException("Expected preserved Quinary method signature was removed.");

        if (preservedNestedType.GetMethods(PublicInstanceFlags)
            .Any(method => string.Equals(method.Name, "PreservedMethod", StringComparison.Ordinal)
                && HasParameterTypes(method, typeof(string))))
        {
            throw new InvalidOperationException("Expected trimmed Quinary string overload is present.");
        }

        if (FindMember(preservedNestedType, "TrimmedNestedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed Quinary nested field is present.");
        if (FindMember(preservedNestedType, "TrimmedMethod", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed Quinary method is present.");

        var trimmedNestedType = assembly.GetType(
            $"{modelNamespace}.CrossBranchRootModel+TrimmedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Quinary cross-branch nothing nested type was removed.");
        if (FindMember(trimmedNestedType, "TrimmedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected Quinary nothing nested field is present.");
        if (FindMember(trimmedNestedType, "TrimmedMethod", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected Quinary nothing nested method is present.");

        Console.WriteLine("reflection-plugin-quinary-cross-branch-root-modes:verified");
        Console.WriteLine("reflection-plugin-quinary-root-modes:verified");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察跨程序集嵌套 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察跨程序集嵌套 root 的成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察跨程序集字段和基类成员。")]
    private static void VerifyCrossAssemblyNestedRootMode(Assembly tertiaryAssembly)
    {
        const string tertiaryNamespace = "ReflectionTrimProbe.TertiaryModels";
        const string pluginNamespace = "ReflectionTrimProbe.PluginModels";
        var nestedType = tertiaryAssembly.GetType(
            $"{tertiaryNamespace}.CrossAssemblyNestedRootModel+PreservedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Cross-assembly preserved nested type was removed.");

        if (FindMember(nestedType, "PreservedNestedField", PublicInstanceFlags) is null)
            throw new InvalidOperationException("Expected preserved nested field was removed.");
        if (FindMember(nestedType, "TrimmedNestedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed nested field is present.");

        var baseType = nestedType.BaseType
            ?? throw new InvalidOperationException("Cross-assembly nested root lost its base type.");
        if (!string.Equals(baseType.Assembly.GetName().Name, CompositeAssemblyName, StringComparison.Ordinal)
            || !string.Equals(baseType.FullName, $"{pluginNamespace}.CompositeCrossAssemblyBase", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cross-assembly nested root resolved an unexpected base type.");
        }

        if (FindMember(baseType, "PreservedBaseField", PublicInstanceFlags) is null)
            throw new InvalidOperationException("Expected preserved cross-assembly base field was removed.");
        if (FindMember(baseType, "TrimmedBaseField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed cross-assembly base field is present.");

        var payloadField = nestedType.GetField("PreservedPayload", PublicInstanceFlags)
            ?? throw new InvalidOperationException("Expected preserved cross-assembly payload field was removed.");
        var payloadType = payloadField.FieldType;
        if (!string.Equals(payloadType.Assembly.GetName().Name, CompositeAssemblyName, StringComparison.Ordinal)
            || !string.Equals(payloadType.FullName, $"{pluginNamespace}.CompositeCrossAssemblyPayload", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cross-assembly nested root resolved an unexpected payload type.");
        }

        if (FindMember(payloadType, "PreservedPayloadField", PublicInstanceFlags) is null)
            throw new InvalidOperationException("Expected preserved cross-assembly payload field was removed.");
        if (FindMember(payloadType, "TrimmedPayloadField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected trimmed cross-assembly payload field is present.");

        var trimmedNestedType = tertiaryAssembly.GetType(
            $"{tertiaryNamespace}.CrossAssemblyNestedRootModel+TrimmedNested",
            throwOnError: false)
            ?? throw new InvalidOperationException("Cross-assembly nothing nested type was removed.");
        if (FindMember(trimmedNestedType, "TrimmedField", PublicInstanceFlags) is not null)
            throw new InvalidOperationException("Expected cross-assembly nothing nested field is present.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型观察 preserve 模式下的成员边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针通过动态类型观察 preserve 模式下的成员边界。")]
    private static void VerifyRootMode(
        Assembly assembly,
        string modelNamespace,
        string typeName,
        string? preservedMember,
        params string[] trimmedMembers)
    {
        var type = assembly.GetType(
            $"{modelNamespace}.{typeName}",
            throwOnError: false)
            ?? throw new InvalidOperationException($"Root-mode type was removed: {typeName}");
        var flags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        if (preservedMember is not null && FindMember(type, preservedMember, flags) is null)
            throw new InvalidOperationException($"Expected preserved member was removed: {typeName}.{preservedMember}");

        foreach (var trimmedMember in trimmedMembers)
        {
            if (FindMember(type, trimmedMember, flags) is not null)
                throw new InvalidOperationException($"Expected trimmed member is present: {typeName}.{trimmedMember}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "探针通过动态类型查询成员，不要求 linker 为未知类型保留成员。")]
    private static MemberInfo? FindMember(Type type, string memberName, BindingFlags flags)
    {
        return type.GetMember(memberName, flags).FirstOrDefault();
    }

    private const BindingFlags PublicInstanceFlags =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过类型名称观察泛型定义和嵌套类型的 linker 根描述边界。")]
    private static void VerifyMember(
        Assembly assembly,
        string modelNamespace,
        string typeName,
        string memberName)
    {
        var type = assembly.GetType(
            $"{modelNamespace}.{typeName}",
            throwOnError: false)
            ?? throw new InvalidOperationException($"Root-mode type was removed: {typeName}");
        var flags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        if (FindMember(type, memberName, flags) is null)
            throw new InvalidOperationException($"Expected preserved member was removed: {typeName}.{memberName}");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "探针通过签名查询观察构造函数和方法重载的 linker 根描述边界。")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "探针按字符串类型名称枚举签名，故意观察 linker 根描述边界。")]
    private static void VerifySignatureRootMode(Assembly assembly, string modelNamespace)
    {
        var type = assembly.GetType(
            $"{modelNamespace}.CompositeSignatureRootModel",
            throwOnError: false)
            ?? throw new InvalidOperationException("Root-mode type was removed: CompositeSignatureRootModel");
        var flags = BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;
        var members = type.GetMembers(flags);
        var constructors = members.OfType<ConstructorInfo>().ToArray();
        if (!constructors.Any(constructor => HasParameterTypes(constructor, typeof(int))))
            throw new InvalidOperationException("Expected preserved constructor was removed: CompositeSignatureRootModel..ctor(System.Int32)");

        if (constructors.Any(constructor => constructor.GetParameters().Length == 0))
            throw new InvalidOperationException("Expected trimmed constructor is present: CompositeSignatureRootModel..ctor()");

        if (constructors.Any(constructor => HasParameterTypes(constructor, typeof(string))))
        {
            throw new InvalidOperationException("Expected trimmed constructor is present: CompositeSignatureRootModel..ctor(System.String)");
        }

        var methods = members.OfType<MethodInfo>()
            .Where(method => string.Equals(method.Name, "PreservedMethod", StringComparison.Ordinal))
            .ToArray();
        if (!methods.Any(method => HasParameterTypes(method, typeof(int))))
            throw new InvalidOperationException("Expected preserved method was removed: CompositeSignatureRootModel.PreservedMethod(System.Int32)");

        if (methods.Any(method => HasParameterTypes(method, typeof(string))))
        {
            throw new InvalidOperationException("Expected trimmed method is present: CompositeSignatureRootModel.PreservedMethod(System.String)");
        }
    }

    private static bool HasParameterTypes(MethodBase method, params Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        return parameters.Length == parameterTypes.Length
            && parameters.Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes);
    }
#pragma warning restore IL2026, IL2070, IL2075
}
