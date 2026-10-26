using System.Buffers;

namespace Bing.IO;

/// <summary>
/// 验证大型内存流的默认页容量和页面资源生命周期。
/// </summary>
[Trait("Bing.IO", "LargeMemoryStream.Resources")]
public class LargeMemoryStreamResourceTest
{
    /// <summary>
    /// 验证默认页大小的首次写入只租用一个 64 KiB 页面。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_DefaultPageSizeFirstWrite_Rents64KiBPage()
    {
        var pool = new RecordingArrayPool();
        using var stream = new LargeMemoryStream(pool);

        stream.Write(new byte[] { 0x2A }, 0, 1);

        pool.RequestedLengths.ShouldBe(new[] { 64 * 1024 });
        stream.Length.ShouldBe(1);
    }

    /// <summary>
    /// 验证空流查询逻辑片段不会租用数据页面。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_EmptyInstance_DoesNotRentPages()
    {
        var pool = new RecordingArrayPool();
        using var stream = new LargeMemoryStream(pool);

        stream.Length.ShouldBe(0);
        stream.GetWrittenSegments().Count.ShouldBe(0);

        pool.RequestedLengths.Count.ShouldBe(0);
        pool.ReturnedArrays.Count.ShouldBe(0);
    }

    /// <summary>
    /// 验证页表从四个槽位开始按几何级数扩展。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_PageTable_GrowsGeometrically()
    {
        var pool = new RecordingArrayPool();
        using var stream = new LargeMemoryStream(1, pool);

        stream.GetSpan().Length.ShouldBe(0);
        stream.SetLength(1);
        stream.GetSpan().Length.ShouldBe(4);
        stream.SetLength(5);
        stream.GetSpan().Length.ShouldBe(8);
        stream.SetLength(9);

        stream.GetSpan().Length.ShouldBe(16);
        pool.RequestedLengths.Count.ShouldBe(9);
    }

    /// <summary>
    /// 验证页数超出整数范围时在租用页面前失败。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_PageCountOverflow_FailsBeforeRentingPages()
    {
        var pool = new RecordingArrayPool();
        using var stream = new LargeMemoryStream(1, pool);

        Should.Throw<OverflowException>(() => stream.SetLength((long)int.MaxValue + 1));

        stream.Length.ShouldBe(0);
        pool.RequestedLengths.Count.ShouldBe(0);
    }

    /// <summary>
    /// 验证页面租用中途失败后流可重试，且释放时每个页面只归还一次。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_RentFailsOnce_RetryAndRepeatedDisposeReturnPagesOnce()
    {
        var pool = new RecordingArrayPool(failOnRentAttempt: 2);
        var stream = new LargeMemoryStream(4, pool);

        Should.Throw<InvalidOperationException>(() => stream.SetLength(9));
        pool.RentedArrays.Count.ShouldBe(1);

        stream.SetLength(9);
        stream.Position = 0;
        byte[] expected = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        stream.Write(expected, 0, expected.Length);
        stream.Position = 0;
        var actual = new byte[expected.Length];
        stream.Read(actual, 0, actual.Length).ShouldBe(actual.Length);
        actual.ShouldBe(expected);

        stream.Dispose();
        stream.Dispose();

        pool.RentedArrays.Count.ShouldBe(3);
        pool.ReturnedArrays.Count.ShouldBe(3);
        pool.ReturnedArrays.ShouldBe(pool.RentedArrays);
    }

    /// <summary>
    /// 记录页面租用请求，并可在指定请求上模拟租用失败。
    /// </summary>
    private sealed class RecordingArrayPool : ArrayPool<byte>
    {
        /// <summary>
        /// 初始化页面记录池。
        /// </summary>
        /// <param name="failOnRentAttempt">需要模拟失败的租用序号；零表示不模拟失败。</param>
        public RecordingArrayPool(int failOnRentAttempt = 0)
        {
            _failOnRentAttempt = failOnRentAttempt;
        }

        /// <summary>
        /// 需要模拟失败的租用序号。
        /// </summary>
        private readonly int _failOnRentAttempt;

        /// <summary>
        /// 获取页面租用时请求的容量序列。
        /// </summary>
        public List<int> RequestedLengths { get; } = new();

        /// <summary>
        /// 获取成功租出的页面序列。
        /// </summary>
        public List<byte[]> RentedArrays { get; } = new();

        /// <summary>
        /// 获取已归还的页面序列。
        /// </summary>
        public List<byte[]> ReturnedArrays { get; } = new();

        /// <inheritdoc />
        public override byte[] Rent(int minimumLength)
        {
            RequestedLengths.Add(minimumLength);
            if (_failOnRentAttempt > 0 && RequestedLengths.Count == _failOnRentAttempt)
                throw new InvalidOperationException("模拟页面租用失败。");

            var array = new byte[minimumLength];
            RentedArrays.Add(array);
            return array;
        }

        /// <inheritdoc />
        public override void Return(byte[] array, bool clearArray = false)
        {
            if (clearArray)
                Array.Clear(array, 0, array.Length);
            ReturnedArrays.Add(array);
        }
    }
}
