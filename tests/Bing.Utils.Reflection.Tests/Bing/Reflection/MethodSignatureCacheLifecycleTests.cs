using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;

namespace Bing.Reflection;

#if NET6_0_OR_GREATER

/// <summary>验证方法实例缓存的 collectible 结果在并发命中时不会残留。</summary>
public class MethodSignatureCacheLifecycleTests
{
    [Fact]
    public void GetCachedInstanceMethod_ConcurrentCollectibleResult_RemovesInstanceCacheEntry()
    {
        var (sourceMethod, resolvedMethod) = CreateCollectibleMethod();
        var instances = new ConditionalWeakTable<MethodInfo, Lazy<MethodInfo>>();
        var lazy = new Lazy<MethodInfo>(() => resolvedMethod, LazyThreadSafetyMode.ExecutionAndPublication);
        instances.Add(sourceMethod, lazy);
        var helper = typeof(TypeVisit).GetMethod("GetCachedInstanceMethod", BindingFlags.Static | BindingFlags.NonPublic)!;
        var methods = new MethodInfo[64];

        Parallel.For(0, methods.Length,
            index => methods[index] = (MethodInfo)helper.Invoke(null, new object[] { instances, sourceMethod, lazy })!);

        methods.ShouldAllBe(method => method.DeclaringType!.Assembly.IsCollectible);
        instances.TryGetValue(sourceMethod, out _).ShouldBeFalse();
    }

    private static (MethodInfo SourceMethod, MethodInfo ResolvedMethod) CreateCollectibleMethod()
    {
        var collectibleAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"CollectibleMethodContract_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var pluginInterfaceBuilder = collectibleAssembly.DefineDynamicModule("Main").DefineType(
            "PluginContract",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        var pluginMethod = pluginInterfaceBuilder.DefineMethod(
            nameof(SourceContract.Execute),
            MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            CallingConventions.Standard,
            typeof(void),
            Type.EmptyTypes);
        var pluginInterface = pluginInterfaceBuilder.CreateTypeInfo()!.AsType();

        var sourceMethod = typeof(SourceContract).GetMethod(nameof(SourceContract.Execute))!;
        var resolvedMethod = pluginInterface.GetMethod(nameof(SourceContract.Execute))!;

        return (sourceMethod, resolvedMethod);
    }

    private sealed class SourceContract
    {
        public void Execute()
        {
        }
    }
}

#endif
