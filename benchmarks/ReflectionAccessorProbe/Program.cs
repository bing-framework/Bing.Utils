using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

// 同一个探针二进制加载不同版本的库；计时区间只调用已绑定的委托。
if (args.Length != 2)
    throw new ArgumentException("参数：Bing.Utils.dll 路径、Bing.Utils.Reflection.dll 路径。");
var core = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var reflection = Assembly.LoadFrom(Path.GetFullPath(args[1]));
var extensions = core.GetType("Bing.Reflection.PropertyInfoExtensions", true)!;
var typeReflections = core.GetType("Bing.Reflection.TypeReflections", true)!;
var typeVisit = reflection.GetType("Bing.Reflection.TypeVisit", true)!;
var sample = new ProbeSample();
var property = typeof(ProbeSample).GetProperty(nameof(ProbeSample.Number))!;
var objectGetterFactory = extensions.GetMethods().Single(method => method.Name == "GetValueGetter"
    && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1);
var objectGetter = (Func<ProbeSample, object>)objectGetterFactory.MakeGenericMethod(typeof(ProbeSample)).Invoke(null, new object[] { property })!;
var readable = (Func<Type, IReadOnlyList<PropertyInfo>>)typeReflections.GetMethod("GetPublicReadableInstanceProperties")!
    .CreateDelegate(typeof(Func<Type, IReadOnlyList<PropertyInfo>>));
var writable = (Func<Type, IReadOnlyList<PropertyInfo>>)typeReflections.GetMethod("GetPublicWritableInstanceProperties")!
    .CreateDelegate(typeof(Func<Type, IReadOnlyList<PropertyInfo>>));
var create = typeVisit.GetMethod("CreateInstance", new[] { typeof(IDictionary<string, object>) })!;
var instance = create.Invoke(null, new object[] { new Dictionary<string, object> { ["Value"] = 7 } })!;
var getterMethod = instance.GetType().GetMethod("GetPropertyValue")!;
var get = (Func<string, object>)getterMethod.CreateDelegate(typeof(Func<string, object>), instance);
var set = (Action<string, object>)instance.GetType().GetMethod("SetPropertyValue")!.CreateDelegate(typeof(Action<string, object>), instance);
var newInstance = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(instance.GetType()), typeof(object))).Compile();
var objectParameter = Expression.Parameter(typeof(object));
var readNew = Expression.Lambda<Func<object, object>>(Expression.Call(
    Expression.Convert(objectParameter, getterMethod.DeclaringType!), getterMethod, Expression.Constant("Value")), objectParameter).Compile();
var measurements = new List<object>();
measurements.Add(Measure("object-getter", () => objectGetter(sample)));
measurements.Add(Measure("readable-properties", () => readable(typeof(ProbeSample))));
measurements.Add(Measure("writable-properties", () => writable(typeof(ProbeSample))));
measurements.Add(Measure("dynamic-getter", () => get("Value")));
object boxedValue = 7;
measurements.Add(Measure("dynamic-setter", () => { set("Value", boxedValue); return 0; }));
measurements.Add(Measure("dynamic-create-and-read", () => readNew(newInstance())));
var typedFactory = extensions.GetMethods().SingleOrDefault(method => method.Name == "GetValueGetter"
    && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2);
if (typedFactory != null)
{
    var typed = (Func<ProbeSample, int>)typedFactory.MakeGenericMethod(typeof(ProbeSample), typeof(int)).Invoke(null, new object[] { property })!;
    measurements.Add(Measure("typed-getter-new-api", () => typed(sample)));
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    runtime = RuntimeInformation.FrameworkDescription,
    core = new { core.Location, mvid = core.ManifestModule.ModuleVersionId },
    reflection = new { reflection.Location, mvid = reflection.ManifestModule.ModuleVersionId },
    warmup = 10000, iterations = 50000, repetitions = 5,
    requestedConcurrency = 1, maxActualParallelism = 1, maxQueuedRequests = 0,
    measurements
}, new JsonSerializerOptions { WriteIndented = true }));

static object Measure<T>(string name, Func<T> action)
{
    for (var index = 0; index < 10000; index++)
        action();
    const int count = 50000;
    var times = new double[5];
    var allocations = new double[5];
    T? last = default;
    for (var repetition = 0; repetition < times.Length; repetition++)
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < count; index++)
            last = action();
        times[repetition] = (Stopwatch.GetTimestamp() - started) * 1e9 / Stopwatch.Frequency / count;
        allocations[repetition] = (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)count;
    }
    GC.KeepAlive(last);
    Array.Sort(times);
    Array.Sort(allocations);
    return new { name, medianNanoseconds = times[2], bytesPerOperation = allocations[2] };
}

public sealed class ProbeSample
{
    public int Number { get; set; } = 7;
    public string? Text { get; set; }
    public long Total { get; set; }
    public int ReadOnly => 1;
    public int PrivateSet { get; private set; }
    public int InitOnly { get; init; }
}
