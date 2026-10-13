using System.Reflection;
using System.Reflection.Emit;
using Shouldly;

namespace Bing.Reflection;

public class ReflectionCacheScopeTests
{
    [Fact]
    public void IsCollectible_GenericParameterFromCollectibleAssembly_IsDetected()
    {
        var collectibleAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"CollectibleConstraint_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var collectibleType = collectibleAssembly.DefineDynamicModule("Main")
            .DefineType("CollectibleConstraint", TypeAttributes.Public | TypeAttributes.Class)
            .CreateTypeInfo()!
            .AsType();

        var hostAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"ConstraintHost_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var hostBuilder = hostAssembly.DefineDynamicModule("Main")
            .DefineType("ConstraintHost", TypeAttributes.Public | TypeAttributes.Class);
        var genericParameter = hostBuilder.DefineGenericParameters("T")[0];
        genericParameter.SetBaseTypeConstraint(collectibleType);
        var hostType = hostBuilder.CreateTypeInfo()!.AsType();

        ReflectionPackageCacheScope.IsCollectible(hostType.GetGenericArguments()[0]).ShouldBeTrue();

        var ordinaryHostAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"OrdinaryConstraintHost_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var ordinaryHostBuilder = ordinaryHostAssembly.DefineDynamicModule("Main")
            .DefineType("OrdinaryHost", TypeAttributes.Public | TypeAttributes.Class);
        var ordinaryParameter = ordinaryHostBuilder.DefineGenericParameters("T")[0];
        ordinaryParameter.SetInterfaceConstraints(typeof(IDisposable));
        var ordinaryHostType = ordinaryHostBuilder.CreateTypeInfo()!.AsType();

        ReflectionPackageCacheScope.IsCollectible(ordinaryHostType.GetGenericArguments()[0]).ShouldBeFalse();
    }

    [Fact]
    public void IsCollectible_RecursiveGenericConstraints_Terminates()
    {
        var typeParameter = typeof(RecursiveTypeConstraint<>).GetGenericArguments()[0];
        var method = typeof(ReflectionCacheScopeTests).GetMethod(
            nameof(RecursiveMethodConstraint), BindingFlags.Static | BindingFlags.NonPublic)!;

        ReflectionPackageCacheScope.IsCollectible(typeParameter).ShouldBeFalse();
        ReflectionPackageCacheScope.IsCollectible(method).ShouldBeFalse();

        TypeReflections.GetPublicInstanceProperties(typeof(RecursiveTypeConstraint<>))
            .Select(property => property.Name)
            .ShouldContain(nameof(RecursiveTypeConstraint<int>.Value));
    }

    private sealed class RecursiveTypeConstraint<T> where T : IComparable<T>
    {
        public T Value { get; set; }
    }

    private static void RecursiveMethodConstraint<T>(T value) where T : IComparable<T>
    {
    }
}
