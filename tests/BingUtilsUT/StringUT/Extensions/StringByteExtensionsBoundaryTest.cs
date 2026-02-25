#pragma warning disable SYSLIB0001
using System.Text;
using Bing.Text;

namespace BingUtilsUT.StringUT.Extensions;

/// <summary>
/// 测试类：覆盖字符串字节转换扩展的边界输入与编码契约。
/// </summary>
[Trait("StringUT.Extensions", "StringByte.Boundary")]
public class StringByteExtensionsBoundaryTest
{
    /// <summary>
    /// 测试用例：各编码扩展方法应与对应 Encoding.GetBytes 结果一致。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetEncodingConverters))]
    public void ToBytes_ExtensionMethods_ShouldMatchEncodingGetBytes(
        string input,
        Func<string, byte[]> converter,
        Encoding encoding)
    {
        var result = converter(input);

        result.ShouldBe(encoding.GetBytes(input));
    }

    /// <summary>
    /// 测试用例：各编码扩展方法在 null 输入时应抛出参数异常，且参数名为 value。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetConverterOnlyCases))]
    public void ToBytes_ExtensionMethods_NullInput_ThrowsArgumentNullException(
        Func<string, byte[]> converter)
    {
        var exception = Should.Throw<ArgumentNullException>(() => converter(null));

        exception.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// 测试用例：空字符串输入时，各编码扩展应返回空字节数组。
    /// </summary>
    [Theory]
    [MemberData(nameof(GetConverterOnlyCases))]
    public void ToBytes_ExtensionMethods_EmptyInput_ReturnsEmptyArray(
        Func<string, byte[]> converter)
    {
        var result = converter(string.Empty);

        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试用例：ASCII 编码遇到非 ASCII 字符时应替换为问号。
    /// </summary>
    [Fact]
    public void ToASCIIBytes_NonAsciiInput_ReplacesWithQuestionMarks()
    {
        var bytes = "你好".ToASCIIBytes();

        Encoding.ASCII.GetString(bytes).ShouldBe("??");
    }

    /// <summary>
    /// 测试用例：Utf8 快捷方法应与 ToBytes(UTF8) 一致。
    /// </summary>
    [Fact]
    public void ToUtf8Bytes_ShouldBeConsistentWithToBytesUtf8()
    {
        const string input = "Hello-中文";

        var byShortcut = input.ToUtf8Bytes();
        var byCore = input.ToBytes(Encoding.UTF8);

        byShortcut.ShouldBe(byCore);
        input.ShouldBe("Hello-中文");
    }

    public static IEnumerable<object[]> GetEncodingConverters()
    {
        const string unicodeInput = "Hello-中文";

        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToUtf8Bytes, Encoding.UTF8 };
        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToUtf7Bytes, Encoding.UTF7 };
        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToUtf32Bytes, Encoding.UTF32 };
        yield return new object[] { "Hello-ASCII", (Func<string, byte[]>)StringExtensions.ToASCIIBytes, Encoding.ASCII };
        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToBigEndianUnicodeBytes, Encoding.BigEndianUnicode };
        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToDefaultBytes, Encoding.Default };
        yield return new object[] { unicodeInput, (Func<string, byte[]>)StringExtensions.ToUnicodeBytes, Encoding.Unicode };
    }

    public static IEnumerable<object[]> GetConverterOnlyCases()
    {
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToUtf8Bytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToUtf7Bytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToUtf32Bytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToASCIIBytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToBigEndianUnicodeBytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToDefaultBytes };
        yield return new object[] { (Func<string, byte[]>)StringExtensions.ToUnicodeBytes };
    }
}
#pragma warning restore SYSLIB0001
