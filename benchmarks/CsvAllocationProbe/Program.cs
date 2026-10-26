using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = new UTF8Encoding(false);
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var csv = assembly.GetType("Bing.Serialization.Csv.CsvStream", true)!;
var optionsType = assembly.GetType("Bing.Serialization.Csv.CsvOptions", true)!;
var read = csv.GetMethod("ReadAsync", BindingFlags.Public | BindingFlags.Static)!;
var options = Activator.CreateInstance(optionsType)!;
optionsType.GetProperty("HasHeader")!.SetValue(options, false);
var results = new List<object>();
foreach (var (name, input) in new[]
{
    ("single-1KiB", new string('x', 1024) + "\n"),
    ("single-1MiB", new string('x', 1024 * 1024) + "\n"),
    ("1024-rows", string.Concat(Enumerable.Repeat(new string('x', 1024) + "\n", 1024)))
})
{
    var bytes = Encoding.UTF8.GetBytes(input);
    await Consume(bytes); // 排除该负载首次 JIT；正式记录一次，不设耗时门禁。
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    var allocated = GC.GetTotalAllocatedBytes(true);
    var clock = Stopwatch.StartNew();
    var rows = await Consume(bytes);
    clock.Stop(); allocated = GC.GetTotalAllocatedBytes(true) - allocated;
    results.Add(new { Name = name, InputBytes = bytes.Length, Rows = rows, Milliseconds = clock.Elapsed.TotalMilliseconds, AllocatedBytes = allocated });
}
var batching = assembly.GetType("Bing.Collections.AsyncSequenceBufferingExtensions", true)!.GetMethod("BatchAsync")!.MakeGenericMethod(typeof(int));
var batches = (IAsyncEnumerable<IReadOnlyList<int>>)batching.Invoke(null,
    new object[] { SynchronousRows(), 10, TimeSpan.FromMilliseconds(1), CancellationToken.None })!;
await using var iterator = batches.GetAsyncEnumerator();
await iterator.MoveNextAsync();
Console.WriteLine(JsonSerializer.Serialize(new { Runtime = Environment.Version.ToString(), Seed = 20261025,
    BatchFirstCount = iterator.Current.Count, Scenarios = results }));

async Task<int> Consume(byte[] bytes)
{
    using var source = new MemoryStream(bytes, writable: false);
    var sequence = (IAsyncEnumerable<object>)read.Invoke(null, new object[] { source, options, true, CancellationToken.None })!;
    var count = 0;
    await foreach (var row in sequence) count++;
    return count;
}

static async IAsyncEnumerable<int> SynchronousRows()
{
    yield return 1;
    Thread.Sleep(50);
    yield return 2;
    yield return 3;
    await Task.CompletedTask;
}
