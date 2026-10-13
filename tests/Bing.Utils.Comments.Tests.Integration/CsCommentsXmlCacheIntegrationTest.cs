using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Bing.Comments;

namespace Bing.Utils.Comments.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CommentsXmlCacheCollection : ICollectionFixture<CommentsXmlCacheFixture>
{
    public const string Name = "Comments XML cache";
}

/// <summary>
/// 测试 XML 注释缓存的文件、编码、安全和并发行为。
/// </summary>
[Collection(CommentsXmlCacheCollection.Name)]
public sealed class CsCommentsXmlCacheIntegrationTest
{
    /// <summary>
    /// 测试目的：验证声明编码加载和防御性 XML 文档副本。
    /// </summary>
    [Fact]
    public void Set_DeclaredEncodingAndDefensiveDocument_ReturnsStableComments()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("编码内容", "utf-16"), Encoding.Unicode);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);

            var document = CsCommentsXmlCache.Get(typeof(CacheFixture));
            document.DocumentElement.RemoveAll();
            var comments = CsCommentReader.Create(typeof(CacheFixture));

            Assert.Equal("编码内容", comments.Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证有效 Reset 会替换缓存快照。
    /// </summary>
    [Fact]
    public void Reset_ValidDocument_ReplacesTheCachedSnapshot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("第一次"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            Assert.Equal("第一次", CsCommentReader.Create(typeof(CacheFixture)).Summary);

            File.WriteAllText(path, CreateXml("第二次"), Encoding.UTF8);
            CsCommentsXmlCache.Reset(path);

            Assert.Equal("第二次", CsCommentReader.Create(typeof(CacheFixture)).Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证文件元数据变化时普通读取会自动加载新快照。
    /// </summary>
    [Fact]
    public void Get_FileChangedSinceSet_RefreshesWithoutExplicitReset()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("第一次"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            Assert.Equal("第一次", CsCommentReader.Create(typeof(CacheFixture)).Summary);

            File.WriteAllText(path, CreateXml("文件变化后的内容"), Encoding.UTF8);

            Assert.Equal("文件变化后的内容", CsCommentReader.Create(typeof(CacheFixture)).Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证自动刷新读取文件失败时保留最近一次完整快照。
    /// </summary>
    [Fact]
    public void Get_FileDeletedSinceSet_PreservesLastSnapshot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("保留内容"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            File.Delete(path);

            Assert.Equal("保留内容", CsCommentReader.Create(typeof(CacheFixture)).Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证损坏 XML 刷新失败时保留旧快照。
    /// </summary>
    [Fact]
    public void Reset_InvalidDocument_PreservesThePreviousSnapshot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("有效内容"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            File.WriteAllText(path, "<doc>", Encoding.UTF8);

            Assert.Throws<XmlException>(() => CsCommentsXmlCache.Reset(path));
            Assert.Equal("有效内容", CsCommentReader.Create(typeof(CacheFixture)).Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证同一路径绑定到多个程序集实例时全部刷新。
    /// </summary>
    [Fact]
    public void Reset_SharedPath_RefreshesAllAssemblySnapshots()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("第一次"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            CsCommentsXmlCache.Set(typeof(CsCommentReader).Assembly, path);

            File.WriteAllText(path, CreateXml("第二次"), Encoding.UTF8);
            CsCommentsXmlCache.Reset(path);

            Assert.Equal("第二次", ReadSummary(CsCommentsXmlCache.Get(typeof(CacheFixture))));
            Assert.Equal("第二次", ReadSummary(CsCommentsXmlCache.Get(typeof(CsCommentReader))));
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
            CsCommentsXmlCache.Set(typeof(CsCommentReader).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证 DTD 和外部实体被拒绝。
    /// </summary>
    [Fact]
    public void Set_DtdDocument_RejectsExternalEntity()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        var xml = "<?xml version=\"1.0\"?><!DOCTYPE doc [<!ENTITY external SYSTEM \"file:///secret\">]><doc><members><member name=\"T:Bing.Utils.Comments.Tests.Integration.CacheFixture\"><summary>&external;</summary></member></members></doc>";

        try
        {
            File.WriteAllText(path, xml, Encoding.UTF8);
            Assert.Throws<XmlException>(() => CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path));
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证超过字符上限的 XML 文档会被拒绝，避免无限制占用内存。
    /// </summary>
    [Fact]
    public void Set_OverSizedDocument_ThrowsXmlException()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("oversized.xml");
        using (var writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            writer.Write("<doc><members><member name=\"T:Oversized\"><summary>");
            writer.Write(new string('x', checked((int)CsCommentsXmlCache.MaximumDocumentCharacters)));
            writer.Write("</summary></member></members></doc>");
        }

        try
        {
            Assert.Throws<XmlException>(() => CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path));
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证空成员参数抛出参数异常。
    /// </summary>
    [Fact]
    public void Get_NullMember_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CsCommentsXmlCache.Get(null));
    }

    /// <summary>
    /// 测试目的：验证缺失 XML 路径返回 null。
    /// </summary>
    [Fact]
    public void GetXmlPath_MissingXml_ReturnsNull()
    {
        using var directory = new TemporaryDirectory();

        Assert.Null(CsCommentsXmlCache.GetXmlPath(directory.Combine("missing.dll")));
    }

    /// <summary>
    /// 测试目的：验证 XML 路径无扩展名时返回 null。
    /// </summary>
    [Fact]
    public void GetXmlPath_WithoutExtension_ReturnsNull()
    {
        using var directory = new TemporaryDirectory();

        Assert.Null(CsCommentsXmlCache.GetXmlPath(directory.Combine("assembly")));
    }

    /// <summary>
    /// 测试目的：验证 BaseDirectory/bin 回退路径。
    /// </summary>
    [Fact]
    public void GetXmlPath_FallsBackToBaseDirectoryBin()
    {
        using var directory = new TemporaryDirectory();
        var fileName = Guid.NewGuid().ToString("N") + ".xml";
        var binDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
        var path = Path.Combine(binDirectory, fileName);
        Directory.CreateDirectory(binDirectory);

        try
        {
            File.WriteAllText(path, "<doc />", Encoding.UTF8);

            var resolved = CsCommentsXmlCache.GetXmlPath(directory.Combine(Path.ChangeExtension(fileName, ".dll")));

            Assert.Equal(Path.GetFullPath(path), Path.GetFullPath(resolved));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// 测试目的：验证并发读取返回完整缓存内容。
    /// </summary>
    [Fact]
    public async Task Get_ConcurrentReads_ReturnsCompleteSnapshots()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("并发内容"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            var tasks = new Task[32];
            for (var index = 0; index < tasks.Length; index++)
            {
                tasks[index] = Task.Run(() =>
                {
                    var comments = CsCommentReader.Create(typeof(CacheFixture));
                    Assert.Equal("并发内容", comments.Summary);
                });
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证并发有效刷新不会产生半加载快照。
    /// </summary>
    [Fact]
    public async Task Reset_ConcurrentValidRefreshes_LeavesCompleteSnapshot()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("初始内容"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            File.WriteAllText(path, CreateXml("刷新内容"), Encoding.UTF8);

            var tasks = new Task[16];
            for (var index = 0; index < tasks.Length; index++)
                tasks[index] = Task.Run(() => CsCommentsXmlCache.Reset(path));
            await Task.WhenAll(tasks);

            Assert.Equal("刷新内容", CsCommentReader.Create(typeof(CacheFixture)).Summary);
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证读取与刷新并发时只观察完整旧或新快照。
    /// </summary>
    [Fact]
    public async Task Get_DuringReset_ReturnsOnlyCompleteSnapshots()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("comments.xml");
        File.WriteAllText(path, CreateXml("旧快照"), Encoding.UTF8);

        try
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, path);
            File.WriteAllText(path, CreateXml("新快照"), Encoding.UTF8);

            using var barrier = new System.Threading.Barrier(33);
            var readers = new Task<string>[32];
            for (var index = 0; index < readers.Length; index++)
            {
                readers[index] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    return CsCommentReader.Create(typeof(CacheFixture)).Summary;
                });
            }

            var reset = Task.Run(() =>
            {
                barrier.SignalAndWait();
                CsCommentsXmlCache.Reset(path);
            });

            await Task.WhenAll(readers);
            await reset;

            foreach (var reader in readers)
                Assert.Contains(reader.Result, new[] { "旧快照", "新快照" });
        }
        finally
        {
            CsCommentsXmlCache.Set(typeof(CacheFixture).Assembly, null);
        }
    }

    /// <summary>
    /// 测试目的：验证跨程序集显式 cref 能读取生产程序集注释。
    /// </summary>
    [Fact]
    public void Create_CrossAssemblyExplicitCref_UsesProductionComments()
    {
        var method = typeof(CrossAssemblyInheritFixture).GetMethod(nameof(CrossAssemblyInheritFixture.Read));

        var comments = CsCommentReader.Create(method);

        Assert.Equal("读取类型的注释信息。", comments.Summary);
    }

    /// <summary>
    /// 测试目的：验证可回收加载上下文中的程序集不会被缓存强引用。
    /// </summary>
    [Fact]
    public void Set_CollectibleAssembly_DoesNotPreventUnload()
    {
        using var directory = new TemporaryDirectory();
        var assemblyPath = directory.Combine("collectible.dll");
        var xmlPath = directory.Combine("collectible.xml");
        File.Copy(typeof(CacheFixture).Assembly.Location, assemblyPath);
        File.WriteAllText(xmlPath, "<doc />", Encoding.UTF8);

        var weakAssembly = LoadAndCacheCollectibleAssembly(assemblyPath, xmlPath);

        for (var index = 0; index < 3 && weakAssembly.IsAlive; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.False(weakAssembly.IsAlive);
    }

    private static WeakReference LoadAndCacheCollectibleAssembly(string assemblyPath, string xmlPath)
    {
        var loadContext = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), isCollectible: true);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        CsCommentsXmlCache.Set(assembly, xmlPath);
        var weakAssembly = new WeakReference(assembly);
        loadContext.Unload();
        return weakAssembly;
    }

    private static string CreateXml(string summary, string encoding = "utf-8")
    {
        return $"<?xml version=\"1.0\" encoding=\"{encoding}\"?><doc><assembly><name>CacheFixture</name></assembly><members><member name=\"T:{typeof(CacheFixture).FullName}\"><summary>{summary}</summary></member></members></doc>";
    }

    private static string ReadSummary(XmlDocument document)
    {
        return document.SelectSingleNode("/doc/members/member/summary")?.InnerText.Trim();
    }
}

public sealed class CommentsXmlCacheFixture
{
}

public sealed class CacheFixture
{
}

public sealed class CrossAssemblyInheritFixture
{
    /// <inheritdoc cref="Bing.Comments.CsCommentReader.Create(System.Type)" path="/member/summary" />
    public string Read(Type type) => type.FullName;
}

internal sealed class TemporaryDirectory : IDisposable
{
    private readonly string _path;

    public TemporaryDirectory()
    {
        _path = Path.Combine(Path.GetTempPath(), "Bing.Utils.Comments.Tests.Integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_path);
    }

    public string Combine(string fileName) => Path.Combine(_path, fileName);

    public void Dispose()
    {
        if (Directory.Exists(_path))
            Directory.Delete(_path, true);
    }
}