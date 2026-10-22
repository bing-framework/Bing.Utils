using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace RuntimeCycleFixture;

/// <summary>
/// 以三种加载入口验证真实互相 AssemblyRef、多分支和深分支引用在 collectible ALC 中的行为。
/// </summary>
public static class CycleLoader
{
    private const string AAssemblyName = "RuntimeCycleFixture.A";
    private const string BAssemblyName = "RuntimeCycleFixture.B";
    private const string CAssemblyName = "RuntimeCycleFixture.C";
    private const string DAssemblyName = "RuntimeCycleFixture.D";
    private const string EAssemblyName = "RuntimeCycleFixture.E";
    private const string ATypeName = "RuntimeCycleFixture.A.AMarker";
    private const string BTypeName = "RuntimeCycleFixture.B.BMarker";
    private const string CTypeName = "RuntimeCycleFixture.C.CMarker";
    private const string DTypeName = "RuntimeCycleFixture.D.DMarker";
    private const string ETypeName = "RuntimeCycleFixture.E.EMarker";

    /// <summary>
    /// 加载指定目录中的 A/B/C/D/E 程序集并验证循环引用、分支引用和卸载。
    /// </summary>
    /// <param name="args">第一个参数为输出目录，第二个参数为 path、resolver 或 bytes。</param>
    /// <returns>全部验证通过时返回 0，否则返回 1；参数错误返回 2。</returns>
    public static int Main(string[] args)
    {
        if (args.Length != 2 || args[1] is not ("path" or "resolver" or "bytes"))
        {
            Console.Error.WriteLine("Usage: CycleLoader <directory> <path|resolver|bytes>");
            return 2;
        }

        var directory = Path.GetFullPath(args[0]);
        var mode = args[1];
        var result = LoadAndRelease(directory, mode);
        var unloaded = WaitForUnload(result.References);
        var assemblyMarker = result.AssemblyReferencesVerified ? "verified" : "failed";
        var branchMarker = result.BranchVerified ? "verified" : "failed";
        var multiBranchMarker = result.MultiBranchVerified ? "verified" : "failed";
        var deepBranchMarker = result.DeepBranchVerified ? "verified" : "failed";
        var unloadMarker = unloaded ? "verified" : "failed";
        Console.WriteLine($"runtime-cycle-assemblyrefs:{mode}:{assemblyMarker}");
        Console.WriteLine($"runtime-cycle-collectible:{mode}:{unloadMarker}");
        Console.WriteLine($"runtime-cycle-branch:{mode}:{branchMarker}");
        Console.WriteLine($"runtime-cycle-multi-branch:{mode}:{multiBranchMarker}");
        Console.WriteLine($"runtime-cycle-deep-branch:{mode}:{deepBranchMarker}");
        return result.AssemblyReferencesVerified
            && result.BranchVerified
            && result.MultiBranchVerified
            && result.DeepBranchVerified
            && unloaded ? 0 : 1;
    }

    private static LoadResult LoadAndRelease(string directory, string mode)
    {
        var references = new List<WeakReference>();
        FixtureLoadContext? context = null;
        try
        {
            context = new FixtureLoadContext(directory);
            references.Add(new WeakReference(context));
            var aAssembly = context.LoadAssembly(AAssemblyName, mode);
            var bAssembly = context.LoadAssembly(BAssemblyName, mode);
            var cAssembly = context.LoadAssembly(CAssemblyName, mode);
            var dAssembly = context.LoadAssembly(DAssemblyName, mode);
            var eAssembly = context.LoadAssembly(EAssemblyName, mode);
            references.Add(new WeakReference(aAssembly));
            references.Add(new WeakReference(bAssembly));
            references.Add(new WeakReference(cAssembly));
            references.Add(new WeakReference(dAssembly));
            references.Add(new WeakReference(eAssembly));

            var aType = aAssembly.GetType(ATypeName, throwOnError: false);
            var bType = bAssembly.GetType(BTypeName, throwOnError: false);
            var cType = cAssembly.GetType(CTypeName, throwOnError: false);
            var dType = dAssembly.GetType(DTypeName, throwOnError: false);
            var eType = eAssembly.GetType(ETypeName, throwOnError: false);
            var aPeer = aType?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var bPeer = bType?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var cPeer = cType?.GetField("Peer", BindingFlags.Public | BindingFlags.Instance);
            var dRootPeer = dType?.GetField("RootPeer", BindingFlags.Public | BindingFlags.Instance);
            var dBranchPeer = dType?.GetField("BranchPeer", BindingFlags.Public | BindingFlags.Instance);
            var eDeepPeer = eType?.GetField("DeepPeer", BindingFlags.Public | BindingFlags.Instance);
            var eCyclePeer = eType?.GetField("CyclePeer", BindingFlags.Public | BindingFlags.Instance);
            var aReferencesB = aAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, BAssemblyName, StringComparison.Ordinal));
            var bReferencesA = bAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, AAssemblyName, StringComparison.Ordinal));
            var aContext = AssemblyLoadContext.GetLoadContext(aAssembly);
            var bContext = AssemblyLoadContext.GetLoadContext(bAssembly);
            var cContext = AssemblyLoadContext.GetLoadContext(cAssembly);
            var dContext = AssemblyLoadContext.GetLoadContext(dAssembly);
            var eContext = AssemblyLoadContext.GetLoadContext(eAssembly);
            var verified = aReferencesB
                && bReferencesA
                && aPeer?.FieldType.Assembly == bAssembly
                && bPeer?.FieldType.Assembly == aAssembly
                && aContext is not null
                && ReferenceEquals(aContext, bContext)
                && aContext.IsCollectible;
            var branchVerified = cAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, BAssemblyName, StringComparison.Ordinal))
                && cPeer is not null
                && cPeer.FieldType == bType
                && cPeer.FieldType.Assembly == bAssembly
                && cContext is not null
                && ReferenceEquals(cContext, aContext)
                && ReferenceEquals(cContext, bContext)
                && cContext.IsCollectible;
            var multiBranchVerified = dAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, AAssemblyName, StringComparison.Ordinal))
                && dAssembly.GetReferencedAssemblies()
                    .Any(reference => string.Equals(reference.Name, CAssemblyName, StringComparison.Ordinal))
                && dRootPeer is not null
                && dRootPeer.FieldType == aType
                && dRootPeer.FieldType.Assembly == aAssembly
                && dBranchPeer is not null
                && dBranchPeer.FieldType == cType
                && dBranchPeer.FieldType.Assembly == cAssembly
                && dContext is not null
                && ReferenceEquals(dContext, aContext)
                && ReferenceEquals(dContext, bContext)
                && ReferenceEquals(dContext, cContext)
                && dContext.IsCollectible;
            var deepBranchVerified = eAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, DAssemblyName, StringComparison.Ordinal))
                && eAssembly.GetReferencedAssemblies()
                    .Any(reference => string.Equals(reference.Name, BAssemblyName, StringComparison.Ordinal))
                && eDeepPeer is not null
                && eDeepPeer.FieldType == dType
                && eDeepPeer.FieldType.Assembly == dAssembly
                && eCyclePeer is not null
                && eCyclePeer.FieldType == bType
                && eCyclePeer.FieldType.Assembly == bAssembly
                && eContext is not null
                && ReferenceEquals(eContext, aContext)
                && ReferenceEquals(eContext, bContext)
                && ReferenceEquals(eContext, cContext)
                && ReferenceEquals(eContext, dContext)
                && eContext.IsCollectible;

            return new LoadResult(verified, branchVerified, multiBranchVerified, deepBranchVerified, references.ToArray());
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"runtime-cycle-error:{mode}:{exception.GetType().Name}:{exception.Message}");
            return new LoadResult(false, false, false, false, references.ToArray());
        }
        finally
        {
            context?.Unload();
        }
    }

    private static bool WaitForUnload(IReadOnlyCollection<WeakReference> references)
    {
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

    private sealed class LoadResult
    {
        public LoadResult(
            bool assemblyReferencesVerified,
            bool branchVerified,
            bool multiBranchVerified,
            bool deepBranchVerified,
            WeakReference[] references)
        {
            AssemblyReferencesVerified = assemblyReferencesVerified;
            BranchVerified = branchVerified;
            MultiBranchVerified = multiBranchVerified;
            DeepBranchVerified = deepBranchVerified;
            References = references;
        }

        public bool AssemblyReferencesVerified { get; }

        public bool BranchVerified { get; }

        public bool MultiBranchVerified { get; }

        public bool DeepBranchVerified { get; }

        public WeakReference[] References { get; }
    }

    private sealed class FixtureLoadContext : AssemblyLoadContext
    {
        private readonly string _directory;

        public FixtureLoadContext(string directory)
            : base("RuntimeCycleFixture", isCollectible: true)
        {
            _directory = directory;
        }

        public Assembly LoadAssembly(string assemblyName, string mode)
        {
            var path = Path.Combine(_directory, assemblyName + ".dll");
            return mode switch
            {
                "path" => LoadFromAssemblyPath(path),
                "resolver" => LoadFromAssemblyName(new AssemblyName(assemblyName)),
                "bytes" => LoadFromStream(new MemoryStream(File.ReadAllBytes(path), writable: false)),
                _ => throw new ArgumentException($"Unsupported load mode: {mode}", nameof(mode))
            };
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not (AAssemblyName or BAssemblyName or CAssemblyName or DAssemblyName or EAssemblyName))
                return null;

            var path = Path.Combine(_directory, assemblyName.Name + ".dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }
    }
}
