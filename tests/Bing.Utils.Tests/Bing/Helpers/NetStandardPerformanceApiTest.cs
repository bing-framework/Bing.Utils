using System.IO.Compression;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;

namespace Bing.Helpers;

[Collection("RegexPool")]
[Trait("Category", "PerformanceApiReview")]
public class NetStandardPerformanceApiTest : IDisposable
{
    private readonly AssemblyLoadContext _context;
    private readonly Assembly _assembly;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bing-std-api-" + Guid.NewGuid());
    public NetStandardPerformanceApiTest()
    {
        Directory.CreateDirectory(_root);
        var path = Environment.GetEnvironmentVariable("BING_REVIEW_NETSTANDARD_DLL");
        if (string.IsNullOrEmpty(path)) _assembly = typeof(Compression).Assembly;
        else
        {
            _context = new AssemblyLoadContext("performance-api-" + Guid.NewGuid(), true);
            _assembly = _context.LoadFromAssemblyPath(Path.GetFullPath(path));
            Assert.Equal(".NETStandard,Version=v2.0", _assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
        }
    }

    [Fact]
    public async Task StreamAndSmallPageApis_ExecuteLoadedArtifact()
    {
        using var input = new MemoryStream(new byte[] { 1, 2, 3 });
        using var compressed = new MemoryStream();
        await (Task)Call("Bing.Helpers.Compression", "CompressToAsync", input, compressed, CompressionLevel.Optimal, true, CancellationToken.None);
        compressed.Position = 0;
        using var output = new MemoryStream();
        Call("Bing.Helpers.Compression", "DecompressTo", compressed, output, 3L, true);
        Assert.Equal(new byte[] { 1, 2, 3 }, output.ToArray());
        using var empty = new MemoryStream();
        using var emptyGzip = new MemoryStream();
        Call("Bing.Helpers.Compression", "CompressTo", empty, emptyGzip, CompressionLevel.Optimal, true);
        Assert.True(emptyGzip.Length > 0);
        using var pages = (Stream)Activator.CreateInstance(_assembly.GetType("Bing.IO.LargeMemoryStream"), new object[] { 2 });
        pages.Write(new byte[] { 1, 2, 3 }, 0, 3);
        pages.Position = 0;
        Assert.Equal(1, pages.ReadByte());
        pages.SetLength(0);
        Assert.Equal(0, pages.Length);
    }

    [Fact]
    public async Task CancellationAndRegex_ExecuteLoadedArtifact()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var fileMethod = _assembly.GetType("Bing.IO.FileHelper").GetMethod("ReadToMemoryStreamAsync");
        var read = (Task<MemoryStream>)fileMethod.Invoke(null, new object[] { Path.Combine(_root, "missing"), cts.Token });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        var forEach = _assembly.GetType("Bing.Collections.BingEnumerableExtensions").GetMethods()
            .Single(m => m.Name == "ForEachAsync").MakeGenericMethod(typeof(int));
        var invoked = 0;
        var task = (Task)forEach.Invoke(null, new object[] { new[] { 1 }, new Func<int, Task>(_ => { invoked++; return Task.CompletedTask; }), null, null, cts.Token });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(0, invoked);
        var pool = _assembly.GetType("Bing.Text.RegularExpressions.RegexPool");
        pool.GetMethod("Clear").Invoke(null, null);
        var get = pool.GetMethod("GetOrCreate", new[] { typeof(string), typeof(RegexOptions) });
        var first = get.Invoke(null, new object[] { "^a$", RegexOptions.None });
        Assert.Same(first, get.Invoke(null, new object[] { "^a$", RegexOptions.None }));
        Assert.Equal(1L, pool.GetProperty("MissCount").GetValue(null));
        pool.GetMethod("Clear").Invoke(null, null);
    }

    [Fact]
    public void ZipLimitOverwrite_ExecutesLoadedArtifact()
    {
        var zip = Path.Combine(_root, "input.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        using (var output = archive.CreateEntry("file.bin").Open()) output.Write(new byte[] { 1, 2 }, 0, 2);
        var target = Path.Combine(_root, "target"); Directory.CreateDirectory(target);
        var file = Path.Combine(target, "file.bin"); File.WriteAllBytes(file, new byte[] { 9 });
        Assert.Throws<InvalidDataException>(() => Call("Bing.Helpers.Compression", "UnZipWithLimit", zip, target, 1L, true));
        Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(file));
        Call("Bing.Helpers.Compression", "UnZipWithLimit", zip, target, 2L, true);
        Assert.Equal(new byte[] { 1, 2 }, File.ReadAllBytes(file));
        Assert.Empty(Directory.GetFiles(target, ".bing-unzip-*.tmp"));
    }

    private object Call(string type, string method, params object[] args)
    {
        try { return _assembly.GetType(type).GetMethod(method).Invoke(null, args); }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    /// <summary>
    /// 通过实际加载的产物执行本轮有界传输、结构恢复及异步序列能力。
    /// </summary>
    [Fact]
    public async Task CapabilityExpansion_ExecutesLoadedArtifact()
    {
        var optionsType = _assembly.GetType("Bing.IO.StreamTransferOptions");
        var options = Activator.CreateInstance(optionsType);
        optionsType.GetProperty("MaxBytes").SetValue(options, 3L);
        optionsType.GetProperty("ComputeSha256").SetValue(options, true);
        using var input = new MemoryStream(new byte[] { 1, 2, 3 });
        using var output = new MemoryStream();
        var copy = (Task)Call("Bing.IO.StreamTransfer", "CopyAsync", input, output, options, true, CancellationToken.None);
        await copy;
        Assert.Equal(new byte[] { 1, 2, 3 }, output.ToArray());
        using var table = new System.Data.DataTable("data"); table.Columns.Add("Id", typeof(long)); table.Rows.Add(long.MaxValue);
        var json = (string)Call("Bing.Utils.Json.TabularJson", "SerializeTable", table, null);
        using var restored = (System.Data.DataTable)Call("Bing.Utils.Json.TabularJson", "DeserializeTable", json, null);
        Assert.Equal(long.MaxValue, restored.Rows[0][0]);
        var count = 0;
        var method = _assembly.GetType("Bing.Collections.BingAsyncEnumerableExtensions").GetMethod("ForEachAwaitAsync").MakeGenericMethod(typeof(int));
        await (Task)method.Invoke(null, new object[] { Values(), new Func<int, CancellationToken, Task>((_, _) => { Interlocked.Increment(ref count); return Task.CompletedTask; }), 2, CancellationToken.None });
        Assert.Equal(2, count);
        using var jsonOutput = new MemoryStream();
        method = _assembly.GetType("Bing.Serialization.SystemTextJson.JsonStream").GetMethod("WriteArrayAsync").MakeGenericMethod(typeof(int));
        await (Task)method.Invoke(null, new object[] { jsonOutput, Values(), null, 1, true, CancellationToken.None });
        Assert.Equal("[1,2]", System.Text.Encoding.UTF8.GetString(jsonOutput.ToArray()));

        static async IAsyncEnumerable<int> Values() { yield return 1; await Task.Yield(); yield return 2; }
    }

    public void Dispose() { _context?.Unload(); Directory.Delete(_root, true); }

    /// <summary>
    /// 在实际 netstandard 产物上验证默认小页和 v2 编辑状态往返。
    /// </summary>
    [Fact]
    public async Task Refinement_ExecutesLoadedArtifact()
    {
        using var pages = (Stream)Activator.CreateInstance(_assembly.GetType("Bing.IO.LargeMemoryStream"));
        pages.WriteByte(7);
        var view = (ArraySegment<byte[]>)pages.GetType().GetMethod("ToArraySegment").Invoke(pages, null);
        Assert.InRange(view.Array[0].Length, 65536, 131072);
        var type = _assembly.GetType("Bing.Utils.Json.TabularJsonOptions");
        var options = Activator.CreateInstance(type);
        type.GetProperty("Format").SetValue(options, System.Enum.ToObject(_assembly.GetType("Bing.Utils.Json.TabularJsonFormat"), 2));
        using var table = new System.Data.DataTable("edits"); table.Columns.Add("Name", typeof(string));
        var row = table.Rows.Add("old"); table.AcceptChanges(); row[0] = "current"; row.BeginEdit(); row[0] = "proposed";
        using var document = new MemoryStream();
        await (Task)Call("Bing.Utils.Json.TabularJson", "WriteTableAsync", document, table, options, true, CancellationToken.None);
        document.Position = 0;
        var operation = (Task)Call("Bing.Utils.Json.TabularJson", "ReadTableAsync", document, options, true, CancellationToken.None);
        await operation;
        using var restored = (System.Data.DataTable)operation.GetType().GetProperty("Result").GetValue(operation);
        Assert.Equal("old", restored.Rows[0][0, System.Data.DataRowVersion.Original]);
        Assert.Equal("current", restored.Rows[0][0, System.Data.DataRowVersion.Current]);
        Assert.Equal("proposed", restored.Rows[0][0, System.Data.DataRowVersion.Proposed]);
    }
}
