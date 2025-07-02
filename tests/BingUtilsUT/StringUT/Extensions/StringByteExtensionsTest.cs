using Bing.Text;

namespace BingUtilsUT.StringUT.Extensions;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展 - 字节数组转换
/// </summary>
[Trait("StringUT.Extensions", "StringByte")]
public class StringByteExtensionsTest
{
    /// <summary>
    /// 测试字符串 - 在所有测试方法中使用的测试字符串
    /// </summary>
    private readonly string _testString = "Hello World! 你好世界！";

    /// <summary>
    /// 测试 - 将字符串转换为 UTF-8 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToUtf8Bytes()
    {
        // 执行转换
        var bytes = _testString.ToUtf8Bytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.UTF8.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.UTF8));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToUtf8Bytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为 UTF-7 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToUtf7Bytes()
    {
        // 执行转换
        var bytes = _testString.ToUtf7Bytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.UTF7.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.UTF7));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToUtf7Bytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为 UTF-32 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToUtf32Bytes()
    {
        // 执行转换
        var bytes = _testString.ToUtf32Bytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.UTF32.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.UTF32));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToUtf32Bytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为 ASCII 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToASCIIBytes()
    {
        // ASCII 只能正确表示英文字符，中文字符会被转换为问号(?)
        string asciiTestString = "Hello World!";

        // 执行转换
        var bytes = asciiTestString.ToASCIIBytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.ASCII.GetString(bytes).ShouldBe(asciiTestString);

        // 验证与基本方法一致
        bytes.ShouldBe(asciiTestString.ToBytes(Encoding.ASCII));

        // 测试非 ASCII 字符的转换
        string nonAsciiString = "你好世界";
        var nonAsciiBytes = nonAsciiString.ToASCIIBytes();
        Encoding.ASCII.GetString(nonAsciiBytes).ShouldBe("????"); // 每个中文字符会被转换为问号

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToASCIIBytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为 BigEndianUnicode 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToBigEndianUnicodeBytes()
    {
        // 执行转换
        var bytes = _testString.ToBigEndianUnicodeBytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.BigEndianUnicode.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.BigEndianUnicode));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToBigEndianUnicodeBytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为系统默认编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToDefaultBytes()
    {
        // 执行转换
        var bytes = _testString.ToDefaultBytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.Default.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.Default));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToDefaultBytes());
    }

    /// <summary>
    /// 测试 - 将字符串转换为 Unicode 编码的字节数组
    /// </summary>
    [Fact]
    public void Test_ToUnicodeBytes()
    {
        // 执行转换
        var bytes = _testString.ToUnicodeBytes();

        // 验证结果
        bytes.ShouldNotBeNull();
        Encoding.Unicode.GetString(bytes).ShouldBe(_testString);

        // 验证与基本方法一致
        bytes.ShouldBe(_testString.ToBytes(Encoding.Unicode));

        // 验证异常处理
        Should.Throw<ArgumentNullException>(() => ((string)null).ToUnicodeBytes());
    }

    /// <summary>
    /// 测试 - 将不同编码方式转换的字节数组长度进行比较
    /// </summary>
    [Fact]
    public void Test_DifferentEncodings_ByteLength()
    {
        string testStr = "Hello 你好";

        // 获取不同编码的字节数组
        var utf8Bytes = testStr.ToUtf8Bytes();
        var utf7Bytes = testStr.ToUtf7Bytes();
        var utf32Bytes = testStr.ToUtf32Bytes();
        var asciiBytes = testStr.ToASCIIBytes();
        var unicodeBytes = testStr.ToUnicodeBytes();
        var bigEndianBytes = testStr.ToBigEndianUnicodeBytes();

        // UTF-8: 英文1字节，中文3字节
        // UTF-16 (Unicode): 所有字符2字节
        // UTF-32: 所有字符4字节
        // ASCII: 只能表示英文，1字节/字符

        // 验证字节长度
        utf8Bytes.Length.ShouldBe(12);       // "Hello " 6个字符(6字节) + "你好" 2个字符(6字节) = 12字节
        unicodeBytes.Length.ShouldBe(16);    // 8个字符 * 2字节/字符 = 16字节
        utf32Bytes.Length.ShouldBe(32);      // 8个字符 * 4字节/字符 = 32字节
        asciiBytes.Length.ShouldBe(8);       // 8个字符 * 1字节/字符 = 8字节 (中文变为?)
        bigEndianBytes.Length.ShouldBe(16);  // 与Unicode相同，但字节顺序不同
    }

    /// <summary>
    /// 测试 - 编码和解码的一致性
    /// </summary>
    [Fact]
    public void Test_EncodingDecodingConsistency()
    {
        var encodings = new[]
        {
            Encoding.UTF8,
            Encoding.UTF7,
            Encoding.UTF32,
            Encoding.Unicode,
            Encoding.BigEndianUnicode
        };

        foreach (var encoding in encodings)
        {
            // 编码
            var bytes = _testString.ToBytes(encoding);

            // 解码
            var decodedString = encoding.GetString(bytes);

            // 验证一致性
            decodedString.ShouldBe(_testString);
        }
    }
}