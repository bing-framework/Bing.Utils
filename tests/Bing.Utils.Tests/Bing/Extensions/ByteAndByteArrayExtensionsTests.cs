using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="ByteExtensions"/> + <see cref="ByteArrayExtensions"/> 单元测试
/// </summary>
public class ByteAndByteArrayExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // ByteExtensions — Min / Max
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Byte_Max_ReturnsLarger() => ((byte)10).Max(20).ShouldBe((byte)20);

    [Fact]
    public void Byte_Max_SameValue_ReturnsSame() => ((byte)5).Max(5).ShouldBe((byte)5);

    [Fact]
    public void Byte_Min_ReturnsSmaller() => ((byte)10).Min(20).ShouldBe((byte)10);

    [Fact]
    public void Byte_Min_SameValue_ReturnsSame() => ((byte)5).Min(5).ShouldBe((byte)5);

    // ─────────────────────────────────────────────────────────────────
    // ByteExtensions — Resize
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Byte_Resize_Larger_PadsWithZero()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var resized = bytes.Resize(5);
        resized.Length.ShouldBe(5);
        resized[3].ShouldBe((byte)0);
        resized[4].ShouldBe((byte)0);
    }

    [Fact]
    public void Byte_Resize_Smaller_TruncatesArray()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var resized = bytes.Resize(3);
        resized.Length.ShouldBe(3);
        resized[2].ShouldBe((byte)3);
    }

    // ─────────────────────────────────────────────────────────────────
    // ByteExtensions — ToBase64String
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Byte_ToBase64String_RoundTrip()
    {
        var bytes = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
        var b64 = bytes.ToBase64String();
        Convert.FromBase64String(b64).ShouldBe(bytes);
    }

    [Fact]
    public void Byte_ToBase64String_WithOffset()
    {
        var bytes = new byte[] { 72, 101, 108 };
        var expected = Convert.ToBase64String(bytes, 0, 2);
        bytes.ToBase64String(0, 2).ShouldBe(expected);
    }

    // ─────────────────────────────────────────────────────────────────
    // ByteExtensions — GetString (encoding)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Byte_GetStringByUtf8_ReturnsCorrectString()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello");
        bytes.GetStringByUtf8().ShouldBe("Hello");
    }

    [Fact]
    public void Byte_GetStringByUnicode_ReturnsCorrectString()
    {
        var bytes = Encoding.Unicode.GetBytes("Test");
        bytes.GetStringByUnicode().ShouldBe("Test");
    }

    // ─────────────────────────────────────────────────────────────────
    // ByteArrayExtensions — ToInt
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ByteArray_ToInt_ValidBytes_ReturnsCorrectInt()
    {
        var bytes = BitConverter.GetBytes(42);
        bytes.ToInt().ShouldBe(42);
    }

    [Fact]
    public void ByteArray_ToInt_EmptyArray_ReturnsZero() =>
        new byte[0].ToInt().ShouldBe(0);

    [Fact]
    public void ByteArray_ToInt_NullArray_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((byte[])null!).ToInt());

    [Fact]
    public void ByteArray_ToInt_InvalidStartIndex_ThrowsArgumentOutOfRangeException() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new byte[] { 1, 2 }.ToInt(-1));

    // ─────────────────────────────────────────────────────────────────
    // ByteArrayExtensions — ToLong
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ByteArray_ToLong_ValidBytes_ReturnsCorrectLong()
    {
        var bytes = BitConverter.GetBytes(1234567890123L);
        bytes.ToLong().ShouldBe(1234567890123L);
    }

    [Fact]
    public void ByteArray_ToLong_EmptyArray_ReturnsZero() =>
        new byte[0].ToLong().ShouldBe(0L);

    [Fact]
    public void ByteArray_ToLong_NullArray_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((byte[])null!).ToLong());

    // ─────────────────────────────────────────────────────────────────
    // ByteArrayExtensions — ToHexString
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ByteArray_ToHexString_ValidBytes_ReturnsUpperHex()
    {
        var bytes = new byte[] { 0x0A, 0xFF, 0x1B };
        bytes.ToHexString().ShouldBe("0A FF 1B");
    }

    [Fact]
    public void ByteArray_ToHexString_EmptyArray_ReturnsEmpty() =>
        new byte[0].ToHexString().ShouldBe(string.Empty);

    [Fact]
    public void ByteArray_ToHexString_NullArray_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((byte[])null!).ToHexString());

    [Fact]
    public void ByteArray_ToHexString_SingleByte_NoSpaces() =>
        new byte[] { 0xAB }.ToHexString().ShouldBe("AB");

    // ─────────────────────────────────────────────────────────────────
    // ByteArrayExtensions — ToBase64String
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ByteArray_ToBase64String_ValidBytes_ReturnsBase64()
    {
        var bytes = new byte[] { 72, 101, 108, 108, 111 };
        bytes.ToBase64String().ShouldBe(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void ByteArray_ToBase64String_NullArray_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((byte[])null!).ToBase64String());

    // ─────────────────────────────────────────────────────────────────
    // ByteArrayExtensions — ToDateTime / Copy
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ByteArray_ToDateTime_RoundTrip()
    {
        var original = new DateTime(2024, 6, 15, 12, 0, 0);
        var bytes = BitConverter.GetBytes(original.ToBinary());
        var restored = bytes.ToDateTime();
        restored.ShouldBe(original);
    }

    [Fact]
    public void ByteArray_Copy_2D_IsDeepCopy()
    {
        var original = new byte[2, 3] { { 1, 2, 3 }, { 4, 5, 6 } };
        var copy = original.Copy();
        copy[0, 0] = 99;
        original[0, 0].ShouldBe((byte)1); // original unchanged
    }

    [Fact]
    public void ByteArray_Copy_NullArray_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ((byte[,])null!).Copy());
}
