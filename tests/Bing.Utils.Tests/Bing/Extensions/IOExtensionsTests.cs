using System;
using System.IO;
using Bing.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="FileInfoExtensions"/>
/// (CompareTo / Read / ReadBinary)
/// </summary>
public class FileInfoExtensionsTests : IDisposable
{
    private readonly string _tmpDir;

    public FileInfoExtensionsTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    private FileInfo CreateFile(string name, string content)
    {
        var path = Path.Combine(_tmpDir, name);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    // ──────────────────────────────────────────
    //  Read
    // ──────────────────────────────────────────

    [Fact]
    public void Read_ExistingFile_ReturnsContent()
    {
        var fi = CreateFile("a.txt", "hello");
        fi.Read().ShouldBe("hello");
    }

    [Fact]
    public void Read_NonExistingFile_ReturnsEmpty()
    {
        var fi = new FileInfo(Path.Combine(_tmpDir, "nonexistent.txt"));
        fi.Read().ShouldBe(string.Empty);
    }

    [Fact]
    public void Read_NullFileInfo_ThrowsArgumentNullException()
    {
        FileInfo fi = null;
        Should.Throw<ArgumentNullException>(() => fi.Read());
    }

    // ──────────────────────────────────────────
    //  ReadBinary
    // ──────────────────────────────────────────

    [Fact]
    public void ReadBinary_ExistingFile_ReturnsByteContent()
    {
        var path = Path.Combine(_tmpDir, "bin.dat");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x02, 0x03 });
        var fi = new FileInfo(path);
        fi.ReadBinary().ShouldBe(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void ReadBinary_NonExistingFile_ReturnsEmptyArray()
    {
        var fi = new FileInfo(Path.Combine(_tmpDir, "missing.dat"));
        fi.ReadBinary().ShouldBeEmpty();
    }

    [Fact]
    public void ReadBinary_NullFileInfo_ThrowsArgumentNullException()
    {
        FileInfo fi = null;
        Should.Throw<ArgumentNullException>(() => fi.ReadBinary());
    }

    // ──────────────────────────────────────────
    //  CompareTo
    // ──────────────────────────────────────────

    [Fact]
    public void CompareTo_IdenticalContent_ReturnsTrue()
    {
        var f1 = CreateFile("c1.txt", "same content");
        var f2 = CreateFile("c2.txt", "same content");
        f1.CompareTo(f2).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_DifferentContent_ReturnsFalse()
    {
        var f1 = CreateFile("d1.txt", "hello");
        var f2 = CreateFile("d2.txt", "world");
        f1.CompareTo(f2).ShouldBeFalse();
    }

    [Fact]
    public void CompareTo_DifferentLength_ReturnsFalse()
    {
        var f1 = CreateFile("e1.txt", "hi");
        var f2 = CreateFile("e2.txt", "hello");
        f1.CompareTo(f2).ShouldBeFalse();
    }

    [Fact]
    public void CompareTo_NullFile1_ThrowsArgumentNullException()
    {
        var f2 = CreateFile("f2.txt", "x");
        FileInfo f1 = null;
        Should.Throw<ArgumentNullException>(() => f1.CompareTo(f2));
    }

    [Fact]
    public void CompareTo_NullFile2_ThrowsArgumentNullException()
    {
        var f1 = CreateFile("g1.txt", "x");
        Should.Throw<ArgumentNullException>(() => f1.CompareTo(null));
    }
}

/// <summary>
/// 测试 <see cref="StreamExtensions"/>
/// (ToFile / ContentsEqual / GetMd5)
/// </summary>
public class StreamExtensionsTests : IDisposable
{
    private readonly string _tmpDir;

    public StreamExtensionsTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    // ──────────────────────────────────────────
    //  ToFile
    // ──────────────────────────────────────────

    [Fact]
    public void ToFile_ValidStream_WritesFile()
    {
        var path = Path.Combine(_tmpDir, "out.dat");
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var result = ms.ToFile(path);
        result.ShouldBeTrue();
        File.ReadAllBytes(path).ShouldBe(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void ToFile_NullStream_ReturnsFalse()
    {
        Stream s = null;
        s.ToFile(Path.Combine(_tmpDir, "x.dat")).ShouldBeFalse();
    }

    [Fact]
    public void ToFile_EmptyPath_ReturnsFalse()
    {
        using var ms = new MemoryStream(new byte[] { 1 });
        ms.ToFile(string.Empty).ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  ContentsEqual
    // ──────────────────────────────────────────

    [Fact]
    public void ContentsEqual_SameBytes_ReturnsTrue()
    {
        using var a = new MemoryStream(new byte[] { 1, 2, 3 });
        using var b = new MemoryStream(new byte[] { 1, 2, 3 });
        a.ContentsEqual(b).ShouldBeTrue();
    }

    [Fact]
    public void ContentsEqual_DifferentBytes_ReturnsFalse()
    {
        using var a = new MemoryStream(new byte[] { 1, 2, 3 });
        using var b = new MemoryStream(new byte[] { 1, 2, 4 });
        a.ContentsEqual(b).ShouldBeFalse();
    }

    [Fact]
    public void ContentsEqual_DifferentLengths_ReturnsFalse()
    {
        using var a = new MemoryStream(new byte[] { 1, 2 });
        using var b = new MemoryStream(new byte[] { 1, 2, 3 });
        a.ContentsEqual(b).ShouldBeFalse();
    }

    [Fact]
    public void ContentsEqual_EmptyStreams_ReturnsTrue()
    {
        using var a = new MemoryStream();
        using var b = new MemoryStream();
        a.ContentsEqual(b).ShouldBeTrue();
    }

    // ──────────────────────────────────────────
    //  GetMd5
    // ──────────────────────────────────────────

    [Fact]
    public void GetMd5_SameContent_ReturnsSameHash()
    {
        using var a = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        using var b = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        a.GetMd5().ShouldBe(b.GetMd5());
    }

    [Fact]
    public void GetMd5_DifferentContent_ReturnsDifferentHash()
    {
        using var a = new MemoryStream(new byte[] { 1, 2, 3 });
        using var b = new MemoryStream(new byte[] { 3, 2, 1 });
        a.GetMd5().ShouldNotBe(b.GetMd5());
    }

    [Fact]
    public void GetMd5_ReturnsLowercaseHex()
    {
        using var ms = new MemoryStream(new byte[] { 0xAB, 0xCD });
        var hash = ms.GetMd5();
        hash.ShouldBe(hash.ToLower());
        hash.Length.ShouldBe(32);
    }
}
