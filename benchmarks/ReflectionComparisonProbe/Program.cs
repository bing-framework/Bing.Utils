using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Length is not (2 or 3))
    throw new ArgumentException("Expected Bing.Utils.dll and Bing.Utils.Reflection.dll paths, optionally followed by --steady, --getter-only, --path-write-only or --cold=<scenario>.");

var core = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var reflection = Assembly.LoadFrom(Path.GetFullPath(args[1]));
var cacheMetrics = CacheMetricProbe.Create(core, reflection);
var typeVisit = reflection.GetType("Bing.Reflection.TypeVisit", throwOnError: true)!;
var propertyExtensions = core.GetType("Bing.Reflection.PropertyInfoExtensions", throwOnError: true)!;
var memberPaths = core.GetType("Bing.Reflection.Reflections", throwOnError: true)!;
var memberPathOptions = core.GetType("Bing.Reflection.MemberPathOptions");
var sampleType = typeof(ReflectionProbeSample);
var sample = new ReflectionProbeSample(1);
var property = sampleType.GetProperty(nameof(ReflectionProbeSample.Value))!;
var method = sampleType.GetMethod(nameof(ReflectionProbeSample.Execute))!;
var alternateMethod = sampleType.GetMethod(nameof(ReflectionProbeSample.ExecuteWithValue))!;
var create = typeVisit.GetMethods().Single(item => item.Name == "CreateInstance"
    && !item.IsGenericMethod && item.GetParameters().Length == 2
    && item.GetParameters()[0].ParameterType == typeof(Type));
var signature = typeVisit.GetMethod("GetMethodBySignature", new[] { typeof(Type), typeof(MethodInfo) })!;
var getterFactory = propertyExtensions.GetMethods().Single(item => item.Name == "GetValueGetter"
    && !item.IsGenericMethod && item.GetParameters().Length == 1);
var setterFactory = propertyExtensions.GetMethods().Single(item => item.Name == "GetValueSetter"
    && !item.IsGenericMethod && item.GetParameters().Length == 1);
var readPath = memberPaths.GetMethods().Single(item => item.Name == "TryGetMemberValueByPath");
var writePath = memberPaths.GetMethods().Single(item => item.Name == "TrySetMemberValueByPath"
    && !item.IsGenericMethod);
var createDynamic = typeVisit.GetMethods().Single(item => item.Name == "CreateDynamicType");
Console.WriteLine($"# runtime: {RuntimeInformation.FrameworkDescription}; core: {core.Location}; reflection: {reflection.Location}");
if (args.Length == 3 && args[2].StartsWith("--cold=", StringComparison.Ordinal))
{
    var scenario = args[2]["--cold=".Length..];
    Func<object?> action = scenario switch
    {
        "constructor" => () => create.Invoke(null, new object?[] { sampleType, new object[] { 1 } }),
        "getter" => () => getterFactory.Invoke(null, new object[] { property }),
        "setter" => () => setterFactory.Invoke(null, new object[] { property }),
        "signature" => () => signature.Invoke(null, new object[] { sampleType, method }),
        "path-read" => () => readPath.Invoke(null, new object?[] { sample, nameof(ReflectionProbeSample.Value), null, null }),
        "path-write" => () => writePath.Invoke(null, new object?[] { sample, nameof(ReflectionProbeSample.Value), 1, null }),
        "dynamic" => () => createDynamic.Invoke(null, new object[] { new Dictionary<string, Type> { ["Value"] = typeof(int) } }),
        _ => throw new ArgumentException($"Unknown cold scenario: {scenario}")
    };
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    var measurement = cacheMetrics.Start();
    try
    {
        var result = action();
        GC.KeepAlive(result);
    }
    finally
    {
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        Console.WriteLine($"cold {scenario},{elapsedUs:F1} us/op,{GC.GetAllocatedBytesForCurrentThread() - allocated:F1} B/op");
        cacheMetrics.Stop($"cold {scenario}", measurement);
    }
    return;
}

var getter = (Func<object, object>)getterFactory.Invoke(null, new object[] { property })!;
var setter = (Action<object, object>)setterFactory.Invoke(null, new object[] { property })!;
var schema = new Dictionary<string, Type> { ["Value"] = typeof(int), ["Name"] = typeof(string) };
var constructorArgs = new object?[] { sampleType, new object[] { 1 } };
var signatureArgs = new object?[] { sampleType, method };
var alternateSignatureArgs = new object?[] { sampleType, alternateMethod };
var readArgs = new object?[] { sample, nameof(ReflectionProbeSample.Value), null, null };
var writeArgs = new object?[] { sample, nameof(ReflectionProbeSample.Value), 1, null };
var explicitOptions = memberPathOptions == null ? null : Activator.CreateInstance(memberPathOptions);
var explicitReadArgs = new object?[] { sample, nameof(ReflectionProbeSample.Value), null, explicitOptions };
var explicitWriteArgs = new object?[] { sample, nameof(ReflectionProbeSample.Value), 1, explicitOptions };
var dynamicArgs = new object?[] { schema };

if (args.Length == 3 && args[2] == "--getter-only")
{
    Measure("direct property getter", () => sample.Value, cacheMetrics);
    Measure("isolated property getter", () => getter(sample), cacheMetrics);
    return;
}
if (args.Length == 3 && args[2] == "--path-write-only")
{
    Measure("isolated member path write", () => writePath.Invoke(null, writeArgs), cacheMetrics);
    if (explicitOptions != null)
        Measure("isolated member path write explicit options", () => writePath.Invoke(null, explicitWriteArgs), cacheMetrics);
    return;
}
if (args.Length == 3 && args[2] != "--steady")
    throw new ArgumentException($"Unknown probe mode: {args[2]}");

Measure("constructor", () => create.Invoke(null, constructorArgs), cacheMetrics);
Measure("property getter", () => getter(sample), cacheMetrics);
Measure("property setter", () => { setter(sample, 1); return null; }, cacheMetrics);
Measure("method signature", () => signature.Invoke(null, signatureArgs), cacheMetrics);
var signatureIndex = 0;
Measure("alternating method signatures", () =>
    signature.Invoke(null, (++signatureIndex & 1) == 0 ? signatureArgs : alternateSignatureArgs), cacheMetrics);
Measure("member path read", () => readPath.Invoke(null, readArgs), cacheMetrics);
Measure("member path write", () => writePath.Invoke(null, writeArgs), cacheMetrics);
if (explicitOptions != null)
{
    Measure("member path read explicit options", () => readPath.Invoke(null, explicitReadArgs), cacheMetrics);
    Measure("member path write explicit options", () => writePath.Invoke(null, explicitWriteArgs), cacheMetrics);
}
Measure("dynamic schema hit", () => createDynamic.Invoke(null, dynamicArgs), cacheMetrics);
Measure("equivalent fresh schema", () => createDynamic.Invoke(null, new object?[]
{
    new Dictionary<string, Type> { ["Name"] = typeof(string), ["Value"] = typeof(int) }
}), cacheMetrics);

static void Measure(string name, Func<object?> action, CacheMetricProbe cacheMetrics)
{
    var measurement = cacheMetrics.Start();
    try
    {
        for (var index = 0; index < 10_000; index++)
            action();

        const int count = 50_000;
        var times = new double[5];
        var allocations = new double[5];
        for (var repetition = 0; repetition < times.Length; repetition++)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            object? last = null;
            for (var index = 0; index < count; index++)
                last = action();
            times[repetition] = (Stopwatch.GetTimestamp() - started) * 1_000_000_000.0 / Stopwatch.Frequency / count;
            allocations[repetition] = (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)count;
            GC.KeepAlive(last);
        }

        Array.Sort(times);
        Array.Sort(allocations);
        Console.WriteLine($"{name},{times[2]:F1} ns/op,{allocations[2]:F1} B/op");
    }
    finally
    {
        cacheMetrics.Stop(name, measurement);
    }
}

/// <summary>
/// Reads the internal cache diagnostics without making them part of the public library API.
/// The probe remains compatible with older baseline assemblies that do not expose the counters.
/// </summary>
internal sealed class CacheMetricProbe
{
    private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly FieldInfo _coreEnabled = null!;
    private readonly FieldInfo _reflectionEnabled = null!;
    private readonly MethodInfo _coreSnapshot = null!;
    private readonly MethodInfo _reflectionSnapshot = null!;
    private readonly PropertyInfo _reflectionStructuralBypasses = null!;
    private readonly PropertyInfo _constructorInvokerBuildCount = null!;
    private readonly PropertyInfo _dynamicTypeBuildCount = null!;
    private readonly string? _unavailableReason;

    private CacheMetricProbe(
        FieldInfo coreEnabled,
        FieldInfo reflectionEnabled,
        MethodInfo coreSnapshot,
        MethodInfo reflectionSnapshot,
        PropertyInfo reflectionStructuralBypasses,
        PropertyInfo constructorInvokerBuildCount,
        PropertyInfo dynamicTypeBuildCount)
    {
        _coreEnabled = coreEnabled;
        _reflectionEnabled = reflectionEnabled;
        _coreSnapshot = coreSnapshot;
        _reflectionSnapshot = reflectionSnapshot;
        _reflectionStructuralBypasses = reflectionStructuralBypasses;
        _constructorInvokerBuildCount = constructorInvokerBuildCount;
        _dynamicTypeBuildCount = dynamicTypeBuildCount;
    }

    private CacheMetricProbe(string unavailableReason) => _unavailableReason = unavailableReason;

    internal static CacheMetricProbe Create(Assembly core, Assembly reflection)
    {
        try
        {
            var coreMetrics = core.GetType("Bing.Reflection.CoreCacheMetrics", throwOnError: true)!;
            var reflectionMetrics = reflection.GetType("Bing.Reflection.ReflectionCacheMetrics", throwOnError: true)!;
            var binder = core.GetType("Bing.Reflection.StrictConstructorBinder", throwOnError: true)!;
            var typeFactory = reflection.GetType("Bing.Reflection.TypeFactory", throwOnError: true)!;
            return new CacheMetricProbe(
                GetRequiredField(coreMetrics, "Enabled"),
                GetRequiredField(reflectionMetrics, "Enabled"),
                GetRequiredMethod(coreMetrics, "Snapshot"),
                GetRequiredMethod(reflectionMetrics, "Snapshot"),
                GetRequiredProperty(reflectionMetrics, "StructuralBypassCount"),
                GetRequiredProperty(binder, "ConstructorInvokerBuildCount"),
                GetRequiredProperty(typeFactory, "BuildCount"));
        }
        catch (Exception exception) when (exception is MissingMemberException or TypeLoadException or InvalidOperationException)
        {
            return new CacheMetricProbe($"cache metrics are unavailable: {exception.Message}");
        }
    }

    internal Measurement Start()
    {
        if (_unavailableReason != null)
            return default;

        var before = Capture();
        var coreWasEnabled = (bool)_coreEnabled.GetValue(null)!;
        var reflectionWasEnabled = (bool)_reflectionEnabled.GetValue(null)!;
        _coreEnabled.SetValue(null, true);
        _reflectionEnabled.SetValue(null, true);
        return new Measurement(before, coreWasEnabled, reflectionWasEnabled);
    }

    internal void Stop(string scope, Measurement measurement)
    {
        if (_unavailableReason != null)
        {
            Console.WriteLine($"cache-metrics,{JsonSerializer.Serialize(new { scope, available = false, reason = _unavailableReason })}");
            return;
        }

        var after = Capture();
        _coreEnabled.SetValue(null, measurement.CoreWasEnabled);
        _reflectionEnabled.SetValue(null, measurement.ReflectionWasEnabled);
        var delta = after - measurement.Before;
        Console.WriteLine($"cache-metrics,{JsonSerializer.Serialize(new
        {
            scope,
            available = true,
            coreHits = delta.Core.Hits,
            coreMisses = delta.Core.Misses,
            corePending = delta.Core.Pending,
            coreBypasses = delta.Core.Bypasses,
            coreFailures = delta.Core.Failures,
            reflectionHits = delta.Reflection.Hits,
            reflectionMisses = delta.Reflection.Misses,
            reflectionPending = delta.Reflection.Pending,
            reflectionBypasses = delta.Reflection.Bypasses,
            reflectionFailures = delta.Reflection.Failures,
            reflectionStructuralBypasses = delta.ReflectionStructuralBypasses,
            constructorInvokerBuilds = delta.ConstructorInvokerBuilds,
            dynamicTypeBuilds = delta.DynamicTypeBuilds
        })}");
    }

    private Snapshot Capture()
    {
        var core = ReadSnapshot(_coreSnapshot);
        var reflection = ReadSnapshot(_reflectionSnapshot);
        return new Snapshot(
            core,
            reflection,
            Convert.ToInt64(_reflectionStructuralBypasses.GetValue(null)),
            Convert.ToInt64(_constructorInvokerBuildCount.GetValue(null)),
            Convert.ToInt64(_dynamicTypeBuildCount.GetValue(null)));
    }

    private static FieldInfo GetRequiredField(Type type, string name) =>
        type.GetField(name, StaticMembers) ?? throw new MissingFieldException(type.FullName, name);

    private static MethodInfo GetRequiredMethod(Type type, string name) =>
        type.GetMethod(name, StaticMembers) ?? throw new MissingMethodException(type.FullName, name);

    private static PropertyInfo GetRequiredProperty(Type type, string name) =>
        type.GetProperty(name, StaticMembers) ?? throw new MissingMemberException(type.FullName, name);

    private static CacheSnapshot ReadSnapshot(MethodInfo method)
    {
        var tuple = (System.Runtime.CompilerServices.ITuple)method.Invoke(null, null)!;
        return new CacheSnapshot(
            Convert.ToInt64(tuple[0]),
            Convert.ToInt64(tuple[1]),
            Convert.ToInt64(tuple[2]),
            Convert.ToInt64(tuple[3]),
            Convert.ToInt64(tuple[4]));
    }

    internal readonly record struct Measurement(Snapshot Before, bool CoreWasEnabled, bool ReflectionWasEnabled);

    internal readonly record struct CacheSnapshot(long Hits, long Misses, long Pending, long Bypasses, long Failures)
    {
        public static CacheSnapshot operator -(CacheSnapshot left, CacheSnapshot right) =>
            new(left.Hits - right.Hits, left.Misses - right.Misses, left.Pending - right.Pending,
                left.Bypasses - right.Bypasses, left.Failures - right.Failures);
    }

    internal readonly record struct Snapshot(
        CacheSnapshot Core,
        CacheSnapshot Reflection,
        long ReflectionStructuralBypasses,
        long ConstructorInvokerBuilds,
        long DynamicTypeBuilds)
    {
        public static Snapshot operator -(Snapshot left, Snapshot right) =>
            new(left.Core - right.Core, left.Reflection - right.Reflection,
                left.ReflectionStructuralBypasses - right.ReflectionStructuralBypasses,
                left.ConstructorInvokerBuilds - right.ConstructorInvokerBuilds,
                left.DynamicTypeBuilds - right.DynamicTypeBuilds);
    }
}

public sealed class ReflectionProbeSample
{
    public ReflectionProbeSample(int value) => Value = value;
    public int Value { get; set; }
    public void Execute() { }
    public void ExecuteWithValue(int value) { }
}
