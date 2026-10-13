using Bing.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace ReflectionTrimThirdPartyPluginProbe;

/// <summary>
/// 验证未声明 IsTrimmable 的外部插件在不同宿主发布边界中的元数据行为。
/// </summary>
public static class Program
{
    private const string AssemblyName = "ReflectionTrimProbe.NonTrimmablePluginModels";
    private const string TypeName = "ReflectionTrimProbe.NonTrimmablePluginModels.ThirdPartyBoundaryModel";
    private const string DependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginDependency";
    private const string DependencyTypeName = "ReflectionTrimProbe.NonTrimmablePluginDependency.ThirdPartyBoundaryDependency";
    private const string TransitiveDependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency";
    private const string TransitiveDependencyTypeName = "ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency.ThirdPartyBoundaryTransitiveDependency";
    private const string VersionConflictProfile = "version-conflict-v2";
    private const string TransitiveVariantProfile = "transitive-variant";
    private const string DirectDependencyVariantProfile = "dependency-variant";
    private const string DiamondProfile = "diamond";
    private const string MixedVariantProfile = "mixed-variant";
    private const string DeepDiamondProfile = "deep-diamond";
    private const string WideDeepDiamondProfile = "wide-deep-diamond";
    private const string StrongNameConflictProfile = "strong-name-conflict";
    private const string StrongNameConflictAssemblyName = "ReflectionTrimProbe.StrongNameConflictModels";
    private const string StrongNameConflictTypeName = "ReflectionTrimProbe.StrongNameConflictModels.StrongNameConflictModel";
    private const string StrongNameConflictDependencyAssemblyName = "System.Text.Json";
    private const string StrongNameConflictDependencyTypeName = "ReflectionTrimProbe.StrongNameConflictDependency.StrongNameConflictDependency";
    private const string SecondaryDependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency";
    private const string SecondaryDependencyTypeName = "ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency.ThirdPartyBoundarySecondaryDependency";
    private const string TertiaryDependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency";
    private const string TertiaryDependencyTypeName = "ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency.ThirdPartyBoundaryTertiaryDependency";
    private const string DeepDependencyAssemblyName = "ReflectionTrimProbe.NonTrimmablePluginDeepDependency";
    private const string DeepDependencyTypeName = "ReflectionTrimProbe.NonTrimmablePluginDeepDependency.ThirdPartyBoundaryDeepDependency";
    // 插件探针必须在创建可回收上下文前拒绝无界输入，避免资源边界测试本身制造无限元数据。
    internal const int MaxMultiPluginContexts = 32;

    public static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--multi-version", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 3 || string.IsNullOrWhiteSpace(args[1]) || string.IsNullOrWhiteSpace(args[2]))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --multi-version <v1-directory> <v2-directory>");
                return 2;
            }

            return RunMultiVersion(args[1], args[2]);
        }

        if (args.Length > 0 && string.Equals(args[0], "--multi-plugin", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 4 || args.Skip(1).Any(string.IsNullOrWhiteSpace))
            {
                Console.Error.WriteLine($"Usage: ReflectionTrimThirdPartyPluginProbe --multi-plugin <v1-directory> <v2-directory> <strong-name-directory> [<transitive-variant-directory> [<dependency-variant-directory> [<diamond-directory> [<mixed-variant-directory> [<deep-diamond-directory> [<wide-deep-diamond-directory> [<duplicate-v1-directory> ...]]]]]]] (最多 {MaxMultiPluginContexts} 个目录)");
                return 2;
            }

            return RunMultiPlugin(args.Skip(1).ToArray());
        }

        if (args.Length > 0 && string.Equals(args[0], "--multi-plugin-rounds", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 5
                || !int.TryParse(
                    args[1],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var rounds)
                || rounds is < 1 or > 20
                || args.Skip(2).Any(string.IsNullOrWhiteSpace))
            {
                Console.Error.WriteLine($"Usage: ReflectionTrimThirdPartyPluginProbe --multi-plugin-rounds <1-20> <v1-directory> <v2-directory> <strong-name-directory> [<transitive-variant-directory> [<dependency-variant-directory> [<diamond-directory> [<mixed-variant-directory> [<deep-diamond-directory> [<wide-deep-diamond-directory> [<duplicate-v1-directory> ...]]]]]]] (最多 {MaxMultiPluginContexts} 个目录)");
                return 2;
            }

            return RunMultiPluginRounds(rounds, args.Skip(2).ToArray());
        }

        if (args.Length > 0 && string.Equals(args[0], "--runtime-cycle", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 3 || args.Skip(1).Any(string.IsNullOrWhiteSpace))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --runtime-cycle <first-directory> <second-directory>");
                return 2;
            }

            return RunRuntimeCycle(args[1], args[2]);
        }

        if (args.Length > 0 && string.Equals(args[0], "--assemblyref-cycle", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --assemblyref-cycle <fixture-directory>");
                return 2;
            }

            return RunAssemblyRefCycle(args[1]);
        }

        if (args.Length > 0 && string.Equals(args[0], "--assemblyref-branch", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --assemblyref-branch <fixture-directory>");
                return 2;
            }

            return RunAssemblyRefBranch(args[1]);
        }

        if (args.Length > 0 && string.Equals(args[0], "--assemblyref-multi-branch", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --assemblyref-multi-branch <fixture-directory>");
                return 2;
            }

            return RunAssemblyRefMultiBranch(args[1]);
        }

        if (args.Length > 0 && string.Equals(args[0], "--assemblyref-deep-branch", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe --assemblyref-deep-branch <fixture-directory>");
                return 2;
            }

            return RunAssemblyRefDeepBranch(args[1]);
        }

        if ((args.Length < 1 || args.Length > 3) || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Usage: ReflectionTrimThirdPartyPluginProbe <published-directory> [<metadata-preserved|assembly-trimmed|member-trimmed|trimmed>] [<version-conflict-v2|strong-name-conflict|transitive-variant|dependency-variant|diamond|mixed-variant|deep-diamond|wide-deep-diamond>]");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --multi-version <v1-directory> <v2-directory>");
            Console.Error.WriteLine($"   or: ReflectionTrimThirdPartyPluginProbe --multi-plugin <v1-directory> <v2-directory> <strong-name-directory> [<transitive-variant-directory> [<dependency-variant-directory> [<diamond-directory> [<mixed-variant-directory> [<deep-diamond-directory> [<wide-deep-diamond-directory> [<duplicate-v1-directory> ...]]]]]]] (最多 {MaxMultiPluginContexts} 个目录)");
            Console.Error.WriteLine($"   or: ReflectionTrimThirdPartyPluginProbe --multi-plugin-rounds <1-20> <v1-directory> <v2-directory> <strong-name-directory> [<transitive-variant-directory> [<dependency-variant-directory> [<diamond-directory> [<mixed-variant-directory> [<deep-diamond-directory> [<wide-deep-diamond-directory> [<duplicate-v1-directory> ...]]]]]]] (最多 {MaxMultiPluginContexts} 个目录)");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --runtime-cycle <first-directory> <second-directory>");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --assemblyref-cycle <fixture-directory>");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --assemblyref-branch <fixture-directory>");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --assemblyref-multi-branch <fixture-directory>");
            Console.Error.WriteLine("   or: ReflectionTrimThirdPartyPluginProbe --assemblyref-deep-branch <fixture-directory>");
            return 2;
        }

        var expected = args.Length >= 2 ? args[1] : null;
        if (expected is not null
            && expected is not ("metadata-preserved" or "assembly-trimmed" or "member-trimmed" or "trimmed"))
        {
            Console.Error.WriteLine($"Unsupported expected plugin boundary: {expected}");
            return 2;
        }

        var profile = args.Length == 3 ? args[2] : null;
        if (profile is not null
            && profile is not (VersionConflictProfile or TransitiveVariantProfile or DirectDependencyVariantProfile or DiamondProfile or MixedVariantProfile or DeepDiamondProfile or WideDeepDiamondProfile or StrongNameConflictProfile))
        {
            Console.Error.WriteLine($"Unsupported plugin verification profile: {profile}");
            return 2;
        }

        var directory = Path.GetFullPath(args[0]);
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Plugin target directory does not exist: {directory}");
            return 2;
        }

        var pluginAssemblyName = profile == StrongNameConflictProfile
            ? StrongNameConflictAssemblyName
            : AssemblyName;
        var assemblyPath = Path.Combine(directory, pluginAssemblyName + ".dll");
        var pathResult = LoadFromPath(directory, assemblyPath, profile);
        var resolverResult = LoadWithResolver(directory, assemblyPath, profile);
        var bytesResult = LoadFromBytes(directory, assemblyPath, profile);

        Console.WriteLine($"reflection-third-party-plugin-boundary:{pathResult}");
        Console.WriteLine($"reflection-third-party-plugin-boundary:resolver:{resolverResult}");
        Console.WriteLine($"reflection-third-party-plugin-boundary:bytes:{bytesResult}");

        return IsUnexpected(pathResult, expected)
            || IsUnexpected(resolverResult, expected)
            || IsUnexpected(bytesResult, expected)
            ? 1
            : 0;
    }

    private static bool IsUnexpected(string result, string? expected) => expected switch
    {
        null => result is not ("metadata-preserved" or "assembly-trimmed"),
        "metadata-preserved" => result != "metadata-preserved",
        "assembly-trimmed" => result != "assembly-trimmed",
        "member-trimmed" => result != "member-trimmed",
        "trimmed" => result is not ("assembly-trimmed" or "member-trimmed"),
        _ => true
    };

    private static int RunMultiVersion(string firstDirectory, string secondDirectory)
    {
        var first = Path.GetFullPath(firstDirectory);
        var second = Path.GetFullPath(secondDirectory);
        if (!Directory.Exists(first) || !Directory.Exists(second))
        {
            Console.Error.WriteLine("Both multi-version plugin directories must exist.");
            return 2;
        }

        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = LoadVersionPair(first, second, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:multi-version:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunMultiPlugin(string[] pluginDirectories, int? round = null)
    {
        if (pluginDirectories.Length > MaxMultiPluginContexts)
        {
            Console.Error.WriteLine(
                $"Multi-plugin resource limit exceeded: {pluginDirectories.Length} contexts requested; maximum is {MaxMultiPluginContexts}.");
            Console.WriteLine(
                $"reflection-third-party-plugin-boundary:multi-plugin:resource-rejected:contexts:{pluginDirectories.Length}>{MaxMultiPluginContexts}");
            return 2;
        }

        var directories = pluginDirectories.Select(Path.GetFullPath).ToArray();
        if (directories.Length < 3 || directories.Any(directory => !Directory.Exists(directory)))
        {
            Console.Error.WriteLine("At least three existing multi-plugin directories are required.");
            return 2;
        }

        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = LoadMultiPluginSet(directories, mode);
            var marker = round is null
                ? $"reflection-third-party-plugin-boundary:multi-plugin:{mode}:{result}"
                : $"reflection-third-party-plugin-boundary:multi-plugin:round:{round}:{mode}:{result}";
            Console.WriteLine(marker);
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunMultiPluginRounds(int rounds, string[] pluginDirectories)
    {
        if (pluginDirectories.Length > MaxMultiPluginContexts)
            return RunMultiPlugin(pluginDirectories);

        var failures = false;
        for (var round = 1; round <= rounds; round++)
            failures |= RunMultiPlugin(pluginDirectories, round) != 0;

        return failures ? 1 : 0;
    }

    private static int RunRuntimeCycle(string firstDirectory, string secondDirectory)
    {
        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = RuntimeCycleProbe.Run(firstDirectory, secondDirectory, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:runtime-cycle:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunAssemblyRefCycle(string directory)
    {
        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = RuntimeCycleProbe.RunAssemblyRefCycle(directory, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:assemblyref-cycle:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunAssemblyRefBranch(string directory)
    {
        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = RuntimeCycleProbe.RunAssemblyRefBranch(directory, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:assemblyref-branch:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunAssemblyRefMultiBranch(string directory)
    {
        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = RuntimeCycleProbe.RunAssemblyRefMultiBranch(directory, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:assemblyref-multi-branch:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static int RunAssemblyRefDeepBranch(string directory)
    {
        var failures = false;
        foreach (var mode in new[] { "path", "resolver", "bytes" })
        {
            var result = RuntimeCycleProbe.RunAssemblyRefDeepBranch(directory, mode);
            Console.WriteLine($"reflection-third-party-plugin-boundary:assemblyref-deep-branch:{mode}:{result}");
            failures |= result != "metadata-preserved";
        }

        return failures ? 1 : 0;
    }

    private static string LoadVersionPair(string firstDirectory, string secondDirectory, string mode)
    {
        var firstAssemblyPath = Path.Combine(firstDirectory, AssemblyName + ".dll");
        var secondAssemblyPath = Path.Combine(secondDirectory, AssemblyName + ".dll");
        if (!File.Exists(firstAssemblyPath) || !File.Exists(secondAssemblyPath))
            return "assembly-trimmed";

        ResolverLoadContext? firstContext = null;
        ResolverLoadContext? secondContext = null;
        try
        {
            var firstHostPath = FindHostPath(firstDirectory);
            var secondHostPath = FindHostPath(secondDirectory);
            if (firstHostPath is null || secondHostPath is null)
                return "assembly-load-failed";

            firstContext = new ResolverLoadContext(
                firstHostPath,
                firstAssemblyPath,
                mode == "bytes" ? File.ReadAllBytes(firstAssemblyPath) : null);
            secondContext = new ResolverLoadContext(
                secondHostPath,
                secondAssemblyPath,
                mode == "bytes" ? File.ReadAllBytes(secondAssemblyPath) : null);

            var firstAssembly = LoadPlugin(firstContext, firstAssemblyPath, mode, AssemblyName);
            var secondAssembly = LoadPlugin(secondContext, secondAssemblyPath, mode, AssemblyName);
            var firstResult = ReadBoundary(firstContext!, firstAssembly, profile: null);
            var secondResult = ReadBoundary(secondContext!, secondAssembly, profile: VersionConflictProfile);
            var firstDependency = FindAssembly(firstContext!, DependencyAssemblyName);
            var secondDependency = FindAssembly(secondContext!, DependencyAssemblyName);

            return firstResult == "metadata-preserved"
                && secondResult == "metadata-preserved"
                && Equals(firstDependency?.GetName().Version, new Version(1, 0, 0, 0))
                && Equals(secondDependency?.GetName().Version, new Version(2, 0, 0, 0))
                && firstDependency is not null
                && secondDependency is not null
                && !ReferenceEquals(firstDependency, secondDependency)
                && ReferenceEquals(AssemblyLoadContext.GetLoadContext(firstAssembly), firstContext)
                && ReferenceEquals(AssemblyLoadContext.GetLoadContext(secondAssembly), secondContext)
                && ReferenceEquals(AssemblyLoadContext.GetLoadContext(firstDependency), firstContext)
                && ReferenceEquals(AssemblyLoadContext.GetLoadContext(secondDependency), secondContext)
                && !ReferenceEquals(firstAssembly.GetType(TypeName), secondAssembly.GetType(TypeName))
                ? "metadata-preserved"
                : "member-trimmed";
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
        finally
        {
            firstContext?.Unload();
            secondContext?.Unload();
        }
    }

    private static string LoadMultiPluginSet(
        IReadOnlyList<string> pluginDirectories,
        string mode)
    {
        var loadResult = LoadMultiPluginSetCore(
            pluginDirectories,
            mode);
        if (loadResult.Boundary != "metadata-preserved")
            return loadResult.Boundary;

        return WaitForUnload(loadResult.Contexts)
            ? "metadata-preserved"
            : "member-trimmed";
    }

    private static MultiPluginLoadResult LoadMultiPluginSetCore(
        IReadOnlyList<string> pluginDirectories,
        string mode)
    {
        if (pluginDirectories.Count < 3)
        {
            return new MultiPluginLoadResult("assembly-trimmed", Array.Empty<WeakReference>());
        }

        var specifications = BuildMultiPluginSpecifications(pluginDirectories);
        var contexts = new List<ResolverLoadContext>(specifications.Length);
        var assemblies = new List<Assembly>(specifications.Length);
        var boundary = "member-trimmed";
        WeakReference[] contextReferences = Array.Empty<WeakReference>();
        try
        {
            if (specifications.Any(specification =>
                !File.Exists(Path.Combine(specification.Directory, specification.AssemblyName + ".dll"))))
            {
                boundary = "assembly-trimmed";
            }
            else
            {
                for (var index = 0; index < specifications.Length; index++)
                {
                    var specification = specifications[index];
                    var assemblyPath = Path.Combine(
                        specification.Directory,
                        specification.AssemblyName + ".dll");
                    var hostPath = FindHostPath(specification.Directory);
                    if (hostPath is null)
                    {
                        boundary = "assembly-load-failed";
                        break;
                    }

                    var context = new ResolverLoadContext(
                        hostPath,
                        assemblyPath,
                        mode == "bytes" ? File.ReadAllBytes(assemblyPath) : null);
                    contexts.Add(context);
                    assemblies.Add(LoadPlugin(
                        context,
                        assemblyPath,
                        mode,
                        specification.AssemblyName));
                }

                if (contexts.Count == specifications.Length)
                {
                    var results = specifications
                        .Select((specification, index) =>
                            ReadBoundary(contexts[index], assemblies[index], specification.Profile))
                        .ToArray();
                    var dependencies = specifications
                        .Select((specification, index) =>
                            FindAssembly(contexts[index], specification.DependencyAssemblyName))
                        .ToArray();
                    var defaultStrongNameDependency = AssemblyLoadContext.Default.LoadFromAssemblyName(
                        new AssemblyName(StrongNameConflictDependencyAssemblyName));
                    var ordinaryDependencyIndexes = specifications
                        .Select((specification, index) => new { specification, index })
                        .Where(item => !item.specification.IsStrongName)
                        .Select(item => item.index)
                        .ToArray();
                    var ordinaryTypes = ordinaryDependencyIndexes
                        .Select(index => assemblies[index].GetType(TypeName, throwOnError: false))
                        .ToArray();
                    var preserved = results.All(result => result == "metadata-preserved")
                        && contexts.Count == specifications.Length
                        && assemblies.Count == specifications.Length
                        && dependencies.All(dependency => dependency is not null)
                        && specifications.Select((specification, index) =>
                            specification.ExpectedDependencyVersion is null
                                || Equals(dependencies[index]?.GetName().Version,
                                    specification.ExpectedDependencyVersion)).All(value => value)
                        && specifications.Select((specification, index) =>
                            ReferenceEquals(
                                AssemblyLoadContext.GetLoadContext(assemblies[index]),
                                contexts[index])).All(value => value)
                        && specifications.Select((specification, index) =>
                            ReferenceEquals(
                                AssemblyLoadContext.GetLoadContext(dependencies[index]!),
                                contexts[index])).All(value => value)
                        && ordinaryTypes.All(type => type is not null)
                        && ordinaryTypes.Distinct().Count() == ordinaryTypes.Length
                        && ordinaryDependencyIndexes
                            .SelectMany((left, leftPosition) => ordinaryDependencyIndexes
                                .Skip(leftPosition + 1)
                                .Select(right => !ReferenceEquals(dependencies[left], dependencies[right])))
                            .All(value => value)
                        && !ReferenceEquals(
                            dependencies[2],
                            defaultStrongNameDependency);
                    boundary = preserved ? "metadata-preserved" : "member-trimmed";
                }
            }
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            boundary = "member-trimmed";
        }
        finally
        {
            contextReferences = CaptureContexts(contexts);
            foreach (var context in contexts)
                context.Unload();
            contexts.Clear();
            assemblies.Clear();
        }

        return new MultiPluginLoadResult(boundary, contextReferences);
    }

    private static MultiPluginSpec[] BuildMultiPluginSpecifications(
        IReadOnlyList<string> pluginDirectories)
    {
        var specifications = new List<MultiPluginSpec>(pluginDirectories.Count)
        {
            new(
                pluginDirectories[0],
                AssemblyName,
                profile: null,
                DependencyAssemblyName,
                new Version(1, 0, 0, 0)),
            new(
                pluginDirectories[1],
                AssemblyName,
                VersionConflictProfile,
                DependencyAssemblyName,
                new Version(2, 0, 0, 0)),
            new(
                pluginDirectories[2],
                StrongNameConflictAssemblyName,
                StrongNameConflictProfile,
                StrongNameConflictDependencyAssemblyName,
                expectedDependencyVersion: null)
        };

        // 第四目录保持 M92 的间接依赖变体，第五目录验证直接依赖变体，第六目录验证 diamond 图，第七目录同时替换两层依赖，第八目录验证三层 diamond 图，第九目录验证三分支深层 diamond 图，后续目录仍可作为普通 v1 副本。
        for (var index = 3; index < pluginDirectories.Count; index++)
        {
            specifications.Add(new MultiPluginSpec(
                pluginDirectories[index],
                AssemblyName,
                profile: index == 3
                    ? TransitiveVariantProfile
                    : index == 4
                        ? DirectDependencyVariantProfile
                        : index == 5
                            ? DiamondProfile
                            : index == 6
                                ? MixedVariantProfile
                                : index == 7
                                    ? DeepDiamondProfile
                                    : index == 8
                                        ? WideDeepDiamondProfile
                                    : null,
                DependencyAssemblyName,
                new Version(1, 0, 0, 0)));
        }

        return specifications.ToArray();
    }

    private static WeakReference[] CaptureContexts(IEnumerable<AssemblyLoadContext> contexts) =>
        contexts
            .Select(context => new WeakReference(context))
            .ToArray();

    private static bool WaitForUnload(IReadOnlyCollection<WeakReference> contexts)
    {
        if (contexts.Count == 0)
            return true;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (contexts.All(context => !context.IsAlive))
                return true;
            Thread.Yield();
        }

        return false;
    }

    private static Assembly LoadPlugin(
        ResolverLoadContext context,
        string assemblyPath,
        string mode,
        string pluginAssemblyName) => mode switch
    {
        "path" => context.LoadFromAssemblyPath(assemblyPath),
        "resolver" => context.LoadFromAssemblyName(new AssemblyName(pluginAssemblyName)),
        "bytes" => context.LoadPlugin(),
        _ => throw new ArgumentException($"Unsupported plugin load mode: {mode}", nameof(mode))
    };

    private static Assembly? FindAssembly(AssemblyLoadContext context, string name) =>
        context.Assemblies.FirstOrDefault(item =>
            string.Equals(item.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

    private static string LoadFromPath(string targetDirectory, string assemblyPath, string? profile)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        ResolverLoadContext? context = null;
        try
        {
            var hostPath = FindHostPath(targetDirectory);
            if (hostPath is null)
                return "assembly-load-failed";
            context = new ResolverLoadContext(hostPath, assemblyPath);
            return ReadBoundary(context, context.LoadFromAssemblyPath(assemblyPath), profile);
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
        finally
        {
            context?.Unload();
        }
    }

    private static string LoadWithResolver(string targetDirectory, string assemblyPath, string? profile)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        AssemblyLoadContext? context = null;
        try
        {
            var hostPath = FindHostPath(targetDirectory);
            if (hostPath is null)
                return "assembly-load-failed";
            context = new ResolverLoadContext(hostPath, assemblyPath);
            var pluginAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            var assembly = context.LoadFromAssemblyName(new AssemblyName(pluginAssemblyName));
            return ReadBoundary(context, assembly, profile);
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
        finally
        {
            context?.Unload();
        }
    }

    private static string LoadFromBytes(string targetDirectory, string assemblyPath, string? profile)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        ResolverLoadContext? context = null;
        try
        {
            var hostPath = FindHostPath(targetDirectory);
            if (hostPath is null)
                return "assembly-load-failed";
            context = new ResolverLoadContext(hostPath, assemblyPath, File.ReadAllBytes(assemblyPath));
            return ReadBoundary(context, context.LoadPlugin(), profile);
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
        finally
        {
            context?.Unload();
        }
    }

    private static string ReadBoundary(AssemblyLoadContext context, Assembly assembly, string? profile)
    {
        if (profile == StrongNameConflictProfile)
            return ReadStrongNameBoundary(context, assembly);

        var type = assembly.GetType(TypeName, throwOnError: false);
        if (type is null)
            return "member-trimmed";

        try
        {
            var description = TypeReflections.GetDescription(type);
            var runtimeDescription = TypeReflections.GetAttribute<DescriptionAttribute>(type);
            var field = type.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
            var property = type.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            var method = TypeVisit.FindMethod(type, "Execute", typeof(int));
            var constructor = TypeVisit.GetParameterlessConstructor(type);
            if (field is null || property is null || method is null || constructor is null)
                return "member-trimmed";

            var instance = Activator.CreateInstance(type);
            if (instance is null)
                return "member-trimmed";
            field.SetValue(instance, 19);
            property.SetValue(instance, "loaded");
            var result = method.Invoke(instance, new object[] { 23 });
            var dependency = context.Assemblies.FirstOrDefault(item =>
                string.Equals(item.GetName().Name, DependencyAssemblyName, StringComparison.OrdinalIgnoreCase));
            var dependencyType = dependency?.GetType(DependencyTypeName, throwOnError: false);
            var dependencyDescription = dependencyType is null ? null : TypeReflections.GetDescription(dependencyType);
            var dependencyRuntimeDescription = dependencyType is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(dependencyType);
            var dependencyMethod = dependencyType?.GetMethod(
                "GetBonus",
                BindingFlags.Public | BindingFlags.Static);
            var dependencyMethodDescription = dependencyMethod is null
                ? null
                : TypeReflections.GetDescription(dependencyMethod);
            var dependencyResult = dependencyMethod?.Invoke(null, null);
            var isVersionConflictProfile = profile == VersionConflictProfile;
            var isTransitiveVariantProfile = profile == TransitiveVariantProfile;
            var isDirectDependencyVariantProfile = profile == DirectDependencyVariantProfile;
            var isDiamondProfile = profile == DiamondProfile;
            var isMixedVariantProfile = profile == MixedVariantProfile;
            var isDeepDiamondProfile = profile == DeepDiamondProfile;
            var isWideDeepDiamondProfile = profile == WideDeepDiamondProfile;
            var isDeepGraphProfile = isDeepDiamondProfile || isWideDeepDiamondProfile;
            var hasTransitiveVariant = isTransitiveVariantProfile || isMixedVariantProfile || isDeepGraphProfile;
            var expectedDependencyVersion = isVersionConflictProfile
                ? new Version(2, 0, 0, 0)
                : new Version(1, 0, 0, 0);
            var expectedDependencyDescription = isVersionConflictProfile
                ? "Third-party non-trimmable dependency v2"
                : isDirectDependencyVariantProfile || isMixedVariantProfile
                    ? "Third-party non-trimmable dependency variant"
                    : "Third-party non-trimmable dependency";
            var expectedDependencyMethodDescription = isVersionConflictProfile
                ? "Third-party dependency bonus v2"
                : isDirectDependencyVariantProfile || isMixedVariantProfile
                    ? "Third-party dependency bonus variant"
                    : "Third-party dependency bonus";
            var expectedDependencyResult = isVersionConflictProfile
                ? 29
                : isDeepGraphProfile
                    ? 25
                : isMixedVariantProfile
                    ? 30
                    : isDirectDependencyVariantProfile
                        ? 24
                        : isTransitiveVariantProfile
                            ? 24
                            : 18;
            var expectedPluginResult = isVersionConflictProfile
                ? 71
                : isWideDeepDiamondProfile
                    ? 167
                : isDeepGraphProfile
                    ? 112
                : isMixedVariantProfile
                    ? 72
                    : isDirectDependencyVariantProfile
                        ? 66
                        : isDiamondProfile
                            ? 98
                            : isTransitiveVariantProfile
                                ? 66
                                : 60;
            var expectedTransitiveDescription = isDeepGraphProfile
                ? "Third-party non-trimmable deep transitive dependency variant"
                : hasTransitiveVariant
                    ? "Third-party non-trimmable transitive dependency variant"
                    : "Third-party non-trimmable transitive dependency";
            var expectedTransitiveMethodDescription = isDeepGraphProfile
                ? "Third-party deep transitive dependency bonus variant"
                : hasTransitiveVariant
                    ? "Third-party transitive dependency bonus variant"
                    : "Third-party transitive dependency bonus";
            var expectedTransitiveResult = isDeepGraphProfile
                ? 14
                : hasTransitiveVariant
                    ? 13
                    : 7;
            var transitiveDependency = context.Assemblies.FirstOrDefault(item =>
                string.Equals(item.GetName().Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase));
            var transitiveDependencyType = transitiveDependency?.GetType(TransitiveDependencyTypeName, throwOnError: false);
            var transitiveDependencyDescription = transitiveDependencyType is null
                ? null
                : TypeReflections.GetDescription(transitiveDependencyType);
            var transitiveDependencyRuntimeDescription = transitiveDependencyType is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(transitiveDependencyType);
            var transitiveDependencyMethod = transitiveDependencyType?.GetMethod(
                "GetBonus",
                BindingFlags.Public | BindingFlags.Static);
            var transitiveDependencyMethodDescription = transitiveDependencyMethod is null
                ? null
                : TypeReflections.GetDescription(transitiveDependencyMethod);
            var transitiveDependencyResult = transitiveDependencyMethod?.Invoke(null, null);
            var secondaryDependency = isDiamondProfile || isDeepGraphProfile
                ? context.Assemblies.FirstOrDefault(item =>
                    string.Equals(item.GetName().Name, SecondaryDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))
                : null;
            var secondaryDependencyType = secondaryDependency?.GetType(SecondaryDependencyTypeName, throwOnError: false);
            var secondaryDependencyDescription = secondaryDependencyType is null
                ? null
                : TypeReflections.GetDescription(secondaryDependencyType);
            var secondaryDependencyRuntimeDescription = secondaryDependencyType is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(secondaryDependencyType);
            var secondaryDependencyMethod = secondaryDependencyType?.GetMethod(
                "GetBonus",
                BindingFlags.Public | BindingFlags.Static);
            var secondaryDependencyMethodDescription = secondaryDependencyMethod is null
                ? null
                : TypeReflections.GetDescription(secondaryDependencyMethod);
            var secondaryDependencyResult = secondaryDependencyMethod?.Invoke(null, null);
            var expectedSecondaryDependencyResult = isDeepGraphProfile ? 45 : 38;
            var tertiaryDependency = isWideDeepDiamondProfile
                ? context.Assemblies.FirstOrDefault(item =>
                    string.Equals(item.GetName().Name, TertiaryDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))
                : null;
            var tertiaryDependencyType = tertiaryDependency?.GetType(TertiaryDependencyTypeName, throwOnError: false);
            var tertiaryDependencyDescription = tertiaryDependencyType is null
                ? null
                : TypeReflections.GetDescription(tertiaryDependencyType);
            var tertiaryDependencyRuntimeDescription = tertiaryDependencyType is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(tertiaryDependencyType);
            var tertiaryDependencyMethod = tertiaryDependencyType?.GetMethod(
                "GetBonus",
                BindingFlags.Public | BindingFlags.Static);
            var tertiaryDependencyMethodDescription = tertiaryDependencyMethod is null
                ? null
                : TypeReflections.GetDescription(tertiaryDependencyMethod);
            var tertiaryDependencyResult = tertiaryDependencyMethod?.Invoke(null, null);
            var deepDependency = isDeepGraphProfile
                ? context.Assemblies.FirstOrDefault(item =>
                    string.Equals(item.GetName().Name, DeepDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))
                : null;
            var deepDependencyType = deepDependency?.GetType(DeepDependencyTypeName, throwOnError: false);
            var deepDependencyDescription = deepDependencyType is null
                ? null
                : TypeReflections.GetDescription(deepDependencyType);
            var deepDependencyRuntimeDescription = deepDependencyType is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(deepDependencyType);
            var deepDependencyMethod = deepDependencyType?.GetMethod(
                "GetBonus",
                BindingFlags.Public | BindingFlags.Static);
            var deepDependencyMethodDescription = deepDependencyMethod is null
                ? null
                : TypeReflections.GetDescription(deepDependencyMethod);
            var deepDependencyResult = deepDependencyMethod?.Invoke(null, null);
            var sharedTransitiveDependencyCount = context.Assemblies.Count(item =>
                string.Equals(item.GetName().Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase));
            var propertyDescription = TypeReflections.GetDescription(property);
            var methodDescription = TypeReflections.GetDescription(method);
            var fieldDescription = TypeReflections.GetDescription(field);

            return description == "Third-party non-trimmable type"
                && runtimeDescription?.Description == "Third-party non-trimmable type"
                && Equals(field.GetValue(instance), 19)
                && Equals(property.GetValue(instance), "loaded")
                && Equals(result, expectedPluginResult)
                && dependencyType is not null
                && Equals(dependency?.GetName().Version, expectedDependencyVersion)
                && dependencyDescription == expectedDependencyDescription
                && dependencyRuntimeDescription?.Description == expectedDependencyDescription
                && dependencyMethodDescription == expectedDependencyMethodDescription
                && Equals(dependencyResult, expectedDependencyResult)
                && (isVersionConflictProfile
                    || (transitiveDependencyType is not null
                        && transitiveDependencyDescription == expectedTransitiveDescription
                        && transitiveDependencyRuntimeDescription?.Description == expectedTransitiveDescription
                        && transitiveDependencyMethodDescription == expectedTransitiveMethodDescription
                        && Equals(transitiveDependencyResult, expectedTransitiveResult)))
                && (!(isDiamondProfile || isDeepGraphProfile)
                    || (secondaryDependency is not null
                        && secondaryDependencyType is not null
                        && secondaryDependencyDescription == "Third-party non-trimmable secondary dependency"
                        && secondaryDependencyRuntimeDescription?.Description == "Third-party non-trimmable secondary dependency"
                        && secondaryDependencyMethodDescription == "Third-party secondary dependency bonus"
                        && Equals(secondaryDependencyResult, expectedSecondaryDependencyResult)
                        && ReferenceEquals(AssemblyLoadContext.GetLoadContext(secondaryDependency), context)
                        && !ReferenceEquals(dependency, secondaryDependency)
                        && sharedTransitiveDependencyCount == 1
                        && dependency.GetReferencedAssemblies().Any(reference =>
                            string.Equals(reference.Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))
                        && secondaryDependency.GetReferencedAssemblies().Any(reference =>
                            string.Equals(reference.Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))))
                && (!isMixedVariantProfile
                    || (transitiveDependency is not null
                        && sharedTransitiveDependencyCount == 1
                        && ReferenceEquals(AssemblyLoadContext.GetLoadContext(dependency), context)
                        && ReferenceEquals(AssemblyLoadContext.GetLoadContext(transitiveDependency), context)
                        && dependency.GetReferencedAssemblies().Any(reference =>
                            string.Equals(reference.Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))))
                && (!isDeepGraphProfile
                    || (deepDependency is not null
                        && deepDependencyType is not null
                        && deepDependencyDescription == "Third-party non-trimmable deep dependency"
                        && deepDependencyRuntimeDescription?.Description == "Third-party non-trimmable deep dependency"
                        && deepDependencyMethodDescription == "Third-party deep dependency bonus"
                        && Equals(deepDependencyResult, 5)
                        && ReferenceEquals(AssemblyLoadContext.GetLoadContext(deepDependency), context)
                        && transitiveDependency is not null
                        && transitiveDependency.GetReferencedAssemblies().Any(reference =>
                            string.Equals(reference.Name, DeepDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))))
                && (!isWideDeepDiamondProfile
                    || (tertiaryDependency is not null
                        && tertiaryDependencyType is not null
                        && tertiaryDependencyDescription == "Third-party non-trimmable tertiary dependency"
                        && tertiaryDependencyRuntimeDescription?.Description == "Third-party non-trimmable tertiary dependency"
                        && tertiaryDependencyMethodDescription == "Third-party tertiary dependency bonus"
                        && Equals(tertiaryDependencyResult, 55)
                        && ReferenceEquals(AssemblyLoadContext.GetLoadContext(tertiaryDependency), context)
                        && !ReferenceEquals(dependency, tertiaryDependency)
                        && !ReferenceEquals(secondaryDependency, tertiaryDependency)
                        && tertiaryDependency.GetReferencedAssemblies().Any(reference =>
                            string.Equals(reference.Name, TransitiveDependencyAssemblyName, StringComparison.OrdinalIgnoreCase))))
                && propertyDescription == "Third-party name property"
                && methodDescription == "Third-party execute method"
                && fieldDescription == "Third-party value field"
                ? "metadata-preserved"
                : "member-trimmed";
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
    }

    private static string ReadStrongNameBoundary(AssemblyLoadContext context, Assembly assembly)
    {
        var type = assembly.GetType(StrongNameConflictTypeName, throwOnError: false);
        if (type is null)
            return "member-trimmed";

        try
        {
            var instance = Activator.CreateInstance(type);
            var method = type.GetMethod("GetDependencyMarker", BindingFlags.Public | BindingFlags.Instance);
            var marker = method?.Invoke(instance, null);
            // 先执行插件方法触发依赖解析，再从当前可回收上下文中检查实际程序集身份。
            var dependency = context.Assemblies.FirstOrDefault(item =>
                string.Equals(item.GetName().Name, StrongNameConflictDependencyAssemblyName,
                    StringComparison.OrdinalIgnoreCase));
            var dependencyType = dependency?.GetType(StrongNameConflictDependencyTypeName, throwOnError: false);
            var dependencyMethod = dependencyType?.GetMethod(
                "GetMarker", BindingFlags.Public | BindingFlags.Static);
            var dependencyMarker = dependencyMethod?.Invoke(null, null);
            var defaultDependency = AssemblyLoadContext.Default.LoadFromAssemblyName(
                new AssemblyName(StrongNameConflictDependencyAssemblyName));
            var pluginReference = assembly.GetReferencedAssemblies().FirstOrDefault(item =>
                string.Equals(item.Name, StrongNameConflictDependencyAssemblyName,
                    StringComparison.OrdinalIgnoreCase));
            var dependencyToken = dependency?.GetName().GetPublicKeyToken() ?? Array.Empty<byte>();
            var defaultToken = defaultDependency.GetName().GetPublicKeyToken() ?? Array.Empty<byte>();
            var referenceToken = pluginReference?.GetPublicKeyToken() ?? Array.Empty<byte>();
            var dependencyContext = dependency is null ? null : AssemblyLoadContext.GetLoadContext(dependency);

            var preserved = dependency is not null
                && dependencyType is not null
                && dependencyMethod is not null
                && Equals(dependencyMarker, 41)
                && instance is not null
                && method is not null
                && Equals(marker, 41)
                && !ReferenceEquals(dependency, defaultDependency)
                && dependencyContext is not null
                && !ReferenceEquals(dependencyContext, AssemblyLoadContext.Default)
                && dependency.GetName().Name == defaultDependency.GetName().Name
                && dependency.GetName().Version != defaultDependency.GetName().Version
                && dependencyToken.Length == 0
                && referenceToken.Length == 0
                && defaultToken.Length > 0
                ? "metadata-preserved"
                : "member-trimmed";
            return preserved;
        }
        catch (Exception exception) when (IsBoundaryFailure(exception))
        {
            return "member-trimmed";
        }
    }

    private static string? FindHostPath(string targetDirectory) =>
        new[] { "ReflectionTrimProbe.dll", "ReflectionTrimProbe.exe" }
            .Select(name => Path.Combine(targetDirectory, name))
            .FirstOrDefault(File.Exists);

    private static bool IsBoundaryFailure(Exception exception) => exception is
        FileLoadException or FileNotFoundException or BadImageFormatException or
        TypeLoadException or MissingMemberException or MemberAccessException or
        TargetInvocationException or ArgumentException or InvalidOperationException or
        IOException or UnauthorizedAccessException;

    private sealed class MultiPluginSpec
    {
        public MultiPluginSpec(
            string directory,
            string assemblyName,
            string? profile,
            string dependencyAssemblyName,
            Version? expectedDependencyVersion)
        {
            Directory = directory;
            AssemblyName = assemblyName;
            Profile = profile;
            DependencyAssemblyName = dependencyAssemblyName;
            ExpectedDependencyVersion = expectedDependencyVersion;
        }

        public string Directory { get; }

        public string AssemblyName { get; }

        public string? Profile { get; }

        public string DependencyAssemblyName { get; }

        public Version? ExpectedDependencyVersion { get; }

        public bool IsStrongName => Profile == StrongNameConflictProfile;
    }

    private sealed class MultiPluginLoadResult
    {
        public MultiPluginLoadResult(string boundary, WeakReference[] contexts)
        {
            Boundary = boundary;
            Contexts = contexts;
        }

        public string Boundary { get; }

        public WeakReference[] Contexts { get; }
    }

    private sealed class ResolverLoadContext : AssemblyLoadContext
    {
        private readonly string _assemblyPath;
        private readonly string _targetDirectory;
        private readonly string _pluginAssemblyName;
        private readonly AssemblyDependencyResolver _resolver;
        private readonly byte[]? _pluginBytes;

        public ResolverLoadContext(string hostPath, string assemblyPath, byte[]? pluginBytes = null)
            : base("ReflectionTrimProbe.NonTrimmablePlugin.Resolver", isCollectible: true)
        {
            _assemblyPath = assemblyPath;
            _targetDirectory = Path.GetDirectoryName(assemblyPath)!;
            _pluginAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            _resolver = new AssemblyDependencyResolver(hostPath);
            _pluginBytes = pluginBytes;
        }

        public Assembly LoadPlugin() => _pluginBytes is null
            ? LoadFromAssemblyPath(_assemblyPath)
            : LoadFromStream(new MemoryStream(_pluginBytes, writable: false));

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (string.Equals(assemblyName.Name, _pluginAssemblyName, StringComparison.OrdinalIgnoreCase))
                return LoadPlugin();
            var localPath = Path.Combine(_targetDirectory, (assemblyName.Name ?? string.Empty) + ".dll");
            if (string.Equals(assemblyName.Name, StrongNameConflictDependencyAssemblyName,
                    StringComparison.OrdinalIgnoreCase)
                && File.Exists(localPath))
                return LoadFromAssemblyPath(localPath);
            if (assemblyName.Name is "System" or "netstandard"
                || assemblyName.Name?.StartsWith("System.", StringComparison.Ordinal) == true
                || assemblyName.Name?.StartsWith("Microsoft.", StringComparison.Ordinal) == true)
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            if (File.Exists(localPath))
                return LoadFromAssemblyPath(localPath);
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path is null ? null : LoadFromAssemblyPath(path);
        }
    }
}
