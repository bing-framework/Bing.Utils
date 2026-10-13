using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bing.Reflection;

/// <summary>验证构造和动态泛型入口向调用方公开运行时限制。</summary>
[Trait("Reflection", "DeploymentContracts")]
public class ReflectionDynamicCodeContractTests
{
#if NET6_0_OR_GREATER
    [Fact]
    public void ConstructorEntryPoints_DeclareUnreferencedCodeContract()
    {
        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateInstance), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(object[])))
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateInstances), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(IEnumerable<object[]>)))
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateGenericInstance), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(Type[]), typeof(object[])))
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(Reflections), nameof(Reflections.CreateInstance), method =>
                method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(object[])))
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
    }
#endif

#if NET7_0_OR_GREATER
    [Fact]
    public void ConstructorEntryPoints_DeclareDynamicCodeContract()
    {
        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateInstance), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(object[])))
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateInstances), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(IEnumerable<object[]>)))
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(TypeVisit), nameof(TypeVisit.CreateGenericInstance), method =>
                !method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(Type[]), typeof(object[])))
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();

        GetMethod(typeof(Reflections), nameof(Reflections.CreateInstance), method =>
                method.IsGenericMethodDefinition
                && HasParameterTypes(method, typeof(Type), typeof(object[])))
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();
    }
#endif

    private static MethodInfo GetMethod(Type declaringType, string name, Func<MethodInfo, bool> predicate) =>
        declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == name && predicate(method));

    private static bool HasParameterTypes(MethodInfo method, params Type[] parameterTypes) =>
        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes);
}
