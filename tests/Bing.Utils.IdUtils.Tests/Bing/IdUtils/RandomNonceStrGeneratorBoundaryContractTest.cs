using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// 测试类：覆盖 <see cref="RandomNonceStrGenerator"/> 的边界与契约行为。
/// </summary>
[Trait("IdUtilsUT", "RandomNonceStrGenerator.Boundary")]
public class RandomNonceStrGeneratorBoundaryContractTest
{
    /// <summary>
    /// 测试用例：Create() 无参重载应返回 16 位字母数字串。
    /// </summary>
    [Fact]
    public void Create_DefaultOverload_Returns16AlphaNumeric()
    {
        var result = RandomNonceStrGenerator.Create();

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(forceToAvoidRepetition) 重载应保持默认长度 16。
    /// </summary>
    /// <param name="forceToAvoidRepetition">是否强制避免生成重复字符。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_ForceAvoidRepetitionOverload_Returns16Length(bool forceToAvoidRepetition)
    {
        var result = RandomNonceStrGenerator.Create(forceToAvoidRepetition);

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(length) 在 length 大于 16 时应返回指定长度。
    /// </summary>
    /// <param name="length">大于最小长度的目标长度。</param>
    [Theory]
    [InlineData(17)]
    [InlineData(32)]
    [InlineData(64)]
    public void Create_LengthGreaterThan16_ReturnsExactLength(int length)
    {
        var result = RandomNonceStrGenerator.Create(length);

        result.Length.ShouldBe(length);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 测试用例：Create(length) 在 length 小于等于 16（含负数）时应回退为 16 位。
    /// </summary>
    /// <param name="length">小于或等于最小长度的输入值。</param>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    public void Create_LengthLessOrEqual16_ReturnsMinimum16(int length)
    {
        var result = RandomNonceStrGenerator.Create(length);

        result.Length.ShouldBe(16);
        result.ShouldMatch("^[a-zA-Z0-9]+$");
    }

    /// <summary>
    /// 验证索引映射覆盖完整的 62 个字符，包括数字 9。
    /// </summary>
    [Fact]
    public void CreateCore_MapsEveryCharacterIndex()
    {
        using var random = new SequenceRandom(Enumerable.Range(0, 62).Select(i => (byte)i));
        var result = RandomNonceStrGenerator.CreateCore(62, random);
        result.ShouldBe("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
    }

    [Fact]
    public void CreateCore_RejectsBiasedTailAndMapsAcceptedRangeEvenly()
    {
        using var random = new SequenceRandom(Enumerable.Range(248, 8).Concat(Enumerable.Range(0, 248)).Select(i => (byte)i));
        var result = RandomNonceStrGenerator.CreateCore(248, random);
        result.Distinct().Count().ShouldBe(62);
        result.GroupBy(c => c).Select(group => group.Count()).ShouldAllBe(count => count == 4);
        random.FillCallCount.ShouldBe(2);
        random.RequestedLengths.ShouldBe(new[] { 248, 8 });
        random.ConsumedBytes.ShouldBe(256);
    }

    /// <summary>
    /// 验证超过单个随机缓冲块的结果按剩余字符数继续填充。
    /// </summary>
    [Fact]
    public void CreateCore_FillsAcrossBoundedChunksWithoutOverreading()
    {
        const int length = 513;
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var random = new SequenceRandom(Enumerable.Range(0, length).Select(i => (byte)(i % alphabet.Length)));

        var result = RandomNonceStrGenerator.CreateCore(length, random);
        var expected = new string(Enumerable.Range(0, length).Select(i => alphabet[i % alphabet.Length]).ToArray());

        result.ShouldBe(expected);
        random.FillCallCount.ShouldBe(3);
        random.RequestedLengths.ShouldBe(new[] { 256, 256, 1 });
        random.ConsumedBytes.ShouldBe(length);
    }

    private sealed class SequenceRandom : RandomNumberGenerator
    {
        private readonly Queue<byte> _bytes;
        public SequenceRandom(IEnumerable<byte> bytes) => _bytes = new Queue<byte>(bytes);
        public int FillCallCount { get; private set; }
        public int ConsumedBytes { get; private set; }
        public List<int> RequestedLengths { get; } = new();

        public override void GetBytes(byte[] data) => GetBytes(data, 0, data.Length);

        public override void GetBytes(byte[] data, int offset, int count)
        {
            FillCallCount++;
            RequestedLengths.Add(count);
            for (var i = 0; i < count; i++)
            {
                data[offset + i] = _bytes.Dequeue();
                ConsumedBytes++;
            }
        }
    }
}
