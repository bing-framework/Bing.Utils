using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = new UTF8Encoding(false);
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var type = assembly.GetType("Bing.IO.TemporaryFileStore", true)!;
var create = type.GetMethod("CreateFile", new[] { typeof(long), typeof(bool) })!;
var batch = type.GetMethod("CreateFiles");
var mode = args.Length > 1 ? args[1] : "individual";
var values = new List<object>();
var counts = args.Length > 2 ? new[] { 10, int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) } : new[] { 10, 100, 1000 };
foreach (var count in counts)
{
    var root = Path.Combine(Path.GetTempPath(), "bing-stream-probe-" + Guid.NewGuid().ToString("N"));
    try
    {
        var store = Activator.CreateInstance(type, root, (long)count * 16)!;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var allocation = GC.GetTotalAllocatedBytes(true); var clock = Stopwatch.StartNew();
        if (mode == "batch")
        {
            if (batch == null) throw new InvalidOperationException("Batch API missing.");
            var leases = (System.Collections.IEnumerable)batch.Invoke(store, new object[] { Enumerable.Repeat(16L, count).ToArray(), false, CancellationToken.None })!;
            foreach (IDisposable lease in leases) lease.Dispose();
        }
        else for (var i = 0; i < count; i++) ((IDisposable)create.Invoke(store, new object[] { 16L, false })!).Dispose();
        clock.Stop(); allocation = GC.GetTotalAllocatedBytes(true) - allocation;
        if (count > 10) values.Add(new { Count = count, Milliseconds = clock.Elapsed.TotalMilliseconds, AllocatedBytes = allocation, Mode = mode });
    }
    finally { Directory.Delete(root, true); }
}
Console.WriteLine(JsonSerializer.Serialize(new { Runtime = Environment.Version.ToString(), Seed = 20261024, Scenarios = values }));
