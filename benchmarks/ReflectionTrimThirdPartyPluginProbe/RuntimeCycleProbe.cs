using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace ReflectionTrimThirdPartyPluginProbe;

/// <summary>
/// 验证两个独立插件部署目录中的循环 resolver 图、程序集隔离和可回收边界。
/// </summary>
public static class RuntimeCycleProbe
{
    private const string PluginAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginModels";
    private const string PluginTypeName = "ReflectionTrimProbe.NonTrimmablePluginModels.ThirdPartyBoundaryModel";
    private const string DependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginDependency";
    private const string TransitiveDependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency";
    private const string MissingCycleAssemblyName = "ReflectionTrimProbe.RuntimeCycleProbe.MissingDependency";
    private const string AssemblyRefCycleAAssemblyName = "RuntimeCycleFixture.A";
    private const string AssemblyRefCycleBAssemblyName = "RuntimeCycleFixture.B";
    private const string AssemblyRefBranchCAssemblyName = "RuntimeCycleFixture.C";
    private const string AssemblyRefMultiBranchDAssemblyName = "RuntimeCycleFixture.D";
    private const string AssemblyRefDeepBranchEAssemblyName = "RuntimeCycleFixture.E";
    private const string AssemblyRefCycleATypeName = "RuntimeCycleFixture.A.AMarker";
    private const string AssemblyRefCycleBTypeName = "RuntimeCycleFixture.B.BMarker";
    private const string AssemblyRefBranchCTypeName = "RuntimeCycleFixture.C.CMarker";
    private const string AssemblyRefMultiBranchDTypeName = "RuntimeCycleFixture.D.DMarker";
    private const string AssemblyRefDeepBranchETypeName = "RuntimeCycleFixture.E.EMarker";

    /// <summary>
    /// 判断加载模式是否由循环 resolver 探针支持。
    /// </summary>
    /// <param name="mode">插件加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>模式受支持时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public static bool IsSupportedMode(string? mode) =>
        mode is "path" or "resolver" or "bytes";

    /// <summary>
    /// 在两个部署目录中执行循环 resolver 的三种加载语义并检查卸载结果。
    /// </summary>
    /// <param name="firstDirectory">第一个插件部署目录。</param>
    /// <param name="secondDirectory">第二个插件部署目录。</param>
    /// <param name="mode">插件加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>全部检查通过时返回 <c>metadata-preserved</c>，否则返回边界结果字符串。</returns>
    public static string Run(string firstDirectory, string secondDirectory, string mode)
    {
        if (!IsSupportedMode(mode)
            || string.IsNullOrWhiteSpace(firstDirectory)
            || string.IsNullOrWhiteSpace(secondDirectory))
        {
            return "assembly-load-failed";
        }

        string firstPath;
        string secondPath;
        try
        {
            firstPath = Path.GetFullPath(firstDirectory);
            secondPath = Path.GetFullPath(secondDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return "assembly-load-failed";
        }

        if (!Directory.Exists(firstPath) || !Directory.Exists(secondPath))
            return "assembly-load-failed";

        var firstPluginPath = Path.Combine(firstPath, PluginAssemblyName + ".dll");
        var secondPluginPath = Path.Combine(secondPath, PluginAssemblyName + ".dll");
        if (!File.Exists(firstPluginPath) || !File.Exists(secondPluginPath))
            return "assembly-trimmed";

        var firstHostPath = FindHostPath(firstPath);
        var secondHostPath = FindHostPath(secondPath);
        if (firstHostPath is null || secondHostPath is null)
            return "assembly-load-failed";

        var loadResult = LoadPair(
            firstPath,
            secondPath,
            firstPluginPath,
            secondPluginPath,
            firstHostPath,
            secondHostPath,
            mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.References)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    /// <summary>
    /// 加载带真实双向 AssemblyRef 的 A/B 程序集，并验证类型解析和卸载边界。
    /// </summary>
    /// <param name="directory">同时包含 A/B 循环程序集的部署目录。</param>
    /// <param name="mode">根程序集加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>全部检查通过时返回 <c>metadata-preserved</c>，否则返回边界结果字符串。</returns>
    public static string RunAssemblyRefCycle(string directory, string mode)
    {
        if (!IsSupportedMode(mode) || string.IsNullOrWhiteSpace(directory))
            return "assembly-load-failed";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(directory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return "assembly-load-failed";
        }

        if (!Directory.Exists(fullPath))
            return "assembly-load-failed";

        var assemblyAPath = Path.Combine(fullPath, AssemblyRefCycleAAssemblyName + ".dll");
        var assemblyBPath = Path.Combine(fullPath, AssemblyRefCycleBAssemblyName + ".dll");
        if (!File.Exists(assemblyAPath) || !File.Exists(assemblyBPath))
            return "assembly-trimmed";

        var loadResult = LoadAssemblyRefCycle(fullPath, assemblyAPath, assemblyBPath, mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.References)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    /// <summary>
    /// 加载 A/B 循环及 C 到 B 的真实 AssemblyRef 分支，并验证类型解析和卸载边界。
    /// </summary>
    /// <param name="directory">同时包含 A/B/C 程序集的部署目录。</param>
    /// <param name="mode">根程序集加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>全部检查通过时返回 <c>metadata-preserved</c>，否则返回边界结果字符串。</returns>
    public static string RunAssemblyRefBranch(string directory, string mode)
    {
        if (!IsSupportedMode(mode) || string.IsNullOrWhiteSpace(directory))
            return "assembly-load-failed";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(directory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return "assembly-load-failed";
        }

        if (!Directory.Exists(fullPath))
            return "assembly-load-failed";

        var assemblyAPath = Path.Combine(fullPath, AssemblyRefCycleAAssemblyName + ".dll");
        var assemblyBPath = Path.Combine(fullPath, AssemblyRefCycleBAssemblyName + ".dll");
        var assemblyCPath = Path.Combine(fullPath, AssemblyRefBranchCAssemblyName + ".dll");
        if (!File.Exists(assemblyAPath) || !File.Exists(assemblyBPath) || !File.Exists(assemblyCPath))
            return "assembly-trimmed";

        var loadResult = LoadAssemblyRefBranch(fullPath, assemblyAPath, assemblyBPath, assemblyCPath, mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.References)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    /// <summary>
    /// 加载 A/B 循环、C→B 分支及 D→A/C 双分支的真实 AssemblyRef 图，并验证类型解析和卸载边界。
    /// </summary>
    /// <param name="directory">同时包含 A/B/C/D 程序集的部署目录。</param>
    /// <param name="mode">根程序集加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>全部检查通过时返回 <c>metadata-preserved</c>，否则返回边界结果字符串。</returns>
    public static string RunAssemblyRefMultiBranch(string directory, string mode)
    {
        if (!IsSupportedMode(mode) || string.IsNullOrWhiteSpace(directory))
            return "assembly-load-failed";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(directory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return "assembly-load-failed";
        }

        if (!Directory.Exists(fullPath))
            return "assembly-load-failed";

        var assemblyAPath = Path.Combine(fullPath, AssemblyRefCycleAAssemblyName + ".dll");
        var assemblyBPath = Path.Combine(fullPath, AssemblyRefCycleBAssemblyName + ".dll");
        var assemblyCPath = Path.Combine(fullPath, AssemblyRefBranchCAssemblyName + ".dll");
        var assemblyDPath = Path.Combine(fullPath, AssemblyRefMultiBranchDAssemblyName + ".dll");
        if (!File.Exists(assemblyAPath)
            || !File.Exists(assemblyBPath)
            || !File.Exists(assemblyCPath)
            || !File.Exists(assemblyDPath))
        {
            return "assembly-trimmed";
        }

        var loadResult = LoadAssemblyRefMultiBranch(
            fullPath,
            assemblyAPath,
            assemblyBPath,
            assemblyCPath,
            assemblyDPath,
            mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.References)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    /// <summary>
    /// 加载 A/B 循环、C→B、D→A/C 及 E→D/B 的深层 AssemblyRef 图，并验证类型解析和卸载边界。
    /// </summary>
    /// <param name="directory">同时包含 A/B/C/D/E 程序集的部署目录。</param>
    /// <param name="mode">根程序集加载模式，支持 <c>path</c>、<c>resolver</c> 和 <c>bytes</c>。</param>
    /// <returns>全部检查通过时返回 <c>metadata-preserved</c>，否则返回边界结果字符串。</returns>
    public static string RunAssemblyRefDeepBranch(string directory, string mode)
    {
        if (!IsSupportedMode(mode) || string.IsNullOrWhiteSpace(directory))
            return "assembly-load-failed";

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(directory);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return "assembly-load-failed";
        }

        if (!Directory.Exists(fullPath))
            return "assembly-load-failed";

        var assemblyAPath = Path.Combine(fullPath, AssemblyRefCycleAAssemblyName + ".dll");
        var assemblyBPath = Path.Combine(fullPath, AssemblyRefCycleBAssemblyName + ".dll");
        var assemblyCPath = Path.Combine(fullPath, AssemblyRefBranchCAssemblyName + ".dll");
        var assemblyDPath = Path.Combine(fullPath, AssemblyRefMultiBranchDAssemblyName + ".dll");
        var assemblyEPath = Path.Combine(fullPath, AssemblyRefDeepBranchEAssemblyName + ".dll");
        if (!File.Exists(assemblyAPath)
            || !File.Exists(assemblyBPath)
            || !File.Exists(assemblyCPath)
            || !File.Exists(assemblyDPath)
            || !File.Exists(assemblyEPath))
        {
            return "assembly-trimmed";
        }

        var loadResult = LoadAssemblyRefDeepBranch(
            fullPath,
            assemblyAPath,
            assemblyBPath,
            assemblyCPath,
            assemblyDPath,
            assemblyEPath,
            mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.References)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    private static CycleRunResult LoadPair(
        string firstDirectory,
        string secondDirectory,
        string firstPluginPath,
        string secondPluginPath,
        string firstHostPath,
        string secondHostPath,
        string mode)
    {
        CycleLoadContext? firstContext = null;
        CycleLoadContext? secondContext = null;
        var references = new List<WeakReference>();
        try
        {
            firstContext = new CycleLoadContext(
                "A",
                firstDirectory,
                firstPluginPath,
                firstHostPath,
                mode);
            references.Add(new WeakReference(firstContext));

            secondContext = new CycleLoadContext(
                "B",
                secondDirectory,
                secondPluginPath,
                secondHostPath,
                mode);
            references.Add(new WeakReference(secondContext));

            firstContext.Peer = secondContext;
            secondContext.Peer = firstContext;

            var firstPlugin = firstContext.LoadPluginAssembly();
            references.Add(new WeakReference(firstPlugin));
            var secondPlugin = secondContext.LoadPluginAssembly();
            references.Add(new WeakReference(secondPlugin));

            var firstPluginUsable = InvokePlugin(firstPlugin);
            var secondPluginUsable = InvokePlugin(secondPlugin);
            var firstDependency = FindAssembly(firstContext, DependencyAssemblyName);
            var secondDependency = FindAssembly(secondContext, DependencyAssemblyName);
            var firstTransitiveDependency = FindAssembly(firstContext, TransitiveDependencyAssemblyName);
            var secondTransitiveDependency = FindAssembly(secondContext, TransitiveDependencyAssemblyName);
            if (firstDependency is not null)
                references.Add(new WeakReference(firstDependency));
            if (secondDependency is not null)
                references.Add(new WeakReference(secondDependency));
            if (firstTransitiveDependency is not null)
                references.Add(new WeakReference(firstTransitiveDependency));
            if (secondTransitiveDependency is not null)
                references.Add(new WeakReference(secondTransitiveDependency));

            var isolated = firstPluginUsable
                && secondPluginUsable
                && firstDependency is not null
                && secondDependency is not null
                && firstTransitiveDependency is not null
                && secondTransitiveDependency is not null
                && !ReferenceEquals(firstPlugin, secondPlugin)
                && !ReferenceEquals(firstDependency, secondDependency)
                && !ReferenceEquals(firstTransitiveDependency, secondTransitiveDependency)
                && !ReferenceEquals(firstPlugin.GetType(PluginTypeName), secondPlugin.GetType(PluginTypeName))
                && BelongsTo(firstPlugin, firstContext)
                && BelongsTo(secondPlugin, secondContext)
                && BelongsTo(firstDependency, firstContext)
                && BelongsTo(secondDependency, secondContext)
                && BelongsTo(firstTransitiveDependency, firstContext)
                && BelongsTo(secondTransitiveDependency, secondContext)
                && firstContext.IsCycleBounded()
                && secondContext.IsCycleBounded();

            return new CycleRunResult(
                isolated ? "metadata-preserved" : "member-trimmed",
                references.ToArray());
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return new CycleRunResult("member-trimmed", references.ToArray());
        }
        finally
        {
            if (firstContext is not null)
                firstContext.Peer = null;
            if (secondContext is not null)
                secondContext.Peer = null;
            firstContext?.Unload();
            secondContext?.Unload();
        }
    }

    private static bool InvokePlugin(Assembly pluginAssembly)
    {
        var type = pluginAssembly.GetType(PluginTypeName, throwOnError: false);
        if (type is null)
            return false;

        var instance = Activator.CreateInstance(type);
        var field = type.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
        var method = type.GetMethod(
            "Execute",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        if (instance is null || field is null || method is null)
            return false;

        field.SetValue(instance, 19);
        return method.Invoke(instance, new object[] { 23 }) is int;
    }

    private static CycleRunResult LoadAssemblyRefCycle(
        string directory,
        string assemblyAPath,
        string assemblyBPath,
        string mode)
    {
        MetadataCycleLoadContext? context = null;
        var references = new List<WeakReference>();
        try
        {
            context = new MetadataCycleLoadContext(directory, assemblyAPath, assemblyBPath, mode);
            references.Add(new WeakReference(context));

            var assemblyA = context.LoadRootAssembly();
            references.Add(new WeakReference(assemblyA));
            var assemblyB = context.LoadPeerAssembly();
            references.Add(new WeakReference(assemblyB));

            var assemblyAReferences = assemblyA.GetReferencedAssemblies();
            var assemblyBReferences = assemblyB.GetReferencedAssemblies();
            var typeA = assemblyA.GetType(AssemblyRefCycleATypeName, throwOnError: false);
            var typeB = assemblyB.GetType(AssemblyRefCycleBTypeName, throwOnError: false);
            var peerA = typeA?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerB = typeB?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);

            var preserved = assemblyAReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyBReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && typeA is not null
                && typeB is not null
                && peerA is not null
                && peerB is not null
                && peerA.FieldType == typeB
                && peerB.FieldType == typeA
                && !ReferenceEquals(assemblyA, assemblyB)
                && BelongsTo(assemblyA, context)
                && BelongsTo(assemblyB, context)
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleAAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleBAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1;

            return new CycleRunResult(
                preserved ? "metadata-preserved" : "member-trimmed",
                references.ToArray());
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return new CycleRunResult("member-trimmed", references.ToArray());
        }
        finally
        {
            context?.Unload();
        }
    }

    private static CycleRunResult LoadAssemblyRefBranch(
        string directory,
        string assemblyAPath,
        string assemblyBPath,
        string assemblyCPath,
        string mode)
    {
        MetadataCycleLoadContext? context = null;
        var references = new List<WeakReference>();
        try
        {
            context = new MetadataCycleLoadContext(directory, assemblyAPath, assemblyBPath, mode, assemblyCPath);
            references.Add(new WeakReference(context));

            var assemblyA = context.LoadRootAssembly();
            var assemblyB = context.LoadPeerAssembly();
            var assemblyC = context.LoadBranchAssembly();
            references.Add(new WeakReference(assemblyA));
            references.Add(new WeakReference(assemblyB));
            references.Add(new WeakReference(assemblyC));

            var assemblyAReferences = assemblyA.GetReferencedAssemblies();
            var assemblyBReferences = assemblyB.GetReferencedAssemblies();
            var assemblyCReferences = assemblyC.GetReferencedAssemblies();
            var typeA = assemblyA.GetType(AssemblyRefCycleATypeName, throwOnError: false);
            var typeB = assemblyB.GetType(AssemblyRefCycleBTypeName, throwOnError: false);
            var typeC = assemblyC.GetType(AssemblyRefBranchCTypeName, throwOnError: false);
            var peerA = typeA?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerB = typeB?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerC = typeC?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);

            var preserved = assemblyAReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyBReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyCReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && typeA is not null
                && typeB is not null
                && typeC is not null
                && peerA is not null
                && peerB is not null
                && peerC is not null
                && peerA.FieldType == typeB
                && peerB.FieldType == typeA
                && peerC.FieldType == typeB
                && !ReferenceEquals(assemblyA, assemblyB)
                && !ReferenceEquals(assemblyA, assemblyC)
                && !ReferenceEquals(assemblyB, assemblyC)
                && BelongsTo(assemblyA, context)
                && BelongsTo(assemblyB, context)
                && BelongsTo(assemblyC, context)
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleAAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleBAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefBranchCAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1;

            return new CycleRunResult(
                preserved ? "metadata-preserved" : "member-trimmed",
                references.ToArray());
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return new CycleRunResult("member-trimmed", references.ToArray());
        }
        finally
        {
            context?.Unload();
        }
    }

    private static CycleRunResult LoadAssemblyRefMultiBranch(
        string directory,
        string assemblyAPath,
        string assemblyBPath,
        string assemblyCPath,
        string assemblyDPath,
        string mode)
    {
        MetadataCycleLoadContext? context = null;
        var references = new List<WeakReference>();
        try
        {
            context = new MetadataCycleLoadContext(
                directory,
                assemblyAPath,
                assemblyBPath,
                mode,
                assemblyCPath,
                assemblyDPath);
            references.Add(new WeakReference(context));

            var assemblyA = context.LoadRootAssembly();
            var assemblyB = context.LoadPeerAssembly();
            var assemblyC = context.LoadBranchAssembly();
            var assemblyD = context.LoadMultiBranchAssembly();
            references.Add(new WeakReference(assemblyA));
            references.Add(new WeakReference(assemblyB));
            references.Add(new WeakReference(assemblyC));
            references.Add(new WeakReference(assemblyD));

            var assemblyAReferences = assemblyA.GetReferencedAssemblies();
            var assemblyBReferences = assemblyB.GetReferencedAssemblies();
            var assemblyCReferences = assemblyC.GetReferencedAssemblies();
            var assemblyDReferences = assemblyD.GetReferencedAssemblies();
            var typeA = assemblyA.GetType(AssemblyRefCycleATypeName, throwOnError: false);
            var typeB = assemblyB.GetType(AssemblyRefCycleBTypeName, throwOnError: false);
            var typeC = assemblyC.GetType(AssemblyRefBranchCTypeName, throwOnError: false);
            var typeD = assemblyD.GetType(AssemblyRefMultiBranchDTypeName, throwOnError: false);
            var peerA = typeA?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerB = typeB?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerC = typeC?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var rootPeerD = typeD?.GetField("RootPeer", BindingFlags.Public | BindingFlags.Instance);
            var branchPeerD = typeD?.GetField("BranchPeer", BindingFlags.Public | BindingFlags.Instance);

            var preserved = assemblyAReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyBReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyCReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyDReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyDReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefBranchCAssemblyName, StringComparison.OrdinalIgnoreCase))
                && typeA is not null
                && typeB is not null
                && typeC is not null
                && typeD is not null
                && peerA is not null
                && peerB is not null
                && peerC is not null
                && rootPeerD is not null
                && branchPeerD is not null
                && peerA.FieldType == typeB
                && peerB.FieldType == typeA
                && peerC.FieldType == typeB
                && rootPeerD.FieldType == typeA
                && branchPeerD.FieldType == typeC
                && !ReferenceEquals(assemblyA, assemblyB)
                && !ReferenceEquals(assemblyA, assemblyC)
                && !ReferenceEquals(assemblyA, assemblyD)
                && !ReferenceEquals(assemblyB, assemblyC)
                && !ReferenceEquals(assemblyB, assemblyD)
                && !ReferenceEquals(assemblyC, assemblyD)
                && BelongsTo(assemblyA, context)
                && BelongsTo(assemblyB, context)
                && BelongsTo(assemblyC, context)
                && BelongsTo(assemblyD, context)
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleAAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleBAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefBranchCAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefMultiBranchDAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1;

            return new CycleRunResult(
                preserved ? "metadata-preserved" : "member-trimmed",
                references.ToArray());
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return new CycleRunResult("member-trimmed", references.ToArray());
        }
        finally
        {
            context?.Unload();
        }
    }

    private static CycleRunResult LoadAssemblyRefDeepBranch(
        string directory,
        string assemblyAPath,
        string assemblyBPath,
        string assemblyCPath,
        string assemblyDPath,
        string assemblyEPath,
        string mode)
    {
        MetadataCycleLoadContext? context = null;
        var references = new List<WeakReference>();
        try
        {
            context = new MetadataCycleLoadContext(
                directory,
                assemblyAPath,
                assemblyBPath,
                mode,
                assemblyCPath,
                assemblyDPath,
                assemblyEPath);
            references.Add(new WeakReference(context));

            var assemblyA = context.LoadRootAssembly();
            var assemblyB = context.LoadPeerAssembly();
            var assemblyC = context.LoadBranchAssembly();
            var assemblyD = context.LoadMultiBranchAssembly();
            var assemblyE = context.LoadDeepBranchAssembly();
            references.Add(new WeakReference(assemblyA));
            references.Add(new WeakReference(assemblyB));
            references.Add(new WeakReference(assemblyC));
            references.Add(new WeakReference(assemblyD));
            references.Add(new WeakReference(assemblyE));

            var assemblyAReferences = assemblyA.GetReferencedAssemblies();
            var assemblyBReferences = assemblyB.GetReferencedAssemblies();
            var assemblyCReferences = assemblyC.GetReferencedAssemblies();
            var assemblyDReferences = assemblyD.GetReferencedAssemblies();
            var assemblyEReferences = assemblyE.GetReferencedAssemblies();
            var typeA = assemblyA.GetType(AssemblyRefCycleATypeName, throwOnError: false);
            var typeB = assemblyB.GetType(AssemblyRefCycleBTypeName, throwOnError: false);
            var typeC = assemblyC.GetType(AssemblyRefBranchCTypeName, throwOnError: false);
            var typeD = assemblyD.GetType(AssemblyRefMultiBranchDTypeName, throwOnError: false);
            var typeE = assemblyE.GetType(AssemblyRefDeepBranchETypeName, throwOnError: false);
            var peerA = typeA?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerB = typeB?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var peerC = typeC?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var rootPeerD = typeD?.GetField("RootPeer", BindingFlags.Public | BindingFlags.Instance);
            var branchPeerD = typeD?.GetField("BranchPeer", BindingFlags.Public | BindingFlags.Instance);
            var deepPeerE = typeE?.GetField("DeepPeer", BindingFlags.Public | BindingFlags.Instance);
            var cyclePeerE = typeE?.GetField("CyclePeer", BindingFlags.Public | BindingFlags.Instance);

            var preserved = assemblyAReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyBReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyCReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyDReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleAAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyDReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefBranchCAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyEReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefMultiBranchDAssemblyName, StringComparison.OrdinalIgnoreCase))
                && assemblyEReferences.Any(reference =>
                    string.Equals(reference.Name, AssemblyRefCycleBAssemblyName, StringComparison.OrdinalIgnoreCase))
                && typeA is not null
                && typeB is not null
                && typeC is not null
                && typeD is not null
                && typeE is not null
                && peerA is not null
                && peerB is not null
                && peerC is not null
                && rootPeerD is not null
                && branchPeerD is not null
                && deepPeerE is not null
                && cyclePeerE is not null
                && peerA.FieldType == typeB
                && peerB.FieldType == typeA
                && peerC.FieldType == typeB
                && rootPeerD.FieldType == typeA
                && branchPeerD.FieldType == typeC
                && deepPeerE.FieldType == typeD
                && cyclePeerE.FieldType == typeB
                && !ReferenceEquals(assemblyA, assemblyB)
                && !ReferenceEquals(assemblyA, assemblyC)
                && !ReferenceEquals(assemblyA, assemblyD)
                && !ReferenceEquals(assemblyA, assemblyE)
                && !ReferenceEquals(assemblyB, assemblyC)
                && !ReferenceEquals(assemblyB, assemblyD)
                && !ReferenceEquals(assemblyB, assemblyE)
                && !ReferenceEquals(assemblyC, assemblyD)
                && !ReferenceEquals(assemblyC, assemblyE)
                && !ReferenceEquals(assemblyD, assemblyE)
                && BelongsTo(assemblyA, context)
                && BelongsTo(assemblyB, context)
                && BelongsTo(assemblyC, context)
                && BelongsTo(assemblyD, context)
                && BelongsTo(assemblyE, context)
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleAAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefCycleBAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefBranchCAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefMultiBranchDAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1
                && context.Assemblies.Count(assembly =>
                    string.Equals(assembly.GetName().Name, AssemblyRefDeepBranchEAssemblyName,
                        StringComparison.OrdinalIgnoreCase)) == 1;

            return new CycleRunResult(
                preserved ? "metadata-preserved" : "member-trimmed",
                references.ToArray());
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return new CycleRunResult("member-trimmed", references.ToArray());
        }
        finally
        {
            context?.Unload();
        }
    }

    private static Assembly? FindAssembly(AssemblyLoadContext context, string assemblyName) =>
        context.Assemblies.FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

    private static bool BelongsTo(Assembly assembly, AssemblyLoadContext context) =>
        ReferenceEquals(AssemblyLoadContext.GetLoadContext(assembly), context);

    private static string? FindHostPath(string directory) =>
        new[] { "ReflectionTrimProbe.dll", "ReflectionTrimProbe.exe" }
            .Select(name => Path.Combine(directory, name))
            .FirstOrDefault(File.Exists);

    private static bool WaitForUnload(IReadOnlyCollection<WeakReference> references)
    {
        if (references.Count == 0)
            return true;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (references.All(reference => !reference.IsAlive))
                return true;
            Thread.Yield();
        }

        return false;
    }

    private static bool IsBoundaryFailure(Exception exception) => exception is
        FileLoadException or FileNotFoundException or BadImageFormatException or
        TypeLoadException or MissingMemberException or MemberAccessException or
        TargetInvocationException or ArgumentException or InvalidOperationException or
        IOException or UnauthorizedAccessException or ReflectionTypeLoadException;

    private sealed class CycleRunResult
    {
        public CycleRunResult(string boundary, WeakReference[] references)
        {
            Boundary = boundary;
            References = references;
        }

        public string Boundary { get; }

        public WeakReference[] References { get; }
    }

    private sealed class ResolutionState
    {
        public HashSet<string> Active { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Visited { get; } = new(StringComparer.Ordinal);

        public int ActiveBoundaryHits { get; private set; }

        public int VisitedBoundaryHits { get; private set; }

        public bool TryEnter(string key, int maxNodes)
        {
            var active = Active.Contains(key);
            var visited = Visited.Contains(key);
            if (active || visited)
            {
                if (active)
                    ActiveBoundaryHits++;
                if (visited)
                    VisitedBoundaryHits++;
                return false;
            }

            if (Visited.Count >= maxNodes)
            {
                ActiveBoundaryHits++;
                VisitedBoundaryHits++;
                return false;
            }

            Active.Add(key);
            Visited.Add(key);
            return true;
        }

        public void Leave(string key) => Active.Remove(key);
    }

    private sealed class CycleLoadContext : AssemblyLoadContext
    {
        private const int MaxResolutionNodes = 32;
        private readonly string _nodeName;
        private readonly string _directory;
        private readonly string _pluginAssemblyPath;
        private readonly string _pluginAssemblyName;
        private readonly string _mode;
        private readonly AssemblyDependencyResolver _resolver;
        private readonly byte[]? _pluginBytes;

        public CycleLoadContext(
            string nodeName,
            string directory,
            string pluginAssemblyPath,
            string hostPath,
            string mode)
            : base($"ReflectionTrimThirdPartyPlugin.Cycle.{nodeName}", isCollectible: true)
        {
            _nodeName = nodeName;
            _directory = directory;
            _pluginAssemblyPath = pluginAssemblyPath;
            _pluginAssemblyName = Path.GetFileNameWithoutExtension(pluginAssemblyPath);
            _mode = mode;
            _resolver = new AssemblyDependencyResolver(hostPath);
            _pluginBytes = mode == "bytes"
                ? File.ReadAllBytes(pluginAssemblyPath)
                : null;
        }

        public CycleLoadContext? Peer { get; set; }

        public Assembly LoadPluginAssembly() => _mode switch
        {
            "path" => LoadFromAssemblyPath(_pluginAssemblyPath),
            "resolver" => LoadFromAssemblyName(new AssemblyName(_pluginAssemblyName)),
            "bytes" => LoadPluginImage(),
            _ => throw new ArgumentException($"Unsupported plugin load mode: {_mode}", nameof(_mode))
        };

        public bool IsCycleBounded()
        {
            var state = new ResolutionState();
            var path = ResolveAssemblyPath(new AssemblyName(MissingCycleAssemblyName), state);
            return path is null
                && state.ActiveBoundaryHits > 0
                && state.VisitedBoundaryHits > 0
                && state.Visited.Count <= MaxResolutionNodes;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (IsFrameworkAssembly(assemblyName))
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }

            if (string.Equals(assemblyName.Name, _pluginAssemblyName, StringComparison.OrdinalIgnoreCase))
                return LoadPluginImage();

            var state = new ResolutionState();
            var path = ResolveAssemblyPath(assemblyName, state);
            return path is null ? null : LoadFromAssemblyPath(path);
        }

        private Assembly LoadPluginImage()
        {
            if (_pluginBytes is null)
                return LoadFromAssemblyPath(_pluginAssemblyPath);

            using var stream = new MemoryStream(_pluginBytes, writable: false);
            return LoadFromStream(stream);
        }

        private string? ResolveAssemblyPath(AssemblyName assemblyName, ResolutionState state)
        {
            var key = $"{_nodeName}:{assemblyName.Name}";
            if (!state.TryEnter(key, MaxResolutionNodes))
                return null;

            try
            {
                var localPath = Path.Combine(_directory, (assemblyName.Name ?? string.Empty) + ".dll");
                if (File.Exists(localPath))
                    return localPath;

                var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath is not null)
                    return resolvedPath;

                return Peer?.ResolveAssemblyPath(assemblyName, state);
            }
            finally
            {
                state.Leave(key);
            }
        }

        private static bool IsFrameworkAssembly(AssemblyName assemblyName) =>
            assemblyName.Name is "System" or "netstandard"
                || assemblyName.Name?.StartsWith("System.", StringComparison.Ordinal) == true
                || assemblyName.Name?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
    }

    private sealed class MetadataCycleLoadContext : AssemblyLoadContext
    {
        private readonly string _directory;
        private readonly string _assemblyAPath;
        private readonly string _assemblyBPath;
        private readonly string? _assemblyCPath;
        private readonly string? _assemblyDPath;
        private readonly string? _assemblyEPath;
        private readonly string _mode;
        private readonly byte[]? _assemblyABytes;

        public MetadataCycleLoadContext(
            string directory,
            string assemblyAPath,
            string assemblyBPath,
            string mode,
            string? assemblyCPath = null,
            string? assemblyDPath = null,
            string? assemblyEPath = null)
            : base("ReflectionTrimThirdPartyPlugin.AssemblyRefCycle", isCollectible: true)
        {
            _directory = directory;
            _assemblyAPath = assemblyAPath;
            _assemblyBPath = assemblyBPath;
            _assemblyCPath = assemblyCPath;
            _assemblyDPath = assemblyDPath;
            _assemblyEPath = assemblyEPath;
            _mode = mode;
            _assemblyABytes = mode == "bytes"
                ? File.ReadAllBytes(assemblyAPath)
                : null;
        }

        public Assembly LoadRootAssembly() => _mode switch
        {
            "path" => LoadFromAssemblyPath(_assemblyAPath),
            "resolver" => LoadFromAssemblyName(new AssemblyName(AssemblyRefCycleAAssemblyName)),
            "bytes" => LoadRootImage(),
            _ => throw new ArgumentException($"Unsupported assembly reference cycle mode: {_mode}", nameof(_mode))
        };

        public Assembly LoadPeerAssembly() => LoadFromAssemblyPath(_assemblyBPath);

        public Assembly LoadBranchAssembly() => _assemblyCPath is null
            ? throw new InvalidOperationException("The AssemblyRef branch fixture is not configured.")
            : LoadFromAssemblyPath(_assemblyCPath);

        public Assembly LoadMultiBranchAssembly() => _assemblyDPath is null
            ? throw new InvalidOperationException("The AssemblyRef multi-branch fixture is not configured.")
            : LoadFromAssemblyPath(_assemblyDPath);

        public Assembly LoadDeepBranchAssembly() => _assemblyEPath is null
            ? throw new InvalidOperationException("The AssemblyRef deep-branch fixture is not configured.")
            : LoadFromAssemblyPath(_assemblyEPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (IsFrameworkAssembly(assemblyName))
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            if (string.Equals(assemblyName.Name, AssemblyRefCycleAAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                return _mode == "bytes" ? LoadRootImage() : LoadFromAssemblyPath(_assemblyAPath);

            if (string.Equals(assemblyName.Name, AssemblyRefCycleBAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                return LoadFromAssemblyPath(_assemblyBPath);

            if (_assemblyCPath is not null
                && string.Equals(assemblyName.Name, AssemblyRefBranchCAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                return LoadFromAssemblyPath(_assemblyCPath);

            if (_assemblyDPath is not null
                && string.Equals(assemblyName.Name, AssemblyRefMultiBranchDAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                return LoadFromAssemblyPath(_assemblyDPath);

            if (_assemblyEPath is not null
                && string.Equals(assemblyName.Name, AssemblyRefDeepBranchEAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
                return LoadFromAssemblyPath(_assemblyEPath);

            var localPath = Path.Combine(_directory, (assemblyName.Name ?? string.Empty) + ".dll");
            return File.Exists(localPath) ? LoadFromAssemblyPath(localPath) : null;
        }

        private Assembly LoadRootImage()
        {
            if (_assemblyABytes is null)
                return LoadFromAssemblyPath(_assemblyAPath);

            using var stream = new MemoryStream(_assemblyABytes, writable: false);
            return LoadFromStream(stream);
        }

        private static bool IsFrameworkAssembly(AssemblyName assemblyName) =>
            assemblyName.Name is "System" or "netstandard"
                || assemblyName.Name?.StartsWith("System.", StringComparison.Ordinal) == true
                || assemblyName.Name?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
    }
}
