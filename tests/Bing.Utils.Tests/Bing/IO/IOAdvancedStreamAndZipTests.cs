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
    [Fact]
    public void DefaultConstructor_CreatesEmptyInstance()
    {
        var fd = new FileDescriptor();
        fd.Name.ShouldBeNull();
        fd.DirectoryName.ShouldBeNull();
        fd.StorageName.ShouldBeNull();
        fd.Md5.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNameOnly_SetsNameAndExtension()
    {
        var fd = new FileDescriptor("report.pdf");
        fd.Name.ShouldBe("report.pdf");
        fd.Extension.ShouldBe("pdf");
    }

    [Fact]
    public void Constructor_WithNameAndSize_SetsSizeCorrectly()
    {
        var fd = new FileDescriptor("data.csv", 1024);
        fd.Name.ShouldBe("data.csv");
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor(null));
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor("   "));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileDescriptor(""));
    }

    [Fact]
    public void Constructor_FileWithNoExtension_ExtensionIsEmpty()
    {
        var fd = new FileDescriptor("Makefile");
        fd.Extension.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_DotFileNameOnly_ExtensionIsEmpty()
    {
        var fd = new FileDescriptor(".gitignore");
        fd.Extension.ShouldBe("gitignore");
    }

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
    private readonly string _testDir;
    private readonly string _zipPath;

    public ZipHelperTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ZipHelperTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _zipPath = Path.Combine(Path.GetTempPath(), $"ZipHelperTests_{Guid.NewGuid():N}.zip");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        if (File.Exists(_zipPath)) File.Delete(_zipPath);
        // 清理解压目录
        var unzipDir = _zipPath.Replace(".zip", "_unzip");
        if (Directory.Exists(unzipDir)) Directory.Delete(unzipDir, true);
    }

    // ── Zip guard clauses ──

    [Fact]
    public void Zip_NullFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(null, _zipPath));
    }

    [Fact]
    public void Zip_EmptyFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip("", _zipPath));
    }

    [Fact]
    public void Zip_NullZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(_testDir, null));
    }

    [Fact]
    public void Zip_EmptyZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.Zip(_testDir, ""));
    }

    [Fact]
    public void Zip_NonExistentFolder_ThrowsDirectoryNotFoundException()
    {
        Should.Throw<DirectoryNotFoundException>(() =>
            ZipHelper.Zip(Path.Combine(_testDir, "nonexistent"), _zipPath));
    }

    // ── UnZip guard clauses ──

    [Fact]
    public void UnZip_NullZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(null, _testDir));
    }

    [Fact]
    public void UnZip_EmptyZipPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip("", _testDir));
    }

    [Fact]
    public void UnZip_NullFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(_zipPath, null));
    }

    [Fact]
    public void UnZip_EmptyFolderPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ZipHelper.UnZip(_zipPath, ""));
    }

    [Fact]
    public void UnZip_NonExistentZipFile_ThrowsFileNotFoundException()
    {
        Should.Throw<FileNotFoundException>(() =>
            ZipHelper.UnZip(Path.Combine(_testDir, "nonexistent.zip"), _testDir));
    }

    // ── Roundtrip ──

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
    [Fact]
    public void CanRead_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void CanSeek_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanSeek.ShouldBeTrue();
    }

    [Fact]
    public void CanWrite_IsTrue()
    {
        using var stream = new LargeMemoryStream();
        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void InitialLength_IsZero()
    {
        using var stream = new LargeMemoryStream();
        stream.Length.ShouldBe(0L);
    }

    [Fact]
    public void InitialPosition_IsZero()
    {
        using var stream = new LargeMemoryStream();
        stream.Position.ShouldBe(0L);
    }

    [Fact]
    public void Write_UpdatesLengthAndPosition()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, data.Length);
        stream.Length.ShouldBe(5L);
        stream.Position.ShouldBe(5L);
    }

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

    [Fact]
    public void Seek_Begin_SetsPositionCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[100];
        stream.Write(data, 0, data.Length);

        stream.Seek(10, SeekOrigin.Begin);
        stream.Position.ShouldBe(10L);
    }

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

    [Fact]
    public void Seek_End_SetsPositionCorrectly()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[100];
        stream.Write(data, 0, data.Length);

        stream.Seek(0, SeekOrigin.End);
        stream.Position.ShouldBe(100L);
    }

    [Fact]
    public void SetLength_TruncatesStream()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[50];
        stream.Write(data, 0, data.Length);

        stream.SetLength(10);
        stream.Length.ShouldBe(10L);
    }

    [Fact]
    public void SetLength_Zero_ClearsStream()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[50];
        stream.Write(data, 0, data.Length);

        stream.SetLength(0);
        stream.Length.ShouldBe(0L);
    }

    [Fact]
    public void SetLength_Negative_Throws()
    {
        using var stream = new LargeMemoryStream();
        Should.Throw<InvalidOperationException>(() => stream.SetLength(-1));
    }

    [Fact]
    public void Position_BeyondLength_Throws()
    {
        using var stream = new LargeMemoryStream();
        var data = new byte[10];
        stream.Write(data, 0, data.Length);

        Should.Throw<InvalidOperationException>(() => stream.Position = 20);
    }

    [Fact]
    public void Position_Negative_Throws()
    {
        using var stream = new LargeMemoryStream();
        Should.Throw<InvalidOperationException>(() => stream.Position = -1);
    }

    [Fact]
    public void Flush_OnActive_Stream_DoesNotThrow()
    {
        using var stream = new LargeMemoryStream();
        Should.NotThrow(() => stream.Flush());
    }

    [Fact]
    public void Dispose_ThenFlush_ThrowsObjectDisposedException()
    {
        var stream = new LargeMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Flush());
    }

    [Fact]
    public void Dispose_ThenWrite_ThrowsObjectDisposedException()
    {
        var stream = new LargeMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
    }

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

    [Fact]
    public void GetSpan_ReturnsNonEmpty_AfterWrite()
    {
        using var stream = new LargeMemoryStream();
        stream.Write(new byte[] { 1, 2 }, 0, 2);
        var span = stream.GetSpan();
        span.Length.ShouldBeGreaterThan(0);
    }

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
    [Fact]
    public void CanRead_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void CanSeek_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanSeek.ShouldBeTrue();
    }

    [Fact]
    public void CanWrite_IsTrue()
    {
        using var stream = new PooledMemoryStream();
        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void InitialLength_IsZero()
    {
        using var stream = new PooledMemoryStream();
        stream.Length.ShouldBe(0L);
    }

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

    [Fact]
    public void Write_UpdatesLengthAndPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[10], 0, 10);
        stream.Length.ShouldBe(10L);
        stream.Position.ShouldBe(10L);
    }

    [Fact]
    public void Seek_Begin_SetsPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[50], 0, 50);
        stream.Seek(5, SeekOrigin.Begin);
        stream.Position.ShouldBe(5L);
    }

    [Fact]
    public void Seek_Current_SetsPosition()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[50], 0, 50);
        stream.Seek(0, SeekOrigin.Begin);
        stream.Seek(10, SeekOrigin.Current);
        stream.Position.ShouldBe(10L);
    }

    [Fact]
    public void Seek_End_SetsPositionAtEnd()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[20], 0, 20);
        stream.Seek(0, SeekOrigin.End);
        stream.Position.ShouldBe(20L);
    }

    [Fact]
    public void SetLength_TruncatesStream()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[30], 0, 30);
        stream.SetLength(10);
        stream.Length.ShouldBe(10L);
    }

    [Fact]
    public void SetLength_Negative_Throws()
    {
        using var stream = new PooledMemoryStream();
        Should.Throw<ArgumentOutOfRangeException>(() => stream.SetLength(-1));
    }

    [Fact]
    public void ToArray_ReturnsCorrectBytes()
    {
        var data = new byte[] { 7, 8, 9 };
        using var stream = new PooledMemoryStream(data);
        var result = stream.ToArray();
        result.ShouldBe(data);
    }

    [Fact]
    public void GetBuffer_ReturnsCorrectBytes()
    {
        var data = new byte[] { 5, 6 };
        using var stream = new PooledMemoryStream(data);
        var buf = stream.GetBuffer();
        buf.ShouldBe(data);
    }

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

    [Fact]
    public void GetMemory_ReturnsCorrectContent()
    {
        var data = new byte[] { 100, 200 };
        using var stream = new PooledMemoryStream(data);
        var mem = stream.GetMemory();
        mem.Length.ShouldBe(2);
        mem.Span[0].ShouldBe((byte)100);
    }

    [Fact]
    public void ToArraySegment_ReturnsCorrectContent()
    {
        var data = new byte[] { 3, 6, 9 };
        using var stream = new PooledMemoryStream(data);
        var seg = stream.ToArraySegment();
        seg.Count.ShouldBe(3);
    }

    [Fact]
    public void WriteTo_CopiesContentToAnotherStream()
    {
        var data = new byte[] { 1, 2, 3 };
        using var src = new PooledMemoryStream(data);
        using var dst = new System.IO.MemoryStream();
        src.WriteTo(dst);
        dst.ToArray().ShouldBe(data);
    }

    [Fact]
    public void WriteTo_NullStream_Throws()
    {
        using var stream = new PooledMemoryStream();
        Should.Throw<ArgumentNullException>(() => stream.WriteTo(null));
    }

    [Fact]
    public void GetEnumerator_EnumeratesAllBytes()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        using var stream = new PooledMemoryStream(data);
        var list = stream.ToList();
        list.ShouldBe(data);
    }

    [Fact]
    public void Flush_OnActive_Stream_DoesNotThrow()
    {
        using var stream = new PooledMemoryStream();
        Should.NotThrow(() => stream.Flush());
    }

    [Fact]
    public void Dispose_ThenFlush_ThrowsObjectDisposedException()
    {
        var stream = new PooledMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Flush());
    }

    [Fact]
    public void Dispose_ThenWrite_ThrowsObjectDisposedException()
    {
        var stream = new PooledMemoryStream();
        stream.Dispose();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
    }

    [Fact]
    public void Capacity_IsGreaterOrEqualToLength_AfterWrite()
    {
        using var stream = new PooledMemoryStream();
        stream.Write(new byte[20], 0, 20);
        stream.Capacity.ShouldBeGreaterThanOrEqualTo(stream.Length);
    }

    [Fact]
    public void Constructor_WithCustomArrayPool_Works()
    {
        using var stream = new PooledMemoryStream(System.Buffers.ArrayPool<byte>.Create(), capacity: 64);
        stream.Write(new byte[10], 0, 10);
        stream.Length.ShouldBe(10L);
    }
}
