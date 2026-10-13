using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bing.Dynamic;
using Bing.Helpers;
using Bing.Reflection;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>反射入口的冷启动和稳态性能探针。</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReflectionBenchmarks
{
    private readonly ReflectionSample _sample = new();
    private readonly PropertyInfo _property = typeof(ReflectionSample).GetProperty(nameof(ReflectionSample.Value))!;
    private readonly MethodInfo _method = typeof(ReflectionSample).GetMethod(nameof(ReflectionSample.Execute))!;
    private readonly Dictionary<string, Type> _schema = new() { ["Value"] = typeof(int), ["Name"] = typeof(string) };
    private Func<ReflectionSample, object> _getter = null!;
    private Action<ReflectionSample, object> _setter = null!;

    [GlobalSetup]
    public void Setup()
    {
        _getter = _property.GetValueGetter<ReflectionSample>();
        _setter = _property.GetValueSetter<ReflectionSample>();
        TypeVisit.CreateInstance<ReflectionSample>(1);
        TypeVisit.GetMethodBySignature(typeof(ReflectionSample), _method);
        Reflections.TryGetMemberValueByPath(_sample, nameof(ReflectionSample.Value), out _);
        TypeVisit.CreateDynamicType(_schema);
    }

    [Benchmark]
    public object Constructor() => TypeVisit.CreateInstance<ReflectionSample>(1);

    [Benchmark]
    public object PropertyGetter() => _getter(_sample);

    [Benchmark]
    public void PropertySetter() => _setter(_sample, 1);

    [Benchmark]
    public MethodInfo MethodSignature() => TypeVisit.GetMethodBySignature(typeof(ReflectionSample), _method);

    [Benchmark]
    public object MemberPathRead()
    {
        Reflections.TryGetMemberValueByPath(_sample, nameof(ReflectionSample.Value), out var value);
        return value;
    }

    [Benchmark]
    public void MemberPathWrite() => Reflections.TrySetMemberValueByPath(_sample, nameof(ReflectionSample.Value), 1);

    [Benchmark]
    public Type DynamicTypeCacheHit() => TypeVisit.CreateDynamicType(_schema);

    /// <summary>逐项观察首次调用成本；每个场景只执行一次。</summary>
    public static void RunColdStartProbe()
    {
        Measure("constructor", () => TypeVisit.CreateInstance(typeof(ColdConstructor), 1));
        var property = typeof(ColdProperty).GetProperty(nameof(ColdProperty.Value))!;
        Measure("getter compile", () => property.GetValueGetter<ColdProperty>());
        Measure("setter compile", () => property.GetValueSetter<ColdProperty>());
        var method = typeof(ColdMethod).GetMethod(nameof(ColdMethod.Execute))!;
        Measure("method signature", () => TypeVisit.GetMethodBySignature(typeof(ColdMethod), method));
        Measure("member path", () => Reflections.TryGetMemberValueByPath(new ColdProperty(), nameof(ColdProperty.Value), out _));
        Measure("member path write", () => Reflections.TrySetMemberValueByPath(new ColdWriteProperty(), nameof(ColdWriteProperty.Value), 1));
        Measure("dynamic schema", () => TypeVisit.CreateDynamicType(new Dictionary<string, Type> { ["ColdValue"] = typeof(long) }));
    }

    /// <summary>观察同一全新模式的并发首次创建；返回类型一致性不等于内部 Emit 次数。</summary>
    public static void RunConcurrentSchemaProbe()
    {
        var wasEnabled = ReflectionCacheMetrics.Enabled;
        ReflectionCacheMetrics.Enabled = true;
        try
        {
        const int concurrency = 8;
        var schema = new Dictionary<string, Type>
        {
            [$"Cold_{Guid.NewGuid():N}"] = typeof(long),
            ["Name"] = typeof(string)
        };
        using var ready = new CountdownEvent(concurrency);
        using var start = new ManualResetEventSlim();
        var tasks = Enumerable.Range(0, concurrency).Select(_ => Task.Factory.StartNew(() =>
        {
            ready.Signal();
            start.Wait();
            return TypeVisit.CreateDynamicType(schema);
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        ready.Wait();
        var buildsBefore = TypeFactory.BuildCount;
        var cacheBefore = ReflectionCacheMetrics.Snapshot();
        var allocated = GC.GetTotalAllocatedBytes(true);
        var started = Stopwatch.GetTimestamp();
        start.Set();
        Task.WaitAll(tasks);
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated;
        var types = tasks.Select(task => task.Result).ToArray();
        var matching = types.Count(type => ReferenceEquals(type, types[0]));
        var builds = TypeFactory.BuildCount - buildsBefore;
        var cold = CacheDelta(cacheBefore, ReflectionCacheMetrics.Snapshot());
        var warmBefore = ReflectionCacheMetrics.Snapshot();
        Parallel.For(0, concurrency, _ => TypeVisit.CreateDynamicType(schema));
        var warm = CacheDelta(warmBefore, ReflectionCacheMetrics.Snapshot());
        if (TypeFactory.BuildCount != buildsBefore + builds)
            throw new InvalidOperationException("重复创建相同动态类型时不应再次 Emit。");
        Console.WriteLine($"concurrent cold schema ({concurrency} callers): {elapsedUs:F1} us, " +
                          $"{allocatedBytes} B, " +
                          $"same returned type: {matching}/{concurrency}, " +
                          $"distinct returned types: {types.Distinct().Count()}, " +
                          $"Emit builds: {builds}");
        PrintCacheMetrics("schema cold", concurrency, cold);
        PrintCacheMetrics("schema warm", concurrency, warm);
        }
        finally
        {
            ReflectionCacheMetrics.Enabled = wasEnabled;
        }
    }

    /// <summary>验证类型级缓存达到容量后旁路解析，并输出高基数输入的旁路计数。</summary>
    /// <param name="args">可选容量探针参数。</param>
    public static void RunCapacityProbe(IReadOnlyList<string>? args = null)
    {
        var options = CapacityProbeOptions.Parse(args ?? Array.Empty<string>());
        var wasEnabled = ReflectionCacheMetrics.Enabled;
        var coreWasEnabled = CoreCacheMetrics.Enabled;
        ReflectionCacheMetrics.Enabled = true;
        CoreCacheMetrics.Enabled = true;
        try
        {
            var started = Stopwatch.GetTimestamp();
            var namedMethod = RunNamedMethodCapacityProbe();
            var methodSignature = RunMethodSignatureCapacityProbe();
            var memberPath = RunMemberPathCapacityProbe();
            var flaggedMembers = RunFlaggedMemberCapacityProbe();
            var dynamicProperty = RunDynamicPropertyCapacityProbe();
            var structMap = RunStructMapCapacityProbe();
            var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
            var totalBypasses = namedMethod.Bypasses + methodSignature.Bypasses
                + memberPath.Bypasses + flaggedMembers.Bypasses + dynamicProperty.Bypasses
                + structMap.Bypasses;
            var structuralPassed = namedMethod.StructuralPassed && methodSignature.StructuralPassed
                && memberPath.StructuralPassed && flaggedMembers.StructuralPassed
                && dynamicProperty.StructuralPassed && structMap.StructuralPassed;
            // 旁路预算保持与历史 probe 一致，只约束原有命名方法场景；其他场景通过结构性断言单独报告。
            var budgetPassed = (!options.MaxBypasses.HasValue || namedMethod.Bypasses <= options.MaxBypasses.Value)
                && (!options.MaxElapsedUs.HasValue || elapsedUs <= options.MaxElapsedUs.Value);
            var passed = structuralPassed && budgetPassed;
            var capacities = new
            {
                constructor = StrictConstructorBinder.ConstructorCacheCapacity,
                memberPath = Reflections.MemberPathCacheCapacity,
                flaggedMemberSnapshots = TypeReflections.TypeCacheManager.FlaggedCacheCapacity,
                methodSignature = TypeVisit.MethodSignatureCacheCapacity,
                namedMethod = TypeVisit.NamedMethodCacheCapacity,
                dynamicBaseProperty = DynamicBase.PropertyCacheCapacity,
                dynamicTypeSchema = TypeFactory.MaxGeneratedTypes,
                dynamicTypeProperties = TypeFactory.MaxProperties,
                structMap = MapperHelper.StructMapCacheCapacity
            };
            var report = new
            {
                schemaVersion = 3,
                probe = "reflection-capacity",
                capacity = namedMethod.Capacity,
                requests = namedMethod.Requests,
                elapsedUs,
                hits = namedMethod.Hits,
                admitted = namedMethod.Admitted,
                pending = namedMethod.Pending,
                bypass = namedMethod.Bypasses,
                bypassRatio = namedMethod.Bypasses / (double)namedMethod.Requests,
                admissionRatio = namedMethod.Admitted / (double)namedMethod.Requests,
                failures = namedMethod.Failures,
                totalBypasses,
                cacheCapacities = capacities,
                scenarios = new[] { namedMethod, methodSignature, memberPath, flaggedMembers, dynamicProperty, structMap },
                maxBypasses = options.MaxBypasses,
                maxElapsedUs = options.MaxElapsedUs,
                structuralPassed,
                budgetPassed,
                passed
            };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (options.OutputPath is not null)
            {
                var outputPath = Path.GetFullPath(options.OutputPath);
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(outputPath, json, Encoding.UTF8);
            }
            Console.WriteLine(json);
            if (!passed)
                throw new InvalidOperationException("反射容量探针未通过结构性断言或显式预算；详见 JSON 报告。");
        }
        finally
        {
            ReflectionCacheMetrics.Enabled = wasEnabled;
            CoreCacheMetrics.Enabled = coreWasEnabled;
        }
    }

    private static CapacityScenarioReport RunNamedMethodCapacityProbe()
    {
        var capacity = TypeVisit.NamedMethodCacheCapacity;
        var requests = capacity + 1;
        var type = CreateCapacityProbeType(requests);
        var before = ReflectionCacheMetrics.Snapshot();
        var structuralBefore = ReflectionCacheMetrics.StructuralBypassCount;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < requests; index++)
            TypeVisit.FindMethod(type, $"M{index}", Type.EmptyTypes);
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var metrics = CacheDelta(before, ReflectionCacheMetrics.Snapshot());
        var structuralBypasses = ReflectionCacheMetrics.StructuralBypassCount - structuralBefore;
        var total = metrics.Hits + metrics.Misses + metrics.Pending + metrics.Bypasses + metrics.Failures;
        var structuralPassed = metrics.Misses == capacity && metrics.Bypasses == 1 && total == requests;
        return new CapacityScenarioReport("named-method", capacity, requests, elapsedUs,
            metrics.Hits, metrics.Misses, metrics.Pending, metrics.Bypasses, metrics.Failures,
            structuralBypasses, structuralPassed);
    }

    private static CapacityScenarioReport RunMethodSignatureCapacityProbe()
    {
        var capacity = TypeVisit.MethodSignatureCacheCapacity;
        var requests = capacity + 1;
        var type = CreateCapacityProbeType(requests);
        var methods = Enumerable.Range(0, requests)
            .Select(index => type.GetMethod($"M{index}")!)
            .ToArray();
        var before = ReflectionCacheMetrics.Snapshot();
        var structuralBefore = ReflectionCacheMetrics.StructuralBypassCount;
        var started = Stopwatch.GetTimestamp();
        foreach (var method in methods)
            TypeVisit.GetMethodBySignature(type, method);
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var metrics = CacheDelta(before, ReflectionCacheMetrics.Snapshot());
        var structuralBypasses = ReflectionCacheMetrics.StructuralBypassCount - structuralBefore;
        var structuralPassed = metrics.Misses == requests && metrics.Bypasses == 0
            && structuralBypasses == 1;
        return new CapacityScenarioReport("method-signature", capacity, requests, elapsedUs,
            metrics.Hits, metrics.Misses, metrics.Pending, metrics.Bypasses, metrics.Failures,
            structuralBypasses, structuralPassed);
    }

    private static CapacityScenarioReport RunMemberPathCapacityProbe()
    {
        var capacity = Reflections.MemberPathCacheCapacity;
        var requests = capacity + 1;
        var type = CreateCapacityProbeType(0, requests);
        var instance = Activator.CreateInstance(type)!;
        var before = CoreCacheMetrics.Snapshot();
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < requests; index++)
        {
            if (!Reflections.TryGetMemberValueByPath(instance, $"P{index}", out _))
                throw new InvalidOperationException($"成员路径 P{index} 未能读取。");
        }
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var metrics = CacheDelta(before, CoreCacheMetrics.Snapshot());
        var total = metrics.Hits + metrics.Misses + metrics.Pending + metrics.Bypasses + metrics.Failures;
        var structuralPassed = metrics.Misses == capacity && metrics.Bypasses == 1 && total == requests;
        return new CapacityScenarioReport("member-path", capacity, requests, elapsedUs,
            metrics.Hits, metrics.Misses, metrics.Pending, metrics.Bypasses, metrics.Failures,
            0, structuralPassed);
    }

    private static CapacityScenarioReport RunDynamicPropertyCapacityProbe()
    {
        var capacity = DynamicBase.PropertyCacheCapacity;
        var requests = capacity + 1;
        var type = CreateDynamicBaseCapacityProbeType(requests);
        var instance = (DynamicBase)Activator.CreateInstance(type)!;
        var before = ReflectionCacheMetrics.Snapshot();
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < requests; index++)
            _ = instance.GetPropertyValue($"P{index}");
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var metrics = CacheDelta(before, ReflectionCacheMetrics.Snapshot());
        var total = metrics.Hits + metrics.Misses + metrics.Pending + metrics.Bypasses + metrics.Failures;
        var structuralPassed = metrics.Misses == capacity && metrics.Bypasses == 1 && total == requests;
        return new CapacityScenarioReport("dynamic-base-property", capacity, requests, elapsedUs,
            metrics.Hits, metrics.Misses, metrics.Pending, metrics.Bypasses, metrics.Failures,
            0, structuralPassed);
    }

    private static CapacityScenarioReport RunFlaggedMemberCapacityProbe()
    {
        var capacity = TypeReflections.TypeCacheManager.FlaggedCacheCapacity;
        var requests = capacity + 1;
        var type = CreateCapacityProbeType(0, 1);
        var admissionBefore = CoreCacheMetrics.FlaggedCacheAdmissionCount;
        var bypassBefore = CoreCacheMetrics.FlaggedCacheBypassCount;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < requests; index++)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | (BindingFlags)(index << 20);
            _ = TypeReflections.TypeCacheManager.GetTypeProperties(type, flags);
        }
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var admissions = CoreCacheMetrics.FlaggedCacheAdmissionCount - admissionBefore;
        var bypasses = CoreCacheMetrics.FlaggedCacheBypassCount - bypassBefore;
        var structuralPassed = admissions == capacity && bypasses == 1;
        return new CapacityScenarioReport("flagged-member-snapshot", capacity, requests, elapsedUs,
            0, admissions, 0, bypasses, 0, 0, structuralPassed);
    }

    private static CapacityScenarioReport RunStructMapCapacityProbe()
    {
        var capacity = MapperHelper.StructMapCacheCapacity;
        var entriesBefore = MapperHelper.StructMapCacheEntryCount;
        var remaining = Math.Max(0, capacity - entriesBefore);
        var requests = Math.Max(1, remaining + 1);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"StructMapCapacityProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var mapDefinition = typeof(MapperHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(MapperHelper.Map)
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 2);
        var compileBefore = MapperHelper.StructMapCompileCount;
        var bypassBefore = MapperHelper.StructMapCacheCapacityBypassCount;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < requests; index++)
        {
            var sourceType = CreateStructMapCapacityProbeType(module, $"Source{index}");
            var destinationType = CreateStructMapCapacityProbeType(module, $"Destination{index}");
            var source = Activator.CreateInstance(sourceType)!;
            var map = mapDefinition.MakeGenericMethod(sourceType, destinationType);
            _ = map.Invoke(null, new[] { source });
        }
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var compileCount = MapperHelper.StructMapCompileCount - compileBefore;
        var bypasses = MapperHelper.StructMapCacheCapacityBypassCount - bypassBefore;
        var admitted = MapperHelper.StructMapCacheEntryCount - entriesBefore;
        var expectedAdmitted = Math.Min(requests, remaining);
        var structuralPassed = admitted == expectedAdmitted
            && bypasses == (requests > remaining ? 1 : 0)
            && compileCount == requests;
        return new CapacityScenarioReport("struct-map", capacity, requests, elapsedUs,
            0, admitted, 0, bypasses, 0, bypasses, structuralPassed, compileCount);
    }

    private static Type CreateStructMapCapacityProbeType(ModuleBuilder module, string name)
    {
        var builder = module.DefineType(
            name,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
            typeof(ValueType));
        var field = builder.DefineField("_value", typeof(int), FieldAttributes.Private);
        var getter = builder.DefineMethod(
            "get_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(int), Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);

        var setter = builder.DefineMethod(
            "set_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(void), new[] { typeof(int) });
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);

        var property = builder.DefineProperty("Value", PropertyAttributes.None, typeof(int), Type.EmptyTypes);
        property.SetGetMethod(getter);
        property.SetSetMethod(setter);
        return builder.CreateTypeInfo()!.AsType();
    }

    /// <summary>
    /// 观察结构体映射委托从冷启动到稳态的编译、耗时和分配变化。
    /// </summary>
    public static void RunStructMapSteadyProbe()
    {
        const int warmRequests = 256;
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"StructMapSteadyProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var sourceType = CreateStructMapCapacityProbeType(module, "Source");
        var destinationType = CreateStructMapCapacityProbeType(module, "Destination");
        var source = Activator.CreateInstance(sourceType)!;
        sourceType.GetProperty("Value")!.SetValue(source, 17);
        var destinationProperty = destinationType.GetProperty("Value")!;
        var mapDefinition = typeof(MapperHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(MapperHelper.Map)
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 2);
        var map = mapDefinition.MakeGenericMethod(sourceType, destinationType);
        var invoke = CreateObjectMapInvoker(map, sourceType);

        var compileBefore = MapperHelper.StructMapCompileCount;
        var coldAllocatedBefore = GC.GetTotalAllocatedBytes(true);
        var coldStarted = Stopwatch.GetTimestamp();
        var firstResult = invoke(source);
        var coldElapsedUs = (Stopwatch.GetTimestamp() - coldStarted) * 1_000_000.0 / Stopwatch.Frequency;
        var coldAllocatedBytes = GC.GetTotalAllocatedBytes(true) - coldAllocatedBefore;
        var coldCompiles = MapperHelper.StructMapCompileCount - compileBefore;

        var warmCompileBefore = MapperHelper.StructMapCompileCount;
        var warmAllocatedBefore = GC.GetTotalAllocatedBytes(true);
        var warmStarted = Stopwatch.GetTimestamp();
        object? lastResult = firstResult;
        for (var index = 0; index < warmRequests; index++)
            lastResult = invoke(source);
        var warmElapsedUs = (Stopwatch.GetTimestamp() - warmStarted) * 1_000_000.0 / Stopwatch.Frequency;
        var warmAllocatedBytes = GC.GetTotalAllocatedBytes(true) - warmAllocatedBefore;
        var warmCompiles = MapperHelper.StructMapCompileCount - warmCompileBefore;
        var structuralPassed = coldCompiles == 1
            && warmCompiles == 0
            && lastResult is not null
            && firstResult.GetType() == destinationType
            && Equals(destinationProperty.GetValue(lastResult), 17);
        var report = new
        {
            schemaVersion = 1,
            probe = "reflection-struct-map-steady",
            warmRequests,
            cold = new
            {
                elapsedUs = coldElapsedUs,
                allocatedBytes = coldAllocatedBytes,
                compiles = coldCompiles
            },
            warm = new
            {
                elapsedUs = warmElapsedUs,
                allocatedBytes = warmAllocatedBytes,
                compiles = warmCompiles
            },
            structuralPassed,
            passed = structuralPassed
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        if (!structuralPassed)
            throw new InvalidOperationException("结构体映射稳态探针未通过冷编译或稳态复用断言。\n" + json);
    }

    private static Func<object, object> CreateObjectMapInvoker(MethodInfo map, Type sourceType)
    {
        var source = System.Linq.Expressions.Expression.Parameter(typeof(object), "source");
        var call = System.Linq.Expressions.Expression.Call(
            map,
            System.Linq.Expressions.Expression.Convert(source, sourceType));
        var result = System.Linq.Expressions.Expression.Convert(call, typeof(object));
        return System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, source).Compile();
    }

    private sealed record CapacityProbeOptions(long? MaxBypasses, double? MaxElapsedUs, string? OutputPath)
    {
        public static CapacityProbeOptions Parse(IReadOnlyList<string> args)
        {
            string? GetValue(string name)
            {
                var prefix = name + "=";
                var values = args.Where(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(arg => arg[prefix.Length..]).ToArray();
                if (values.Length > 1)
                    throw new ArgumentException($"容量探针参数 {name} 不能重复。", nameof(args));
                return values.SingleOrDefault();
            }

            var maxBypassesText = GetValue("--reflection-capacity-max-bypass");
            long? maxBypasses = null;
            if (maxBypassesText is not null)
            {
                if (!long.TryParse(maxBypassesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    || value < 0)
                    throw new ArgumentException("--reflection-capacity-max-bypass 必须是非负整数。", nameof(args));
                maxBypasses = value;
            }

            var maxElapsedText = GetValue("--reflection-capacity-max-time-us");
            double? maxElapsedUs = null;
            if (maxElapsedText is not null)
            {
                if (!double.TryParse(maxElapsedText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    || double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                    throw new ArgumentException("--reflection-capacity-max-time-us 必须是有限的非负数。", nameof(args));
                maxElapsedUs = value;
            }

            var outputPath = GetValue("--reflection-capacity-output");
            if (outputPath is not null && string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("--reflection-capacity-output 不能为空。", nameof(args));
            return new CapacityProbeOptions(maxBypasses, maxElapsedUs, outputPath);
        }
    }

    private static Type CreateCapacityProbeType(int methodCount, int propertyCount = 0)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"ReflectionCapacityProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var builder = module.DefineType(
            "CapacityTarget", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        for (var index = 0; index < methodCount; index++)
        {
            var method = builder.DefineMethod($"M{index}", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
        }

        for (var index = 0; index < propertyCount; index++)
        {
            var name = $"P{index}";
            var property = builder.DefineProperty(name, PropertyAttributes.None, typeof(int), Type.EmptyTypes);
            var getter = builder.DefineMethod($"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            getter.GetILGenerator().Emit(OpCodes.Ldc_I4_0);
            getter.GetILGenerator().Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
        }

        return builder.CreateTypeInfo()!.AsType();
    }

    private static Type CreateDynamicBaseCapacityProbeType(int propertyCount)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"DynamicBaseCapacityProbe_{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Main");
        var builder = module.DefineType(
            "DynamicBaseCapacityTarget", TypeAttributes.Public | TypeAttributes.Class, typeof(DynamicBase));
        var baseConstructor = typeof(DynamicBase).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;
        var constructor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var constructorIl = constructor.GetILGenerator();
        constructorIl.Emit(OpCodes.Ldarg_0);
        constructorIl.Emit(OpCodes.Call, baseConstructor);
        constructorIl.Emit(OpCodes.Ret);

        for (var index = 0; index < propertyCount; index++)
        {
            var name = $"P{index}";
            var property = builder.DefineProperty(name, PropertyAttributes.None, typeof(int), Type.EmptyTypes);
            var getter = builder.DefineMethod($"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            getter.GetILGenerator().Emit(OpCodes.Ldc_I4_0);
            getter.GetILGenerator().Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
        }

        return builder.CreateTypeInfo()!.AsType();
    }

    private sealed record CapacityScenarioReport(
        string Name,
        int Capacity,
        int Requests,
        double ElapsedUs,
        long Hits,
        long Admitted,
        long Pending,
        long Bypasses,
        long Failures,
        long StructuralBypasses,
        bool StructuralPassed,
        long? Compiles = null);

    /// <summary>观察并发首次访问时缓存的构建和复用次数。</summary>
    public static void RunConcurrentCacheProbe()
    {
        var coreWasEnabled = CoreCacheMetrics.Enabled;
        var reflectionWasEnabled = ReflectionCacheMetrics.Enabled;
        CoreCacheMetrics.Enabled = true;
        ReflectionCacheMetrics.Enabled = true;
        try
        {
        const int calls = 32;
        var sample = new CacheProbeSample(1);
        var property = typeof(CacheProbeSample).GetProperty(nameof(CacheProbeSample.Value))!;
        var method = typeof(CacheProbeSample).GetMethod(nameof(CacheProbeSample.Execute))!;

        var constructorCallIndex = 0;
        MeasureConcurrent("constructor", calls,
            () =>
            {
                if ((System.Threading.Interlocked.Increment(ref constructorCallIndex) & 1) == 0)
                    TypeVisit.CreateInstance(typeof(CacheProbeSample), 1);
                else
                    Reflections.CreateInstance<object>(typeof(CacheProbeSample), 1);
            },
            () => TypeVisit.ConstructorScanCount,
            CoreCacheMetrics.Snapshot,
            invokerBuildCount: () => TypeVisit.ConstructorInvokerBuildCount);
        MeasureConcurrent("getter compile", calls,
            () => property.GetValueGetter<CacheProbeSample>(),
            () => Bing.Reflection.PropertyInfoExtensions.AccessorCompileCount,
            CoreCacheMetrics.Snapshot);
        MeasureConcurrent("setter compile", calls,
            () => property.GetValueSetter<CacheProbeSample>(),
            () => Bing.Reflection.PropertyInfoExtensions.AccessorCompileCount,
            CoreCacheMetrics.Snapshot);
        MeasureConcurrent("method signature", calls,
            () => TypeVisit.GetMethodBySignature(typeof(CacheProbeSample), method),
            () => TypeVisit.MethodSignatureScanCount,
            ReflectionCacheMetrics.Snapshot);
        MeasureConcurrent("member path", calls,
            () => Reflections.TryGetMemberValueByPath(sample, nameof(CacheProbeSample.Value), out _),
            () => Reflections.MemberPathResolveCount,
            CoreCacheMetrics.Snapshot);
        var failedBefore = CoreCacheMetrics.Snapshot();
        for (var index = 0; index < 2; index++)
        {
            try
            {
                TypeVisit.CreateInstance(typeof(MissingCacheProbeSample), 1);
                throw new InvalidOperationException("缺失构造函数应抛出参数异常。");
            }
            catch (ArgumentException)
            {
            }
        }
        var failed = CacheDelta(failedBefore, CoreCacheMetrics.Snapshot());
        if (failed.Misses != 1 || failed.Failures != 1)
            throw new InvalidOperationException("失败的构造函数缓存应记录一次未命中和一次失败复用。");
        PrintCacheMetrics("constructor failure reuse", 2, failed);
        var scansBefore = TypeVisit.ConstructorScanCount;
        var concurrentFailureBefore = CoreCacheMetrics.Snapshot();
        var exceptions = new ArgumentException[calls];
        Parallel.For(0, calls, index =>
        {
            try
            {
                TypeVisit.CreateInstance(typeof(ConcurrentMissingCacheProbeSample), 1);
                throw new InvalidOperationException("缺失构造函数应抛出参数异常。");
            }
            catch (ArgumentException exception)
            {
                exceptions[index] = exception;
            }
        });
        if (TypeVisit.ConstructorScanCount - scansBefore != 1
            || exceptions.Any(exception => !ReferenceEquals(exception, exceptions[0])))
            throw new InvalidOperationException("并发失败请求应只扫描一次，并复用同一原始异常。");
        PrintCacheMetrics("constructor concurrent failure", calls,
            CacheDelta(concurrentFailureBefore, CoreCacheMetrics.Snapshot()));
        }
        finally
        {
            CoreCacheMetrics.Enabled = coreWasEnabled;
            ReflectionCacheMetrics.Enabled = reflectionWasEnabled;
        }
    }

    private static void MeasureConcurrent(string name, int calls, Action action, Func<int> buildCount,
        Func<(long Hits, long Misses, long Pending, long Bypasses, long Failures)> cacheSnapshot,
        Func<int>? invokerBuildCount = null)
    {
        var before = buildCount();
        var invokersBefore = invokerBuildCount?.Invoke() ?? 0;
        var cacheBefore = cacheSnapshot();
        var allocated = GC.GetTotalAllocatedBytes(true);
        var started = Stopwatch.GetTimestamp();
        Parallel.For(0, calls, _ => action());
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocated;
        var builds = buildCount() - before;
        var invokers = invokerBuildCount?.Invoke() - invokersBefore;
        var cold = CacheDelta(cacheBefore, cacheSnapshot());
        var warmBefore = cacheSnapshot();
        var warmBuildsBefore = buildCount();
        var warmInvokersBefore = invokerBuildCount?.Invoke();
        Parallel.For(0, calls, _ => action());
        var warm = CacheDelta(warmBefore, cacheSnapshot());
        if (buildCount() != warmBuildsBefore || (invokerBuildCount != null && invokerBuildCount() != warmInvokersBefore))
            throw new InvalidOperationException($"{name} 重复访问时不应再次扫描或构建委托。");
        Console.WriteLine($"{name} ({calls} calls): {elapsedUs:F1} us, " +
                          $"{allocatedBytes} B, " +
                          $"scans/builds: {builds}" +
                          (invokers.HasValue ? $", constructor invoker builds: {invokers}" : string.Empty));
        PrintCacheMetrics($"{name} cold", calls, cold);
        PrintCacheMetrics($"{name} warm", calls, warm);
    }

    private static (long Hits, long Misses, long Pending, long Bypasses, long Failures) CacheDelta(
        (long Hits, long Misses, long Pending, long Bypasses, long Failures) before,
        (long Hits, long Misses, long Pending, long Bypasses, long Failures) after) =>
        (after.Hits - before.Hits, after.Misses - before.Misses,
            after.Pending - before.Pending, after.Bypasses - before.Bypasses,
            after.Failures - before.Failures);

    private static void PrintCacheMetrics(string name, int calls,
        (long Hits, long Misses, long Pending, long Bypasses, long Failures) metrics)
    {
        if (metrics.Hits + metrics.Misses + metrics.Pending + metrics.Bypasses + metrics.Failures != calls)
            throw new InvalidOperationException($"{name} 的缓存请求计数与调用次数不一致。");
        Console.WriteLine($"{name}: hit {metrics.Hits}/{calls} ({metrics.Hits * 100.0 / calls:F1}%), " +
                          $"miss {metrics.Misses}, pending {metrics.Pending}, bypass {metrics.Bypasses}, failed {metrics.Failures}");
    }

    private static void Measure(string name, Action action)
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        action();
        var elapsedUs = (Stopwatch.GetTimestamp() - started) * 1_000_000.0 / Stopwatch.Frequency;
        Console.WriteLine($"{name}: {elapsedUs:F1} us, {GC.GetAllocatedBytesForCurrentThread() - allocated} B");
    }

    private sealed class ReflectionSample
    {
        public ReflectionSample() { }
        public ReflectionSample(int value) => Value = value;
        public int Value { get; set; }
        public void Execute() { }
    }

    private sealed class ColdConstructor { public ColdConstructor(int value) { } }
    private sealed class ColdProperty { public int Value { get; set; } }
    private sealed class ColdWriteProperty { public int Value { get; set; } }
    private sealed class ColdMethod { public void Execute() { } }
    private sealed class CacheProbeSample
    {
        public CacheProbeSample(int value) => Value = value;
        public int Value { get; set; }
        public void Execute() { }
    }

    private sealed class MissingCacheProbeSample
    {
        public MissingCacheProbeSample(string value) { }
    }

    private sealed class ConcurrentMissingCacheProbeSample
    {
        public ConcurrentMissingCacheProbeSample(string value) { }
    }
}
