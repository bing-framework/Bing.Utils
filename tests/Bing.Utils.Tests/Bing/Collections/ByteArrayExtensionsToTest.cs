using Bing.Date;
using Bing.Tests;

namespace Bing.Collections;

/// <summary>
/// 字节数组 扩展 转换 测试
/// </summary>
[Trait("CollectionsUT", "ByteArrayExtensions.To")]
public class ByteArrayExtensionsToTest : TestBase
{
    /// <inheritdoc />
    public ByteArrayExtensionsToTest(ITestOutputHelper output) : base(output)
    {
    }

    #region ToInt

    /// <summary>
    /// 测试 - ToInt - 正确转换32位整数
    /// </summary>
    [Fact]
    public void ToInt_ReturnsCorrectInteger()
    {
        // 准备
        int expected = 12345678;
        byte[] bytes = BitConverter.GetBytes(expected);

        // 执行
        int result = bytes.ToInt();

        // 验证
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToInt - 指定起始索引正确转换
    /// </summary>
    [Fact]
    public void ToInt_WithStartIndex_ReturnsCorrectInteger()
    {
        // 准备
        int expected = 12345678;
        byte[] bytes = new byte[8];
        byte[] valueBytes = BitConverter.GetBytes(expected);

        // 将数据放在偏移位置
        Array.Copy(valueBytes, 0, bytes, 4, 4);

        // 执行
        int result = bytes.ToInt(4);

        // 验证
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToInt - 数组长度不足时返回0
    /// </summary>
    [Fact]
    public void ToInt_InsufficientBytes_ReturnsZero()
    {
        // 准备
        byte[] shortBytes = new byte[] { 1, 2, 3 }; // 少于4个字节

        // 执行
        int result = shortBytes.ToInt();

        // 验证
        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - ToInt - null输入抛出异常
    /// </summary>
    [Fact]
    public void ToInt_NullInput_ThrowsException()
    {
        // 准备
        byte[] nullBytes = null;

        // 执行 & 验证
        Should.Throw<ArgumentNullException>(() => nullBytes.ToInt());
    }

    /// <summary>
    /// 测试 - ToInt - 无效起始索引抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1)]     // 负值索引
    [InlineData(10)]     // 超出范围索引
    public void ToInt_InvalidStartIndex_ThrowsException(int invalidIndex)
    {
        // 准备
        byte[] bytes = new byte[4];

        // 执行 & 验证
        Should.Throw<ArgumentOutOfRangeException>(() => bytes.ToInt(invalidIndex));
    }

    /// <summary>
    /// 测试 - ToInt - 空数组返回0
    /// </summary>
    [Fact]
    public void ToInt_EmptyArray_ReturnsZero()
    {
        // 准备
        byte[] emptyBytes = new byte[0];

        // 执行
        int result = emptyBytes.ToInt();

        // 验证
        result.ShouldBe(0);
    }

    #endregion

    #region ToLong

    /// <summary>
    /// 测试 - ToLong - 正确转换64位整数
    /// </summary>
    [Fact]
    public void ToLong_ReturnsCorrectLong()
    {
        // 准备
        long expected = 1234567890123456789L;
        byte[] bytes = BitConverter.GetBytes(expected);

        // 执行
        long result = bytes.ToLong();

        // 验证
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToLong - 指定起始索引正确转换
    /// </summary>
    [Fact]
    public void ToLong_WithStartIndex_ReturnsCorrectLong()
    {
        // 准备
        long expected = 1234567890123456789L;
        byte[] bytes = new byte[16];
        byte[] valueBytes = BitConverter.GetBytes(expected);

        // 将数据放在偏移位置
        Array.Copy(valueBytes, 0, bytes, 8, 8);

        // 执行
        long result = bytes.ToLong(8);

        // 验证
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToLong - 数组长度不足时返回0
    /// </summary>
    [Fact]
    public void ToLong_InsufficientBytes_ReturnsZero()
    {
        // 准备
        byte[] shortBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7 }; // 少于8个字节

        // 执行
        long result = shortBytes.ToLong();

        // 验证
        result.ShouldBe(0);
    }

    /// <summary>
    /// 测试 - ToLong - null输入抛出异常
    /// </summary>
    [Fact]
    public void ToLong_NullInput_ThrowsException()
    {
        // 准备
        byte[] nullBytes = null;

        // 执行 & 验证
        Should.Throw<ArgumentNullException>(() => nullBytes.ToLong());
    }

    /// <summary>
    /// 测试 - ToLong - 无效起始索引抛出异常
    /// </summary>
    [Theory]
    [InlineData(-1)]     // 负值索引
    [InlineData(10)]     // 超出范围索引
    public void ToLong_InvalidStartIndex_ThrowsException(int invalidIndex)
    {
        // 准备
        byte[] bytes = new byte[8];

        // 执行 & 验证
        Should.Throw<ArgumentOutOfRangeException>(() => bytes.ToLong(invalidIndex));
    }

    /// <summary>
    /// 测试 - ToLong - 空数组返回0
    /// </summary>
    [Fact]
    public void ToLong_EmptyArray_ReturnsZero()
    {
        // 准备
        byte[] emptyBytes = new byte[0];

        // 执行
        long result = emptyBytes.ToLong();

        // 验证
        result.ShouldBe(0);
    }

    #endregion

    #region ToHexString

    /// <summary>
    /// 测试 - ToHexString - 正确转换为16进制字符串
    /// </summary>
    [Fact]
    public void ToHexString_ReturnsCorrectHexString()
    {
        // 准备
        byte[] bytes = { 0x12, 0xAB, 0x00, 0xFF };

        // 执行
        var result = bytes.ToHexString();

        // 验证
        result.ShouldBe("12 AB 00 FF");
    }

    /// <summary>
    /// 测试 - ToHexString - 空字节数组返回空字符串
    /// </summary>
    [Fact]
    public void ToHexString_EmptyArray_ReturnsEmptyString()
    {
        // 准备
        byte[] emptyBytes = new byte[0];

        // 执行
        var result = emptyBytes.ToHexString();

        // 验证
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试 - ToHexString - null输入抛出异常
    /// </summary>
    [Fact]
    public void ToHexString_NullInput_ThrowsException()
    {
        // 准备
        byte[] nullBytes = null;

        // 执行 & 验证
        Should.Throw<ArgumentNullException>(() => nullBytes.ToHexString());
    }

    #endregion

    #region ToBase64String

    /// <summary>
    /// 测试 - ToBase64String - 正确转换为Base64字符串
    /// </summary>
    [Fact]
    public void ToBase64String_ReturnsCorrectBase64String()
    {
        // 准备
        byte[] bytes = { 0x12, 0xAB, 0x34, 0xCD };
        string expected = Convert.ToBase64String(bytes);

        // 执行
        var result = bytes.ToBase64String();

        // 验证
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToBase64String - 空字节数组返回空Base64字符串
    /// </summary>
    [Fact]
    public void ToBase64String_EmptyArray_ReturnsEmptyBase64String()
    {
        // 准备
        byte[] emptyBytes = new byte[0];

        // 执行
        var result = emptyBytes.ToBase64String();

        // 验证
        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// 测试 - ToBase64String - null输入抛出异常
    /// </summary>
    [Fact]
    public void ToBase64String_NullInput_ThrowsException()
    {
        // 准备
        byte[] nullBytes = null;

        // 执行 & 验证
        Should.Throw<ArgumentNullException>(() => nullBytes.ToBase64String());
    }

    /// <summary>
    /// 测试 - ToBase64String - 解码后应与原字节数组一致
    /// </summary>
    [Fact]
    public void ToBase64String_DecodedValueMatchesOriginal()
    {
        // 准备
        byte[] original = { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

        // 执行
        string base64 = original.ToBase64String();
        byte[] decoded = Convert.FromBase64String(base64);

        // 验证
        decoded.ShouldBe(original);
    }

    #endregion

    #region ToDateTime

    /// <summary>
    /// 测试 - ToDateTime - 成功将字节数组转换为DateTime
    /// </summary>
    [Fact]
    public void ToDateTime_ReturnsCorrectDateTime()
    {
        // 准备
        var originalDate = new DateTime(2023, 1, 1, 12, 30, 45);
        var bytes = BitConverter.GetBytes(originalDate.ToBinary());

        // 执行
        var restoredDate = bytes.ToDateTime();

        // 验证
        restoredDate.ShouldBe(originalDate);
        restoredDate.Kind.ShouldBe(originalDate.Kind);
    }

    /// <summary>
    /// 测试 - ToDateTime - 最小日期值转换
    /// </summary>
    [Fact]
    public void ToDateTime_MinValue_ReturnsCorrectDateTime()
    {
        // 准备
        var originalDate = DateTime.MinValue;
        var bytes = BitConverter.GetBytes(originalDate.ToBinary());

        // 执行
        var restoredDate = bytes.ToDateTime();

        // 验证
        restoredDate.ShouldBe(originalDate);
    }

    /// <summary>
    /// 测试 - ToDateTime - 最大日期值转换
    /// </summary>
    [Fact]
    public void ToDateTime_MaxValue_ReturnsCorrectDateTime()
    {
        // 准备
        var originalDate = DateTime.MaxValue;
        var bytes = BitConverter.GetBytes(originalDate.ToBinary());

        // 执行
        var restoredDate = bytes.ToDateTime();

        // 验证
        restoredDate.ShouldBe(originalDate);
    }

    /// <summary>
    /// 测试 - ToDateTime - 不同Kind属性的日期时间转换
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ToDateTime_DifferentKinds_PreservesKind(DateTimeKind kind)
    {
        // 准备
        var originalDate = new DateTime(2023, 1, 1, 12, 30, 45, kind);
        var bytes = BitConverter.GetBytes(originalDate.ToBinary());

        // 执行
        var restoredDate = bytes.ToDateTime();

        // 验证
        restoredDate.ShouldBe(originalDate);
        restoredDate.Kind.ShouldBe(kind);
    }

    /// <summary>
    /// 测试 - ToDateTime - 使用非零起始索引
    /// </summary>
    [Fact]
    public void ToDateTime_WithNonZeroStartIndex()
    {
        // 准备
        var originalDate = new DateTime(2023, 1, 1, 12, 30, 45);
        var dateBytes = BitConverter.GetBytes(originalDate.ToBinary());

        // 创建一个更大的数组，并将日期字节放在偏移位置
        var offset = 5;
        var bytes = new byte[dateBytes.Length + offset];
        Array.Copy(dateBytes, 0, bytes, offset, dateBytes.Length);

        // 执行
        var restoredDate = bytes.ToDateTime(offset);

        // 验证
        restoredDate.ShouldBe(originalDate);
    }

    /// <summary>
    /// 测试 - ToDateTime - 参数验证异常
    /// </summary>
    [Fact]
    public void ToDateTime_InvalidArgs_ThrowsException()
    {
        // 准备 - null数组
        byte[] nullBytes = null;

        // 验证 - null数组应抛出ArgumentNullException
        Should.Throw<ArgumentNullException>(() => nullBytes.ToDateTime());

        // 准备 - 数组长度不足
        var shortBytes = new byte[4]; // 需要至少8个字节

        // 验证 - 数组长度不足应抛出ArgumentException
        Should.Throw<ArgumentException>(() => shortBytes.ToDateTime());

        // 准备 - 起始索引超出范围
        var bytes = new byte[8];

        // 验证 - 无效的起始索引应抛出ArgumentOutOfRangeException
        Should.Throw<ArgumentOutOfRangeException>(() => bytes.ToDateTime(9));
        Should.Throw<ArgumentOutOfRangeException>(() => bytes.ToDateTime(-1));
    }

    /// <summary>
    /// 测试 - ToDateTime - 与ToBytes方法的可逆性
    /// </summary>
    [Fact]
    public void ToDateTime_IsReversibleWithToBytes()
    {
        // 准备
        var originalDate = DateTime.Now;

        // 使用DateTimeExtensions.ToBytes()方法（如果可用）
        byte[] bytes;
        try
        {
            // 尝试使用DateTimeExtensions.ToBytes方法
            bytes = originalDate.ToBytes();
        }
        catch (MissingMethodException)
        {
            // 如果方法不可用，就使用BitConverter
            bytes = BitConverter.GetBytes(originalDate.ToBinary());
        }

        // 执行
        var restoredDate = bytes.ToDateTime();

        // 验证
        restoredDate.ShouldBe(originalDate);
        restoredDate.Kind.ShouldBe(originalDate.Kind);

        Output.WriteLine($"原始日期: {originalDate}");
        Output.WriteLine($"还原日期: {restoredDate}");
    }

    #endregion
}