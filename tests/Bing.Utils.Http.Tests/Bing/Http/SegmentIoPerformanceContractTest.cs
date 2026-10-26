using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证分片文件 I/O 的性能结构与失败时资源契约。
/// </summary>
public class SegmentIoPerformanceContractTest
{
    /// <summary>
    /// 同名目录不能充当已完成分片，仍按缺失分片报告并保留旧目标。
    /// </summary>
    [Fact]
    public async Task Merge_DirectoryInsteadOfPartKeepsMissingFileContract()
    {
        var root = CreateDirectory();
        try
        {
            var staging = Path.Combine(root, "parts");
            var target = Path.Combine(root, "target.bin");
            var store = new HttpDownloadSegmentFileStore(staging, target);
            var state = await CreateSmallState(store);
            var partPath = GetPartPath(staging, state.Segments[0].Range);
            File.Delete(partPath);
            Directory.CreateDirectory(partPath);
            File.WriteAllText(target, "original", Encoding.UTF8);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.MergeAsync(state));

            Assert.Equal("original", File.ReadAllText(target, Encoding.UTF8));
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
            Assert.True(File.Exists(GetPartPath(staging, state.Segments[1].Range)));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// 大分片按 1 MiB 级块复制，结果字节和 SHA-256 均与输入一致。
    /// </summary>
    [Fact]
    public async Task WriteSegment_LargePayloadUsesBoundedReadCallsAndExactHash()
    {
        var root = CreateDirectory();
        try
        {
            const int length = 8 * 1024 * 1024;
            var payload = CreatePayload(length);
            using var source = new CountingNonSeekableStream(payload, int.MaxValue);
            var staging = Path.Combine(root, "parts");
            var target = Path.Combine(root, "target.bin");
            var store = new HttpDownloadSegmentFileStore(staging, target);
            var range = new HttpDownloadRange(0, length - 1);

            var segment = await store.WriteSegmentAsync(range, source);

            Assert.True(source.CanRead);
            Assert.InRange(source.ReadCalls, 1, 10);
            Assert.Equal(length, segment.BytesWritten);
            Assert.True(segment.Completed);
            Assert.Equal(Hash(payload), segment.Sha256);
            var state = CreateState(segment);
            await store.MergeAsync(state);
            Assert.Equal(payload, File.ReadAllBytes(target));
            Assert.False(File.Exists(GetPartPath(staging, range)));

            await Assert.ThrowsAsync<InvalidDataException>(() => store.MergeAsync(state));
            Assert.Equal(payload, File.ReadAllBytes(target));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// 短读且不可定位的输入流可完整写入，文件存储不会关闭调用方流。
    /// </summary>
    [Fact]
    public async Task WriteSegment_ShortNonSeekableInputRemainsOpen()
    {
        var root = CreateDirectory();
        try
        {
            var payload = Encoding.UTF8.GetBytes("short reads across several calls");
            using var source = new CountingNonSeekableStream(payload, 2);
            var staging = Path.Combine(root, "parts");
            var range = new HttpDownloadRange(0, payload.Length - 1);
            var store = new HttpDownloadSegmentFileStore(staging, Path.Combine(root, "target.bin"));

            var segment = await store.WriteSegmentAsync(range, source);

            Assert.True(source.ReadCalls > 2);
            Assert.True(source.CanRead);
            Assert.Equal(Hash(payload), segment.Sha256);
            Assert.Equal(payload, File.ReadAllBytes(GetPartPath(staging, range)));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// 实际读取中发生异常或取消时保留先前完整分片并删除暂存文件。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteSegment_ReadFailureOrCancellationKeepsPreviousPart(bool cancel)
    {
        var root = CreateDirectory();
        try
        {
            const int length = 2 * 1024 * 1024;
            var previousBytes = Enumerable.Repeat((byte)0x5A, length).ToArray();
            var replacementBytes = CreatePayload(length);
            var staging = Path.Combine(root, "parts");
            var range = new HttpDownloadRange(0, length - 1);
            var store = new HttpDownloadSegmentFileStore(staging, Path.Combine(root, "target.bin"));
            using (var previousSource = new MemoryStream(previousBytes))
                await store.WriteSegmentAsync(range, previousSource);
            using var cancellation = new CancellationTokenSource();
            using var source = new FaultAfterFirstChunkStream(replacementBytes,
                cancel ? (Action)cancellation.Cancel : null);

            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.WriteSegmentAsync(range, source, cancellation.Token));
            else
                await Assert.ThrowsAsync<IOException>(() => store.WriteSegmentAsync(range, source));

            Assert.Equal(2, source.ReadCalls);
            Assert.True(source.CanRead);
            Assert.Equal(previousBytes, File.ReadAllBytes(GetPartPath(staging, range)));
            Assert.Empty(Directory.GetFiles(staging, "*.tmp"));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// 最终校验失败或校验期间取消时保留目标文件及全部分片。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Merge_ValidatorFailureOrCancellationPreservesFiles(bool cancel)
    {
        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "target.bin");
            var staging = Path.Combine(root, "parts");
            var original = Encoding.UTF8.GetBytes("old target");
            File.WriteAllBytes(target, original);
            var store = new HttpDownloadSegmentFileStore(staging, target);
            var state = await CreateSmallState(store);
            using var cancellation = new CancellationTokenSource();
            Stream validationStream = null;
            Func<Stream, CancellationToken, Task> validator = (stream, _) =>
            {
                validationStream = stream;
                if (cancel)
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                }
                return Task.FromException(new IOException("validation failed"));
            };

            if (cancel)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.MergeAsync(state, validator, cancellation.Token));
            else
                await Assert.ThrowsAsync<IOException>(() => store.MergeAsync(state, validator));

            Assert.NotNull(validationStream);
            Assert.False(validationStream.CanRead);
            Assert.Equal(original, File.ReadAllBytes(target));
            Assert.Equal(2, Directory.GetFiles(staging, "*.part").Length);
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// Windows 拒绝替换被占用的目标时保留分片，释放占用后可重试合并。
    /// </summary>
    [Fact]
    public async Task Merge_LockedTargetCanBeRetried()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var root = CreateDirectory();
        try
        {
            var target = Path.Combine(root, "target.bin");
            var staging = Path.Combine(root, "parts");
            File.WriteAllText(target, "old", Encoding.UTF8);
            var store = new HttpDownloadSegmentFileStore(staging, target);
            var state = await CreateSmallState(store);
            Exception commitError = null;
            using (new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try { await store.MergeAsync(state); }
                catch (Exception exception) { commitError = exception; }
                Assert.IsAssignableFrom<IOException>(commitError);
                Assert.Equal("old", File.ReadAllText(target, Encoding.UTF8));
                Assert.Equal(2, Directory.GetFiles(staging, "*.part").Length);
                Assert.Empty(Directory.GetFiles(root, "*.tmp"));
            }

            await store.MergeAsync(state);

            Assert.Equal("abcdefgh", File.ReadAllText(target, Encoding.UTF8));
            Assert.False(Directory.Exists(staging));
        }
        finally { DeleteDirectory(root); }
    }

    /// <summary>
    /// 创建包含两个连续小分片的状态。
    /// </summary>
    private static async Task<HttpDownloadSegmentsState> CreateSmallState(HttpDownloadSegmentFileStore store)
    {
        HttpDownloadSegmentState first;
        using (var firstContent = new MemoryStream(Encoding.UTF8.GetBytes("abcd")))
            first = await store.WriteSegmentAsync(new HttpDownloadRange(0, 3), firstContent);
        HttpDownloadSegmentState second;
        using (var secondContent = new MemoryStream(Encoding.UTF8.GetBytes("efgh")))
            second = await store.WriteSegmentAsync(new HttpDownloadRange(4, 7), secondContent);
        return CreateState(first, second);
    }

    /// <summary>
    /// 创建符合状态格式的连续分片清单。
    /// </summary>
    /// <param name="segments">按范围顺序排列且已完成的分片状态。</param>
    /// <returns>覆盖从零开始正文的完整分片清单。</returns>
    private static HttpDownloadSegmentsState CreateState(params HttpDownloadSegmentState[] segments)
        => new HttpDownloadSegmentsState
        {
            SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
            TotalLength = segments[segments.Length - 1].End + 1,
            Segments = segments.ToList()
        };

    /// <summary>
    /// 生成固定模式的校验数据，避免性能结构测试受随机数据生成影响。
    /// </summary>
    /// <param name="length">要生成的字节数。</param>
    /// <returns>按固定模式填充的数据。</returns>
    private static byte[] CreatePayload(int length)
    {
        var payload = new byte[length];
        for (var index = 0; index < payload.Length; index++)
            payload[index] = unchecked((byte)(index * 31 + index / 251));
        return payload;
    }

    /// <summary>
    /// 计算小写 SHA-256 十六进制摘要。
    /// </summary>
    /// <param name="bytes">参与校验的数据。</param>
    /// <returns>小写 SHA-256 十六进制字符串。</returns>
    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// 根据指定范围得到磁盘分片路径。
    /// </summary>
    /// <param name="staging">分片暂存目录。</param>
    /// <param name="range">分片范围。</param>
    /// <returns>对应的分片文件路径。</returns>
    private static string GetPartPath(string staging, HttpDownloadRange range)
        => Path.Combine(staging, "segment-" + range.Start + "-" + range.End + ".part");

    /// <summary>
    /// 创建本测试独占的临时目录。
    /// </summary>
    /// <returns>已经创建的临时目录完整路径。</returns>
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bing-segment-io-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// 删除本测试创建的临时目录。
    /// </summary>
    /// <param name="path">本测试创建的目录路径。</param>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }

    /// <summary>
    /// 模拟不可定位、可限制单次读取量的调用方流。
    /// </summary>
    private class CountingNonSeekableStream : Stream
    {
        /// <summary>
        /// 保存可供读取的完整源数据。
        /// </summary>
        private readonly byte[] _bytes;
        /// <summary>
        /// 限制单次读取返回的最大字节数。
        /// </summary>
        private readonly int _maxRead;
        /// <summary>
        /// 保存当前读取偏移。
        /// </summary>
        private int _position;
        /// <summary>
        /// 记录流是否已释放。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 获取实际进入的异步读取次数，包含 EOF 探测。
        /// </summary>
        public int ReadCalls { get; private set; }

        /// <summary>
        /// 创建可控短读流。
        /// </summary>
        /// <param name="bytes">源数据。</param>
        /// <param name="maxRead">每次最多读取的字节数。</param>
        public CountingNonSeekableStream(byte[] bytes, int maxRead)
        {
            _bytes = bytes;
            _maxRead = maxRead;
        }

        /// <summary>
        /// 在复制器每次读取时注入测试行为。
        /// </summary>
        /// <param name="call">当前读取次数。</param>
        protected virtual void OnRead(int call) { }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var call = ++ReadCalls;
            OnRead(call);
            cancellationToken.ThrowIfCancellationRequested();
            var read = Math.Min(count, Math.Min(_maxRead, _bytes.Length - _position));
            if (read > 0)
            {
                Buffer.BlockCopy(_bytes, _position, buffer, offset, read);
                _position += read;
            }
            return Task.FromResult(read);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override bool CanRead => !_disposed;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        /// <inheritdoc />
        public override void Flush() { }
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 在已读取一个完整复制块后注入 I/O 异常或取消。
    /// </summary>
    private sealed class FaultAfterFirstChunkStream : CountingNonSeekableStream
    {
        /// <summary>
        /// 可选的取消回调；为空时第二次读取抛出 I/O 异常。
        /// </summary>
        private readonly Action _cancel;

        /// <summary>
        /// 创建在第二次读取时发生指定行为的流。
        /// </summary>
        /// <param name="bytes">流式替换数据。</param>
        /// <param name="cancel">可选的取消操作。</param>
        public FaultAfterFirstChunkStream(byte[] bytes, Action cancel) : base(bytes, int.MaxValue)
            => _cancel = cancel;

        /// <inheritdoc />
        protected override void OnRead(int call)
        {
            if (call != 2) return;
            if (_cancel != null) _cancel();
            else throw new IOException("injected read failure");
        }
    }
}
