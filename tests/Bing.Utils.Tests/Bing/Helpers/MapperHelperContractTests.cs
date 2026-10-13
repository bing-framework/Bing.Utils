#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace Bing.Helpers;

/// <summary>
/// 验证映射器公开入口向裁剪和 NativeAOT 调用方声明运行时限制。
/// </summary>
[Trait("Bing.Helpers", "DeploymentContracts")]
public class MapperHelperContractTests
{
    [Fact]
    public void ReferencedAssembly_TargetFrameworkMatchesTestAssembly()
    {
        var mapperTargetFramework = typeof(MapperHelper).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            .ShouldNotBeNull();
        var testTargetFramework = typeof(MapperHelperContractTests).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            .ShouldNotBeNull();

        mapperTargetFramework.FrameworkName.ShouldBe(testTargetFramework.FrameworkName);
    }

    [Fact]
    public void ReflectionBasedEntryPoints_DeclareUnreferencedCodeContract()
    {
        GetGenericMethod(nameof(MapperHelper.Map), 1)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapWith), 2)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapWithout), 2)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapTo), 3)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapList), 2)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
        typeof(MapperHelper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(MapperHelper.MapDictionary))
            .ShouldAllBe(method => method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() != null);
        typeof(MapperHelper)
            .GetMethod(nameof(MapperHelper.ToObject), BindingFlags.Public | BindingFlags.Static)
            .GetCustomAttribute<RequiresUnreferencedCodeAttribute>()
            .ShouldNotBeNull();
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void ExpressionBasedEntryPoints_DeclareDynamicCodeContract()
    {
        GetGenericMethod(nameof(MapperHelper.Map), 1)
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapWith), 2)
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();
        GetGenericMethod(nameof(MapperHelper.MapWithout), 2)
            .GetCustomAttribute<RequiresDynamicCodeAttribute>()
            .ShouldNotBeNull();
    }
#endif

    private static MethodInfo GetGenericMethod(string name, int parameterCount) =>
        typeof(MapperHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == name
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 2
                              && method.GetParameters().Length == parameterCount);
}
#endif
