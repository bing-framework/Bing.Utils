using System;
using System.Collections.Generic;
using System.Linq;

namespace Bing.IdUtils;

/// <summary>
/// 指定字符集安全随机标识生成器回归测试。
/// </summary>
public class SecureRandomIdTest
{
    /// <summary>
    /// 验证预置字母数字字符集包含完整且不重复的 62 个字符。
    /// </summary>
    [Fact]
    public void AlphaNumericAlphabet_ContainsUnique62Characters()
    {
        SecureRandomId.AlphaNumericAlphabet.Length.ShouldBe(62);
        SecureRandomId.AlphaNumericAlphabet.Distinct().Count().ShouldBe(62);
    }

    /// <summary>
    /// 验证新 API 对小于 Nonce 历史下限的正长度仍按请求精确返回。
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    public void Create_UsesExactPositiveLength(int length)
    {
        var result = SecureRandomId.Create(length, "ab");

        result.Length.ShouldBe(length);
        result.All(character => "ab".IndexOf(character) >= 0).ShouldBeTrue();
    }

    /// <summary>
    /// 验证字节索引逐一映射到完整 256 字符字符集。
    /// </summary>
    [Fact]
    public void CreateCore_MapsAll256ByteValues()
    {
        var alphabet = new string(Enumerable.Range(0, 256).Select(value => (char)value).ToArray());
        var random = new RecordingByteFiller(index => (byte)index);

        var result = SecureRandomId.CreateCore(256, alphabet, random.Fill);

        result.ShouldBe(alphabet);
        random.FillCallCount.ShouldBe(1);
        random.RequestedBufferLengths.ShouldBe(new[] { 256 });
    }

    /// <summary>
    /// 验证普通 Unicode BMP 字符可以作为字符集成员。
    /// </summary>
    [Fact]
    public void CreateCore_AcceptsUnicodeBmpCharacters()
    {
        var random = new RecordingByteFiller(index => (byte)(index % 2));

        var result = SecureRandomId.CreateCore(3, "中é", random.Fill);

        result.ShouldBe("中é中");
    }

    /// <summary>
    /// 验证字符集长度不能整除 256 时拒绝尾部字节并保持均匀映射。
    /// </summary>
    [Fact]
    public void CreateCore_RejectsOutOfRangeByteValues()
    {
        var random = new RecordingByteFiller(index => index switch
        {
            0 => byte.MaxValue,
            1 => (byte)0,
            2 => (byte)1,
            _ => (byte)2,
        });

        var result = SecureRandomId.CreateCore(3, "abc", random.Fill);

        result.ShouldBe("abc");
        random.FillCallCount.ShouldBe(1);
        random.RequestedBufferLengths.ShouldBe(new[] { 256 });
    }

    /// <summary>
    /// 验证长度跨越多个随机块时重复使用固定大小缓冲且返回精确长度。
    /// </summary>
    [Fact]
    public void CreateCore_ReusesFixedSizeRandomBufferAcrossChunks()
    {
        const int length = 513;
        var alphabet = new string(Enumerable.Range(0, 256).Select(value => (char)value).ToArray());
        var random = new RecordingByteFiller(index => (byte)(index % 256));
        var expected = new string(Enumerable.Range(0, length).Select(value => alphabet[value % 256]).ToArray());

        var result = SecureRandomId.CreateCore(length, alphabet, random.Fill);

        result.ShouldBe(expected);
        random.FillCallCount.ShouldBe(3);
        random.RequestedBufferLengths.ShouldBe(new[] { 256, 256, 256 });
    }

    /// <summary>
    /// 验证长度必须大于零。
    /// </summary>
    /// <param name="length">无效长度。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveLength(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandomId.Create(length, "ab"));
    }

    /// <summary>
    /// 验证字符集必须非空且不包含重复或代理字符。
    /// </summary>
    /// <param name="alphabet">无效字符集。</param>
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aabc")]
    [InlineData("\uD800a")]
    [InlineData("\uDC00a")]
    [InlineData("\uD83D\uDE00")]
    public void Create_RejectsInvalidAlphabet(string alphabet)
    {
        Assert.Throws<ArgumentException>(() => SecureRandomId.Create(1, alphabet));
    }

    /// <summary>
    /// 验证字符集不能为 null。
    /// </summary>
    [Fact]
    public void Create_RejectsNullAlphabet()
    {
        Assert.Throws<ArgumentNullException>(() => SecureRandomId.Create(1, null));
    }

    /// <summary>
    /// 验证字符集最大长度为 256 个 UTF-16 字符。
    /// </summary>
    [Fact]
    public void Create_RejectsAlphabetLongerThan256Characters()
    {
        var alphabet = new string(Enumerable.Range(0, 257).Select(value => (char)value).ToArray());

        Assert.Throws<ArgumentException>(() => SecureRandomId.Create(1, alphabet));
    }

    /// <summary>
    /// 验证无效输入或空随机填充委托不会开始生成。
    /// </summary>
    [Fact]
    public void CreateCore_RejectsMissingRandomFiller()
    {
        Assert.Throws<ArgumentNullException>(() => SecureRandomId.CreateCore(1, "ab", null));
    }

    private sealed class RecordingByteFiller
    {
        /// <summary>
        /// 根据随机字节在全局序列中的索引计算测试字节值。
        /// </summary>
        private readonly Func<int, byte> _valueAt;

        /// <summary>
        /// 下一个待生成字节在全局序列中的位置。
        /// </summary>
        private int _byteIndex;

        /// <summary>
        /// 使用确定性的字节计算函数初始化填充器。
        /// </summary>
        /// <param name="valueAt">按全局索引返回字节值的函数。</param>
        public RecordingByteFiller(Func<int, byte> valueAt) => _valueAt = valueAt;

        /// <summary>
        /// 获取调用随机填充委托的次数。
        /// </summary>
        public int FillCallCount { get; private set; }

        /// <summary>
        /// 获取每次调用传入的缓冲区长度。
        /// </summary>
        public List<int> RequestedBufferLengths { get; } = new List<int>();

        /// <summary>
        /// 按确定性序列填充指定缓冲区。
        /// </summary>
        /// <param name="buffer">待填充的随机字节缓冲区。</param>
        public void Fill(byte[] buffer)
        {
            FillCallCount++;
            RequestedBufferLengths.Add(buffer.Length);
            for (var index = 0; index < buffer.Length; index++)
                buffer[index] = _valueAt(_byteIndex++);
        }
    }
}
