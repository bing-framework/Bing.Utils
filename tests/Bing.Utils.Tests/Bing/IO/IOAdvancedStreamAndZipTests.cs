using System;
using System.IO;
using System.Linq;
using Bing.IO;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.IO;

// ============================================================
//  FileDescriptor
// ============================================================

/// <summary>
/// 测试 <see cref="FileDescriptor"/> 的构造函数与属性
/// </summary>
[Trait("Bing.IO", "FileDescriptor")]
public class FileDescriptorTests
{
    /// <summary>
    /// 验证默认文件描述信息的可选属性为空。
    /// </summary>
    [Fact]
    public void DefaultConstructor_CreatesEmptyInstance()
    {
        var fd = new FileDescriptor();
        fd.Name.ShouldBeNull();
        fd.DirectoryName.ShouldBeNull();
        fd.StorageName.ShouldBeNull();
        fd.Md5.ShouldBeNull();
    }

    /// <summary>
    /// 验证文件名构造函数保存名称并提取扩展名。
    /// </summary>
    [Fact]
    public void Constructor_WithNameOnly_SetsNameAndExtension()
    {
        var fd = new FileDescriptor("report.pdf");
        fd.Name.ShouldBe("report.pdf");
        fd.Extension.ShouldBe("pdf");
    }

    /// <summary>
    /// 验证带大小的构造函数保存文件名。
    /// </summary>
    [Fact]
    public void Constructor_WithNameAndSize_SetsSizeCorrectly()
    {
        var fd = new FileDescriptor("data.csv", 1024);
        fd.Name.ShouldBe("data.csv");
    }

    /// <summary>
    /// 验证文件描述构造函数拒绝空文件名。
    /// </summary>
    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor(null));
    }

    /// <summary>
    /// 验证文件描述构造函数拒绝空白文件名。
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor("   "));
    }

    /// <summary>
    /// 验证文件描述构造函数拒绝空文本文件名。
    /// </summary>
    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor(""));
    }

    /// <summary>
    /// 验证不含点号的文件名没有扩展名。
    /// </summary>
    [Fact]
    public void Constructor_FileWithNoExtension_ExtensionIsEmpty()
    {
        var fd = new FileDescriptor("Makefile");
        fd.Extension.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证点号开头的文件名提取点号后的扩展名。
    /// </summary>
    [Fact]
    public void Constructor_DotFileNameOnly_ExtensionIsEmpty()
    {
        var fd = new FileDescriptor(".gitignore");
        fd.Extension.ShouldBe("gitignore");
    }

    /// <summary>
    /// 验证构造后可以设置目录名、存储名和 MD5。
    /// </summary>
    [Fact]
    public void Properties_CanBeSetAfterConstruction()
    {
        var fd = new FileDescriptor("file.txt");
        fd.DirectoryName = "/uploads";
        fd.StorageName = "uuid-1234.txt";
        fd.Md5 = "abc123";

        fd.DirectoryName.ShouldBe("/uploads");
        fd.StorageName.ShouldBe("uuid-1234.txt");
        fd.Md5.ShouldBe("abc123");
    }
}

// ============================================================
//  ZipHelper
// ============================================================

/// <summary>
/// 测试 <see cref="ZipHelper"/> 的参数守卫与正常路径
/// </summary>
[Trait("Bing.IO", "ZipHelper")]
public class ZipHelperTests : IDisposable
{
    /// <summary>
    /// 压缩测试使用的临时源目录。
    /// </summary>
    private readonly string _testDir;
    /// <summary>
    /// 压缩测试使用的临时压缩文件路径。
    /// </summary>
    private readonly string _zipPath;

    /// <summary>
    /// 初始化一个 <see cref="ZipHelperTests"/> 类型的实例。
    /// </summary>
    public ZipHelperTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ZipHelperTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _zipPath = Path.Combine(Path.GetTempPath(), $"ZipHelperTests_{Guid.NewGuid():N}.zip");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        if (File.Exists(_zipPath)) File.Delete(_zipPath);
        // 清理解压目录
        var unzipDir = _zipPath.Replace(".zip", "_unzip");
        if (Directory.Exists(unzipDir)) Directory.Delete(unzipDir, true);
    }

    // ── Zip guard clauses ──

    /// <summary>
    /// 验证压缩操作拒绝空源目录。
    /// </summary>
    [Fact]
    public void Zip_NullFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(null, _zipPath));
    }

    /// <summary>
    /// 验证压缩操作拒绝空文本源目录。
    /// </summary>
    [Fact]
    public void Zip_EmptyFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip("", _zipPath));
    }

    /// <summary>
    /// 验证压缩操作拒绝空目标压缩文件路径。
    /// </summary>
    [Fact]
    public void Zip_NullZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(_testDir, null));
    }

    /// <summary>
    /// 验证压缩操作拒绝空文本目标压缩文件路径。
    /// </summary>
    [Fact]
    public void Zip_EmptyZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(_testDir, ""));
    }

    /// <summary>
    /// 验证压缩操作拒绝不存在的源目录。
    /// </summary>
    [Fact]
    public void Zip_NonExistentFolder_ThrowsDirectoryNotFoundException()
    {
        Should.Throw<DirectoryNotFoundException>(() =>
            ZipHelper.Zip(Path.Combine(_testDir, "nonexistent"), _zipPath));
    }

    // ── UnZip guard clauses ──

    /// <summary>
    /// 验证解压操作拒绝空压缩文件路径。
    /// </summary>
    [Fact]
    public void UnZip_NullZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(null, _testDir));
    }

    /// <summary>
    /// 验证解压操作拒绝空文本压缩文件路径。
    /// </summary>
    [Fact]
    public void UnZip_EmptyZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip("", _testDir));
    }

    /// <summary>
    /// 验证解压操作拒绝空目标目录。
    /// </summary>
    [Fact]
    public void UnZip_NullFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(_zipPath, null));
    }

    /// <summary>
    /// 验证解压操作拒绝空文本目标目录。
    /// </summary>
    [Fact]
    public void UnZip_EmptyFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(_zipPath, ""));
    }

    /// <summary>
    /// 验证解压操作拒绝不存在的压缩文件。
    /// </summary>
    [Fact]
    public void UnZip_NonExistentZipFile_ThrowsFileNotFoundException()
    {
        Should.Throw<FileNotFoundException>(() =>
            ZipHelper.UnZip(Path.Combine(_testDir, "nonexistent.zip"), _testDir));
    }

    // ── Roundtrip ──

    /// <summary>
    /// 验证目录压缩及解压后保留文件内容。
    /// </summary>
    [Fact]
    public void Zip_And_UnZip_Roundtrip_PreservesFileContent()
    {
        // Arrange: 在 _testDir 中写两个文件
        File.WriteAllText(Path.Combine(_testDir, "hello.txt"), "Hello World");
        File.WriteAllText(Path.Combine(_testDir, "data.csv"), "a,b,c");

        // Act: 压缩
        ZipHelper.Zip(_testDir, _zipPath);
        File.Exists(_zipPath).ShouldBeTrue();

        // Act: 解压到新目录
        var unzipDir = _zipPath.Replace(".zip", "_unzip");
        ZipHelper.UnZip(_zipPath, unzipDir);

        // Assert: 文件还原
        File.Exists(Path.Combine(unzipDir, "hello.txt")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(unzipDir, "hello.txt")).ShouldBe("Hello World");
        File.ReadAllText(Path.Combine(unzipDir, "data.csv")).ShouldBe("a,b,c");
    }
}

// ============================================================
//  LargeMemoryStream
// ============================================================

/// <summary>
/// 测试 <see cref="LargeMemoryStream"/> 的基本流操作
/// </summary>
[Trait("Bing.IO", "LargeMemoryStream")]
public class LargeMemoryStreamTests
{
    /// <summary>
    /// 验证新建大型内存流可读。
    /// </summary>
    [Fact]
    public void CanRead_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建大型内存流可定位。
    /// </summary>
    [Fact]
    public void CanSeek_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanSeek.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建大型内存流可写。
    /// </summary>
    [Fact]
    public void CanWrite_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建大型内存流长度为零。
    /// </summary>
    [Fact]
    public void InitialLength_IsZero()
    {
        using var stream = new LargeMemoryStream();
        stream.Length.ShouldBe(0L);
    }

    /// <summary>
    /// 验证新建大型内存流的位置为零。
    /// </summary>
    [Fact]
    public void InitialPosition_IsZero()
    {
        using var stream = new LargeMemoryStream();
        stream.Position.ShouldBe(0L);
    }

    /// <summary>
    /// 验证大型内存流写入后更新长度和位置。
    /// </summary>
    [Fact]
    public void Write_UpdatesLengthAndPosition()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, data.Length);
        stream.Length.ShouldBe(5L);
        stream.Position.ShouldBe(5L);
    }

    /// <summary>
    /// 验证大型内存流写入后可读回原始字节。
    /// </summary>
    [Fact]
    public void Write_ThenRead_ReturnsCorrectData()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[] { 10, 20, 30, 40 };
        stream.Write(data, 0, data.Length);

        // Seek back to start
        stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[data.Length];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        bytesRead.ShouldBe(data.Length);
        buffer.ShouldBe(data);
    }

    /// <summary>
    /// 验证大型内存流按起点偏移设置位置。
    /// </summary>
    [Fact]
    public void Seek_Begin_SetsPositionCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[100];
        stream.Write(data, 0, data.Length);

        stream.Seek(10, SeekOrigin.Begin);
        stream.Position.ShouldBe(10L);
    }

    /// <summary>
    /// 验证大型内存流按当前位置偏移设置位置。
    /// </summary>
    [Fact]
    public void Seek_Current_SetsPositionCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[100];
        stream.Write(data, 0, data.Length);

        stream.Seek(0, SeekOrigin.Begin);
        stream.Seek(15, SeekOrigin.Current);
        stream.Position.ShouldBe(15L);
    }

    /// <summary>
    /// 验证大型内存流定位到末尾。
    /// </summary>
    [Fact]
    public void Seek_End_SetsPositionCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[100];
        stream.Write(data, 0, data.Length);

        stream.Seek(0, SeekOrigin.End);
        stream.Position.ShouldBe(100L);
    }

    /// <summary>
    /// 验证大型内存流可以截断到较短长度。
    /// </summary>
    [Fact]
    public void SetLength_TruncatesStream()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[50];
        stream.Write(data, 0, data.Length);

        stream.SetLength(10);
        stream.Length.ShouldBe(10L);
    }

    /// <summary>
    /// 验证大型内存流长度归零后清空内容。
    /// </summary>
    [Fact]
    public void SetLength_Zero_ClearsStream()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[50];
        stream.Write(data, 0, data.Length);

        stream.SetLength(0);
        stream.Length.ShouldBe(0L);
    }

    /// <summary>
    /// 验证大型内存流拒绝负长度。
    /// </summary>
    [Fact]
    public void SetLength_Negative_Throws()
    {
        using var stream = new LargeMemoryStream();
        Should.Throw<InvalidOperationException>(() => stream.SetLength(-1));
    }

    /// <summary>
    /// 验证大型内存流拒绝超出长度的位置。
    /// </summary>
    [Fact]
    public void Position_BeyondLength_Throws()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[10];
        stream.Write(data, 0, data.Length);

        Should.Throw<InvalidOperationException>(() => stream.Position = 20);
    }

    /// <summary>
    /// 验证大型内存流拒绝负位置。
    /// </summary>
    [Fact]
    public void Position_Negative_Throws()
    {
        using var stream = new LargeMemoryStream();
        Should.Throw<InvalidOperationException>(() => stream.Position = -1);
    }

    /// <summary>
    /// 验证活动的大型内存流刷新时不抛出异常。
    /// </summary>
    [Fact]
    public void Flush_OnActive_Stream_DoesNotThrow()
    {
        using var stream = new LargeMemoryStream();
        Should.NotThrow(() => stream.Flush());
    }

    /// <summary>
    /// 验证大型内存流释放后拒绝刷新。
    /// </summary>
    [Fact]
    public void Dispose_ThenFlush_ThrowsObjectDisposedException()
    {
        var stream = new LargeMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Flush());
    }

    /// <summary>
    /// 验证大型内存流释放后拒绝写入。
    /// </summary>
    [Fact]
    public void Dispose_ThenWrite_ThrowsObjectDisposedException()
    {
        var stream = new LargeMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
    }

    /// <summary>
    /// 验证大型内存流按顺序保留多次写入的字节。
    /// </summary>
    [Fact]
    public void Write_MultipleChunks_ReadBackCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var chunk1 = new byte[] { 1, 2, 3 };
        var chunk2 = new byte[] { 4, 5, 6 };
        stream.Write(chunk1, 0, chunk1.Length);
        stream.Write(chunk2, 0, chunk2.Length);

        stream.Seek(0, SeekOrigin.Begin);
        var buffer = new byte[6];
        stream.Read(buffer, 0, 6);
        buffer.ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    /// <summary>
    /// 验证大型内存流写入后提供非空跨度。
    /// </summary>
    [Fact]
    public void GetSpan_ReturnsNonEmpty_AfterWrite()
    {
        using var stream = new LargeMemoryStream();
        stream.Write(new byte[] { 1, 2 }, 0, 2);
        var span = stream.GetSpan();
        span.Length.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// 验证大型内存流写入后提供非空内存。
    /// </summary>
    [Fact]
    public void GetMemory_ReturnsNonEmpty_AfterWrite()
    {
        using var stream = new LargeMemoryStream();
        stream.Write(new byte[] { 1, 2 }, 0, 2);
        var mem = stream.GetMemory();
        mem.Length.ShouldBeGreaterThan(0);
    }
}

// ============================================================
//  PooledMemoryStream
// ============================================================

/// <summary>
/// 测试 <see cref="PooledMemoryStream"/> 的基本流操作
/// </summary>
[Trait("Bing.IO", "PooledMemoryStream")]
public class PooledMemoryStreamTests
{
    /// <summary>
    /// 验证新建池化内存流可读。
    /// </summary>
    [Fact]
    public void CanRead_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建池化内存流可定位。
    /// </summary>
    [Fact]
    public void CanSeek_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanSeek.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建池化内存流可写。
    /// </summary>
    [Fact]
    public void CanWrite_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 验证新建池化内存流长度为零。
    /// </summary>
    [Fact]
    public void InitialLength_IsZero()
    {
        using var stream = new PooledMemoryStream();
        stream.Length.ShouldBe(0L);
    }

    /// <summary>
    /// 验证池化内存流从字节数组初始化长度和内容。
    /// </summary>
    [Fact]
    public void Constructor_FromByteArray_SetsLengthAndContent()
    {
        var data = new byte[] { 10, 20, 30 };
        using var stream = new PooledMemoryStream(data);
        stream.Length.ShouldBe(3);
        var buf = new byte[3];
        stream.Read(buf, 0, 3);
        buf.ShouldBe(data);
    }

    /// <summary>
    /// 验证池化内存流写入后可读回原始字节。
    /// </summary>
    [Fact]
    public void Write_ThenRead_RoundTrip()
    {
        using var stream = new PooledMemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, data.Length);

        stream.Seek(0, SeekOrigin.Begin);
        var buf = new byte[5];
        stream.Read(buf, 0, 5);
        buf.ShouldBe(data);
    }

    /// <summary>
    /// 验证池化内存流写入后更新长度和位置。
    /// </summary>
    [Fact]
    public void Write_UpdatesLengthAndPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[10], 0, 10);
        stream.Length.ShouldBe(10L);
        stream.Position.ShouldBe(10L);
    }

    /// <summary>
    /// 验证池化内存流按起点偏移设置位置。
    /// </summary>
    [Fact]
    public void Seek_Begin_SetsPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[50], 0, 50);
        stream.Seek(5, SeekOrigin.Begin);
        stream.Position.ShouldBe(5L);
    }

    /// <summary>
    /// 验证池化内存流按当前位置偏移设置位置。
    /// </summary>
    [Fact]
    public void Seek_Current_SetsPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[50], 0, 50);
        stream.Seek(0, SeekOrigin.Begin);
        stream.Seek(10, SeekOrigin.Current);
        stream.Position.ShouldBe(10L);
    }

    /// <summary>
    /// 验证池化内存流定位到末尾。
    /// </summary>
    [Fact]
    public void Seek_End_SetsPositionAtEnd()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[20], 0, 20);
        stream.Seek(0, SeekOrigin.End);
        stream.Position.ShouldBe(20L);
    }

    /// <summary>
    /// 验证池化内存流可以截断到较短长度。
    /// </summary>
    [Fact]
    public void SetLength_TruncatesStream()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[30], 0, 30);
        stream.SetLength(10);
        stream.Length.ShouldBe(10L);
    }

    /// <summary>
    /// 验证池化内存流拒绝负长度。
    /// </summary>
    [Fact]
    public void SetLength_Negative_Throws()
    {
        using var stream = new PooledMemoryStream();
        Should.Throw<ArgumentOutOfRangeException>(() => stream.SetLength(-1));
    }

    /// <summary>
    /// 验证池化内存流转换为原始字节数组。
    /// </summary>
    [Fact]
    public void ToArray_ReturnsCorrectBytes()
    {
        var data = new byte[] { 7, 8, 9 };
        using var stream = new PooledMemoryStream(data);
        var result = stream.ToArray();
        result.ShouldBe(data);
    }

    /// <summary>
    /// 验证池化内存流返回预期缓冲区字节。
    /// </summary>
    [Fact]
    public void GetBuffer_ReturnsCorrectBytes()
    {
        var data = new byte[] { 5, 6 };
        using var stream = new PooledMemoryStream(data);
        var buf = stream.GetBuffer();
        buf.ShouldBe(data);
    }

    /// <summary>
    /// 验证池化内存流跨度的长度及指定位置字节。
    /// </summary>
    [Fact]
    public void GetSpan_ReturnsCorrectContent()
    {
        var data = new byte[] { 11, 22, 33 };
        using var stream = new PooledMemoryStream(data);
        var span = stream.GetSpan();
        span.Length.ShouldBe(3);
        span[0].ShouldBe((byte)11);
        span[2].ShouldBe((byte)33);
    }

    /// <summary>
    /// 验证池化内存流内存的长度及首字节。
    /// </summary>
    [Fact]
    public void GetMemory_ReturnsCorrectContent()
    {
        var data = new byte[] { 100, 200 };
        using var stream = new PooledMemoryStream(data);
        var mem = stream.GetMemory();
        mem.Length.ShouldBe(2);
        mem.Span[0].ShouldBe((byte)100);
    }

    /// <summary>
    /// 验证池化内存流数组段的元素数量。
    /// </summary>
    [Fact]
    public void ToArraySegment_ReturnsCorrectContent()
    {
        var data = new byte[] { 3, 6, 9 };
        using var stream = new PooledMemoryStream(data);
        var seg = stream.ToArraySegment();
        seg.Count.ShouldBe(3);
    }

    /// <summary>
    /// 验证池化内存流内容复制到目标流。
    /// </summary>
    [Fact]
    public void WriteTo_CopiesContentToAnotherStream()
    {
        var data = new byte[] { 1, 2, 3 };
        using var src = new PooledMemoryStream(data);
        using var dst = new System.IO.MemoryStream();
        src.WriteTo(dst);
        dst.ToArray().ShouldBe(data);
    }

    /// <summary>
    /// 验证池化内存流复制拒绝空目标流。
    /// </summary>
    [Fact]
    public void WriteTo_NullStream_Throws()
    {
        using var stream = new PooledMemoryStream();
        Should.Throw<ArgumentNullException>(() => stream.WriteTo(null));
    }

    /// <summary>
    /// 验证池化内存流按顺序枚举全部字节。
    /// </summary>
    [Fact]
    public void GetEnumerator_EnumeratesAllBytes()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        using var stream = new PooledMemoryStream(data);
        var list = stream.ToList();
        list.ShouldBe(data);
    }

    /// <summary>
    /// 验证活动的池化内存流刷新时不抛出异常。
    /// </summary>
    [Fact]
    public void Flush_OnActive_Stream_DoesNotThrow()
    {
        using var stream = new PooledMemoryStream();
        Should.NotThrow(() => stream.Flush());
    }

    /// <summary>
    /// 验证池化内存流释放后拒绝刷新。
    /// </summary>
    [Fact]
    public void Dispose_ThenFlush_ThrowsObjectDisposedException()
    {
        var stream = new PooledMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Flush());
    }

    /// <summary>
    /// 验证池化内存流释放后拒绝写入。
    /// </summary>
    [Fact]
    public void Dispose_ThenWrite_ThrowsObjectDisposedException()
    {
        var stream = new PooledMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
    }

    /// <summary>
    /// 验证池化内存流写入后的容量覆盖内容长度。
    /// </summary>
    [Fact]
    public void Capacity_IsGreaterOrEqualToLength_AfterWrite()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[20], 0, 20);
        stream.Capacity.ShouldBeGreaterThanOrEqualTo(stream.Length);
    }

    /// <summary>
    /// 验证池化内存流可使用自定义数组池并写入。
    /// </summary>
    [Fact]
    public void Constructor_WithCustomArrayPool_Works()
    {
        using var stream = new PooledMemoryStream(System.Buffers.ArrayPool<byte>.Create(), capacity: 64);
        stream.Write(new byte[10], 0, 10);
        stream.Length.ShouldBe(10L);
    }
}
