using System.Buffers;
using System.Linq;

namespace Bing.IO;

/// <summary>
/// 覆盖内存流审查中发现的数据完整性和缓冲区生命周期边界。
/// </summary>
[Trait("Bing.IO", "MemoryStreamReviewRegression")]
public class MemoryStreamReviewRegressionTest
{
    /// <summary>
    /// 验证 SetLength、Seek 和定位写入不会暴露池中的历史字节。
    /// </summary>
    [Fact]
    public void PooledMemoryStream_ClearsLengthAndPositionGaps()
    {
        var pool = new DirtyArrayPool();
        using var stream = new PooledMemoryStream(pool, capacity: 8);

        stream.SetLength(2);
        stream.ToArray().ShouldBe(new byte[2]);

        stream.Seek(5, SeekOrigin.Begin).ShouldBe(5);
        stream.ToArray().ShouldBe(new byte[5]);

        stream.Position = 7;
        stream.Write([0x7A], 0, 1);

        ReadFromStart(stream).ShouldBe([0, 0, 0, 0, 0, 0, 0, 0x7A]);
    }

    /// <summary>
    /// 验证当前位置不在末尾时扩展长度仍保留所有有效字节并清零新增区域。
    /// </summary>
    [Fact]
    public void PooledMemoryStream_GrowingAtEarlierPositionPreservesContent()
    {
        var pool = new DirtyArrayPool();
        using var stream = new PooledMemoryStream(pool, capacity: 8);
        byte[] original = [1, 2, 3, 4, 5, 6, 7, 8];
        stream.Write(original, 0, original.Length);
        stream.Position = 2;

        stream.SetLength(12);

        stream.Position.ShouldBe(2);
        ReadFromStart(stream).ShouldBe(original.Concat(new byte[4]).ToArray());
    }

    /// <summary>
    /// 验证 GetBuffer 和 ToArray 返回独立快照，不受后续写入或归还缓冲区影响。
    /// </summary>
    [Fact]
    public void PooledMemoryStream_BufferMethodsReturnIndependentSnapshots()
    {
        var pool = new DirtyArrayPool();
        var stream = new PooledMemoryStream(pool, capacity: 8);
        byte[] original = [1, 2, 3, 4, 5, 6, 7, 8];
        stream.Write(original, 0, original.Length);

        var bufferSnapshot = stream.GetBuffer();
        var arraySnapshot = stream.ToArray();
        bufferSnapshot.ShouldNotBeSameAs(arraySnapshot);

        stream.Position = 0;
        stream.Write([9, 8, 7], 0, 3);
        stream.Dispose();

        bufferSnapshot.ShouldBe(original);
        arraySnapshot.ShouldBe(original);
    }

    [Fact]
    public void PooledMemoryStream_TruncateAndRegrowClearsRemovedBytes()
    {
        using var stream = new PooledMemoryStream(new DirtyArrayPool(), 8);
        stream.Write([1, 2, 3, 4, 5, 6, 7, 8], 0, 8);
        stream.SetLength(2);
        stream.SetLength(6);
        stream.ToArray().ShouldBe([1, 2, 0, 0, 0, 0]);
        stream.Position = 30;
        stream.Read(new byte[1], 0, 1).ShouldBe(0);
    }

    /// <summary>
    /// 验证重复释放只归还一次池化缓冲区。
    /// </summary>
    [Fact]
    public void PooledMemoryStream_RepeatedDisposeReturnsBufferOnce()
    {
        var pool = new DirtyArrayPool();
        var stream = new PooledMemoryStream(pool, capacity: 8);

        stream.Dispose();
        stream.Dispose();

        pool.RentCount.ShouldBe(1);
        pool.ReturnCount.ShouldBe(1);
    }

    /// <summary>
    /// 验证相对流尾的负偏移定位到正确位置。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_SeekNegativeOffsetFromEndReadsLastByte()
    {
        var pool = new DirtyArrayPool();
        using var stream = new LargeMemoryStream(8, pool);
        byte[] content = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        stream.Write(content, 0, content.Length);

        stream.Seek(-1, SeekOrigin.End).ShouldBe(15);
        var lastByte = new byte[1];
        stream.Read(lastByte, 0, 1).ShouldBe(1);
        lastByte.ShouldBe([16]);
    }

    /// <summary>
    /// 验证写入和读取都能跨越小页边界。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_ReadAndWriteAcrossPageBoundary()
    {
        var pool = new DirtyArrayPool();
        using var stream = new LargeMemoryStream(8, pool);
        byte[] prefix = [1, 2, 3, 4, 5, 6];
        byte[] acrossBoundary = [10, 11, 12, 13];
        stream.Write(prefix, 0, prefix.Length);

        stream.Write(acrossBoundary, 0, acrossBoundary.Length);

        ReadFromStart(stream).ShouldBe(prefix.Concat(acrossBoundary).ToArray());
    }

    /// <summary>
    /// 验证流长度增长时，已有页面尾部和新租用页面中的脏字节都会清零。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_GrowingLengthClearsDirtyPages()
    {
        var pool = new DirtyArrayPool();
        using var stream = new LargeMemoryStream(8, pool);
        stream.SetLength(1);
        stream.Write([0x7A], 0, 1);

        stream.SetLength(12);

        ReadFromStart(stream).ShouldBe([0x7A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
    }

    /// <summary>
    /// 验证缩容归还超出部分页面，后续增长保留有效前缀并清零新区域。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_ShrinkReturnsPagesAndRegrowthPreservesPrefix()
    {
        var pool = new DirtyArrayPool();
        using var stream = new LargeMemoryStream(8, pool);
        byte[] original = Enumerable.Range(1, 25).Select(value => (byte)value).ToArray();
        stream.Write(original, 0, original.Length);

        stream.SetLength(9);
        pool.ReturnCount.ShouldBe(2);

        stream.SetLength(17);

        pool.RentCount.ShouldBe(5);
        var expected = original.Take(9).Concat(new byte[8]).ToArray();
        ReadFromStart(stream).ShouldBe(expected);
    }

    /// <summary>
    /// 验证清空后所有页面都归还，流仍可重新租页和写入。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_SetLengthZeroCanBeWrittenAgain()
    {
        var pool = new DirtyArrayPool();
        var stream = new LargeMemoryStream(8, pool);
        stream.Write(Enumerable.Range(1, 17).Select(value => (byte)value).ToArray(), 0, 17);

        stream.SetLength(0);
        pool.ReturnCount.ShouldBe(3);
        stream.Write([0x5A], 0, 1);

        ReadFromStart(stream).ShouldBe([0x5A]);
        stream.Dispose();
        pool.ReturnCount.ShouldBe(4);
    }

    /// <summary>
    /// 验证超过 1024 页后扩展的页表仍可访问末页。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_CanWriteBeyond1024Pages()
    {
        const int pageSize = 8;
        const int pageCount = 1025;
        var pool = new DirtyArrayPool();
        using var stream = new LargeMemoryStream(pageSize, pool);
        var length = pageSize * pageCount;
        stream.SetLength(length);

        stream.Position = length - 1;
        stream.Write([0x6B], 0, 1);
        stream.Position = length - 1;
        var lastByte = new byte[1];

        stream.Read(lastByte, 0, 1).ShouldBe(1);
        lastByte.ShouldBe([0x6B]);
        pool.RentCount.ShouldBe(pageCount);
    }

    /// <summary>
    /// 验证重复释放只归还每个已分配页面一次。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_RepeatedDisposeReturnsEachPageOnce()
    {
        var pool = new DirtyArrayPool();
        var stream = new LargeMemoryStream(8, pool);
        stream.Write(Enumerable.Range(1, 17).Select(value => (byte)value).ToArray(), 0, 17);

        stream.Dispose();
        stream.Dispose();

        pool.RentCount.ShouldBe(3);
        pool.ReturnCount.ShouldBe(3);
    }

    private static byte[] ReadFromStart(Stream stream)
    {
        stream.Position = 0;
        var content = new byte[(int)stream.Length];
        stream.Read(content, 0, content.Length).ShouldBe(content.Length);
        return content;
    }

    private sealed class DirtyArrayPool : ArrayPool<byte>
    {
        private const byte RentValue = 0xA5;
        private const byte ReturnValue = 0xCC;

        public int RentCount { get; private set; }

        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            var buffer = new byte[minimumLength];
            Array.Fill(buffer, RentValue);
            RentCount++;
            return buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (clearArray)
            {
                Array.Clear(array, 0, array.Length);
            }
            else
            {
                Array.Fill(array, ReturnValue);
            }

            ReturnCount++;
        }
    }
}
