using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Shouldly;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Bing.IO.Buffers;

/// <summary>
/// 测试 <see cref="BinaryDigitReader"/>
/// 覆盖 Span-based 和 byte[]-based 读取方法
/// </summary>
[Trait("Bing.IO.Buffers", "BinaryDigitReader")]
public class BinaryDigitReaderTests
{
    // ──────────────────────────────────────────
    //  Span-based — ReadInt16 / ReadUInt16
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt16_Span_ReturnsCorrectValue()
    {
        // 0x0102 in little-endian: byte[0]=0x02, byte[1]=0x01
        Span<byte> buf = new byte[] { 0x02, 0x01 };
        BinaryDigitReader.ReadInt16(buf).ShouldBe((short)0x0102);
    }

    [Fact]
    public void ReadUInt16_Span_ReturnsCorrectValue()
    {
        Span<byte> buf = new byte[] { 0xCD, 0xAB };
        BinaryDigitReader.ReadUInt16(buf).ShouldBe((ushort)0xABCD);
    }

    // ──────────────────────────────────────────
    //  Span-based — ReadInt32 / ReadUInt32
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt32_Span_ReturnsCorrectValue()
    {
        Span<byte> buf = new byte[] { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 LE
        BinaryDigitReader.ReadInt32(buf).ShouldBe(0x12345678);
    }

    [Fact]
    public void ReadUInt32_Span_ReturnsCorrectValue()
    {
        Span<byte> buf = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE }; // 0xDEADBEEF LE
        BinaryDigitReader.ReadUInt32(buf).ShouldBe(0xDEADBEEFU);
    }

    [Fact]
    public void ReadInt32_Span_Zero_ReturnsZero()
    {
        Span<byte> buf = new byte[4];
        BinaryDigitReader.ReadInt32(buf).ShouldBe(0);
    }

    // ──────────────────────────────────────────
    //  Span-based — ReadInt64 / ReadUInt64
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt64_Span_ReturnsCorrectValue()
    {
        // long 0x0102030405060708 in little-endian
        Span<byte> buf = new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 };
        BinaryDigitReader.ReadInt64(buf).ShouldBe(0x0102030405060708L);
    }

    [Fact]
    public void ReadUInt64_Span_ReturnsCorrectValue()
    {
        Span<byte> buf = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        BinaryDigitReader.ReadUInt64(buf).ShouldBe(1UL);
    }

    // ──────────────────────────────────────────
    //  byte[]-based — ReadInt16 / ReadUInt16
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt16_ByteArray_ReturnsCorrectValue()
    {
        var buf = new byte[] { 0xFF, 0xFF, 0x02, 0x01 }; // 0x0102 at position 2
        BinaryDigitReader.ReadInt16(buf, 2).ShouldBe((short)0x0102);
    }

    [Fact]
    public void ReadUInt16_ByteArray_AtPosition_ReturnsCorrectValue()
    {
        var buf = new byte[] { 0x00, 0xCD, 0xAB };
        BinaryDigitReader.ReadUInt16(buf, 1).ShouldBe((ushort)0xABCD);
    }

    // ──────────────────────────────────────────
    //  byte[]-based — ReadInt32 / ReadUInt32
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt32_ByteArray_AtPosition_ReturnsCorrectValue()
    {
        var buf = new byte[] { 0x00, 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 at pos 1
        BinaryDigitReader.ReadInt32(buf, 1).ShouldBe(0x12345678);
    }

    [Fact]
    public void ReadUInt32_ByteArray_AtPosition_ReturnsCorrectValue()
    {
        var buf = new byte[] { 0x00, 0xEF, 0xBE, 0xAD, 0xDE }; // 0xDEADBEEF at pos 1
        BinaryDigitReader.ReadUInt32(buf, 1).ShouldBe(0xDEADBEEFU);
    }

    // ──────────────────────────────────────────
    //  byte[]-based — ReadInt64 / ReadUInt64
    // ──────────────────────────────────────────

    [Fact]
    public void ReadInt64_ByteArray_AtPosition_ReturnsCorrectValue()
    {
        var buf = new byte[9];
        buf[1] = 0x08; buf[2] = 0x07; buf[3] = 0x06; buf[4] = 0x05;
        buf[5] = 0x04; buf[6] = 0x03; buf[7] = 0x02; buf[8] = 0x01;
        BinaryDigitReader.ReadInt64(buf, 1).ShouldBe(0x0102030405060708L);
    }

    [Fact]
    public void ReadUInt64_ByteArray_AtPosition_ReturnsCorrectValue()
    {
        var buf = new byte[9];
        buf[1] = 0xFF; buf[2] = 0xFF; buf[3] = 0xFF; buf[4] = 0xFF;
        buf[5] = 0xFF; buf[6] = 0xFF; buf[7] = 0xFF; buf[8] = 0xFF;
        BinaryDigitReader.ReadUInt64(buf, 1).ShouldBe(ulong.MaxValue);
    }
}

/// <summary>
/// 测试 <see cref="BinaryDigitWriter"/>
/// 覆盖 Span-based 和 byte[]-based 写入方法
/// </summary>
[Trait("Bing.IO.Buffers", "BinaryDigitWriter")]
public class BinaryDigitWriterTests
{
    // ──────────────────────────────────────────
    //  Span-based 写入后读回 (roundtrip)
    // ──────────────────────────────────────────

    [Fact]
    public void Write_Short_Span_Roundtrip()
    {
        Span<byte> buf = new byte[2];
        BinaryDigitWriter.Write(buf, (short)0x1234);
        BinaryDigitReader.ReadInt16(buf).ShouldBe((short)0x1234);
    }

    [Fact]
    public void Write_UShort_Span_Roundtrip()
    {
        Span<byte> buf = new byte[2];
        BinaryDigitWriter.Write(buf, (ushort)0xABCD);
        BinaryDigitReader.ReadUInt16(buf).ShouldBe((ushort)0xABCD);
    }

    [Fact]
    public void Write_Int_Span_Roundtrip()
    {
        Span<byte> buf = new byte[4];
        BinaryDigitWriter.Write(buf, 0x12345678);
        BinaryDigitReader.ReadInt32(buf).ShouldBe(0x12345678);
    }

    [Fact]
    public void Write_UInt_Span_Roundtrip()
    {
        Span<byte> buf = new byte[4];
        BinaryDigitWriter.Write(buf, 0xDEADBEEFU);
        BinaryDigitReader.ReadUInt32(buf).ShouldBe(0xDEADBEEFU);
    }

    [Fact]
    public void Write_Long_Span_Roundtrip()
    {
        Span<byte> buf = new byte[8];
        BinaryDigitWriter.Write(buf, 0x0102030405060708L);
        BinaryDigitReader.ReadInt64(buf).ShouldBe(0x0102030405060708L);
    }

    [Fact]
    public void Write_ULong_Span_Roundtrip()
    {
        Span<byte> buf = new byte[8];
        BinaryDigitWriter.Write(buf, ulong.MaxValue);
        BinaryDigitReader.ReadUInt64(buf).ShouldBe(ulong.MaxValue);
    }

    [Fact]
    public void Write_Long_Span_Zero_Roundtrip()
    {
        Span<byte> buf = new byte[8];
        BinaryDigitWriter.Write(buf, 0L);
        BinaryDigitReader.ReadInt64(buf).ShouldBe(0L);
    }

    // ──────────────────────────────────────────
    //  byte[]-based 写入后读回 (roundtrip)
    // ──────────────────────────────────────────

    [Fact]
    public void Write_Short_ByteArray_Roundtrip()
    {
        var buf = new byte[4];
        BinaryDigitWriter.Write(buf, 1, (short)0x1234);
        BinaryDigitReader.ReadInt16(buf, 1).ShouldBe((short)0x1234);
    }

    [Fact]
    public void Write_UShort_ByteArray_Roundtrip()
    {
        var buf = new byte[4];
        BinaryDigitWriter.Write(buf, 1, (ushort)0xABCD);
        BinaryDigitReader.ReadUInt16(buf, 1).ShouldBe((ushort)0xABCD);
    }

    [Fact]
    public void Write_Int_ByteArray_Roundtrip()
    {
        var buf = new byte[6];
        BinaryDigitWriter.Write(buf, 2, 0x12345678);
        BinaryDigitReader.ReadInt32(buf, 2).ShouldBe(0x12345678);
    }

    [Fact]
    public void Write_UInt_ByteArray_Roundtrip()
    {
        var buf = new byte[6];
        BinaryDigitWriter.Write(buf, 1, 0xDEADBEEFU);
        BinaryDigitReader.ReadUInt32(buf, 1).ShouldBe(0xDEADBEEFU);
    }

    [Fact]
    public void Write_Long_ByteArray_Roundtrip()
    {
        var buf = new byte[10];
        BinaryDigitWriter.Write(buf, 1, 0x0102030405060708L);
        BinaryDigitReader.ReadInt64(buf, 1).ShouldBe(0x0102030405060708L);
    }

    [Fact]
    public void Write_ULong_ByteArray_Roundtrip()
    {
        var buf = new byte[9];
        BinaryDigitWriter.Write(buf, 0, ulong.MaxValue);
        BinaryDigitReader.ReadUInt64(buf, 0).ShouldBe(ulong.MaxValue);
    }

    [Fact]
    public void Write_Short_ByteArray_WritesToCorrectBytes()
    {
        var buf = new byte[4];
        BinaryDigitWriter.Write(buf, 0, (short)0x0201);
        // 小端序：低字节在前
        buf[0].ShouldBe((byte)0x01);
        buf[1].ShouldBe((byte)0x02);
    }

    [Fact]
    public void Write_Int_ByteArray_WritesToCorrectBytes()
    {
        var buf = new byte[4];
        BinaryDigitWriter.Write(buf, 0, 0x04030201);
        buf[0].ShouldBe((byte)0x01);
        buf[1].ShouldBe((byte)0x02);
        buf[2].ShouldBe((byte)0x03);
        buf[3].ShouldBe((byte)0x04);
    }
}

/// <summary>
/// 测试 <see cref="BinaryDigitSwapper"/>
/// 覆盖 Int16/UInt16/Int32/UInt32/Int64/UInt64 字节交换
/// </summary>
[Trait("Bing.IO.Buffers", "BinaryDigitSwapper")]
public class BinaryDigitSwapperTests
{
    // ──────────────────────────────────────────
    //  SwapInt16 / SwapUInt16
    // ──────────────────────────────────────────

    [Fact]
    public void SwapInt16_KnownValue_ReturnsSwapped()
    {
        // 0x0102 → 0x0201
        BinaryDigitSwapper.SwapInt16(0x0102).ShouldBe((short)0x0201);
    }

    [Fact]
    public void SwapUInt16_KnownValue_ReturnsSwapped()
    {
        BinaryDigitSwapper.SwapUInt16(0x0102).ShouldBe((ushort)0x0201);
    }

    [Fact]
    public void SwapInt16_Zero_ReturnsZero()
    {
        BinaryDigitSwapper.SwapInt16(0).ShouldBe((short)0);
    }

    [Fact]
    public void SwapInt16_SwapTwice_ReturnsOriginal()
    {
        short original = 0x1234;
        BinaryDigitSwapper.SwapInt16(BinaryDigitSwapper.SwapInt16(original)).ShouldBe(original);
    }

    [Fact]
    public void SwapUInt16_SwapTwice_ReturnsOriginal()
    {
        ushort original = 0xABCD;
        BinaryDigitSwapper.SwapUInt16(BinaryDigitSwapper.SwapUInt16(original)).ShouldBe(original);
    }

    // ──────────────────────────────────────────
    //  SwapInt32 / SwapUInt32
    // ──────────────────────────────────────────

    [Fact]
    public void SwapInt32_SwapTwice_ReturnsOriginal()
    {
        int original = 0x12345678;
        BinaryDigitSwapper.SwapInt32(BinaryDigitSwapper.SwapInt32(original)).ShouldBe(original);
    }

    [Fact]
    public void SwapUInt32_SwapTwice_ReturnsOriginal()
    {
        uint original = 0xDEADBEEFU;
        BinaryDigitSwapper.SwapUInt32(BinaryDigitSwapper.SwapUInt32(original)).ShouldBe(original);
    }

    [Fact]
    public void SwapInt32_Zero_ReturnsZero()
    {
        BinaryDigitSwapper.SwapInt32(0).ShouldBe(0);
    }

    [Fact]
    public void SwapUInt32_Zero_ReturnsZero()
    {
        BinaryDigitSwapper.SwapUInt32(0U).ShouldBe(0U);
    }

    // ──────────────────────────────────────────
    //  SwapInt64 / SwapUInt64
    // ──────────────────────────────────────────

    [Fact]
    public void SwapInt64_SwapTwice_ReturnsOriginal()
    {
        long original = 0x0102030405060708L;
        BinaryDigitSwapper.SwapInt64(BinaryDigitSwapper.SwapInt64(original)).ShouldBe(original);
    }

    [Fact]
    public void SwapUInt64_SwapTwice_ReturnsOriginal()
    {
        ulong original = 0xFEDCBA9876543210UL;
        BinaryDigitSwapper.SwapUInt64(BinaryDigitSwapper.SwapUInt64(original)).ShouldBe(original);
    }

    [Fact]
    public void SwapInt64_Zero_ReturnsZero()
    {
        BinaryDigitSwapper.SwapInt64(0L).ShouldBe(0L);
    }

    [Fact]
    public void SwapUInt64_Zero_ReturnsZero()
    {
        BinaryDigitSwapper.SwapUInt64(0UL).ShouldBe(0UL);
    }

    // ──────────────────────────────────────────
    //  综合：Int 与 UInt swap 结果一致性
    // ──────────────────────────────────────────

    [Fact]
    public void SwapInt16_And_SwapUInt16_ProduceSameBitPattern()
    {
        short sv = 0x1234;
        ushort uv = 0x1234;
        // 只要底层 bit 相同，swap 结果也应相同
        var swappedInt16 = (ushort)BinaryDigitSwapper.SwapInt16(sv);
        var swappedUInt16 = BinaryDigitSwapper.SwapUInt16(uv);
        swappedInt16.ShouldBe(swappedUInt16);
    }
}

/// <summary>
/// 测试 <see cref="FileStreamExtensions"/>
/// 覆盖 ReadAllLines
/// </summary>
[Trait("Bing.IO", "FileStreamExtensions")]
public class FileStreamExtensionsTests : IDisposable
{
    private readonly string _tempFile;

    public FileStreamExtensionsTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"BingTest_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void ReadAllLines_SingleLine_ReturnsSingleLine()
    {
        File.WriteAllText(_tempFile, "hello", Encoding.UTF8);
        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        var lines = fs.ReadAllLines(Encoding.UTF8, closeAfter: false);
        lines.ShouldHaveSingleItem();
        lines[0].ShouldBe("hello");
    }

    [Fact]
    public void ReadAllLines_MultipleLines_ReturnsAllLines()
    {
        File.WriteAllLines(_tempFile, new[] { "line1", "line2", "line3" }, Encoding.UTF8);
        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        var lines = fs.ReadAllLines(Encoding.UTF8, closeAfter: false);
        lines.Count.ShouldBe(3);
        lines[0].ShouldBe("line1");
        lines[1].ShouldBe("line2");
        lines[2].ShouldBe("line3");
    }

    [Fact]
    public void ReadAllLines_EmptyFile_ReturnsEmptyList()
    {
        File.WriteAllText(_tempFile, string.Empty, Encoding.UTF8);
        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        var lines = fs.ReadAllLines(Encoding.UTF8, closeAfter: false);
        lines.ShouldBeEmpty();
    }

    [Fact]
    public void ReadAllLines_WithCloseAfterTrue_ClosesStream()
    {
        File.WriteAllText(_tempFile, "test", Encoding.UTF8);
        var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        var lines = fs.ReadAllLines(Encoding.UTF8, closeAfter: true);
        lines.ShouldHaveSingleItem();
        // Stream should be closed; accessing it should throw
        Should.Throw<ObjectDisposedException>(() => { var _ = fs.Length; });
    }

    [Fact]
    public void ReadAllLines_ChineseContent_DecodesCorrectly()
    {
        const string content = "你好世界";
        File.WriteAllText(_tempFile, content, Encoding.UTF8);
        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        var lines = fs.ReadAllLines(Encoding.UTF8, closeAfter: false);
        lines.ShouldHaveSingleItem();
        lines[0].ShouldBe(content);
    }
}
