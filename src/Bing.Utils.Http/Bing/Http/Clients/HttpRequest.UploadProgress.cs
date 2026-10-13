using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> : IHttpRequestUploadProgressRegistration<TResult> where TResult : class
{
    /// <summary>
    /// 接收请求正文上传进度快照的回调。
    /// </summary>
    private Action<HttpUploadProgress> _uploadProgressObserver;
    /// <summary>
    /// 两次上传进度通知之间的最小字节间隔。
    /// </summary>
    private long _uploadProgressMinimumBytes;
    /// <summary>
    /// 两次上传进度通知之间的最小时间间隔。
    /// </summary>
    private TimeSpan _uploadProgressMinimumInterval;

    /// <inheritdoc />
    public void SetUploadProgressObserver(Action<HttpUploadProgress> observer, long minimumBytes, TimeSpan minimumInterval)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        if (minimumBytes <= 0) throw new ArgumentOutOfRangeException(nameof(minimumBytes));
        if (minimumInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minimumInterval));
        _uploadProgressObserver = observer;
        _uploadProgressMinimumBytes = minimumBytes;
        _uploadProgressMinimumInterval = minimumInterval;
    }

    /// <summary>

    /// 包装请求正文以观测上传进度。

    /// </summary>
    private HttpContent WrapUploadContent(HttpContent inner, int attempt, Action<HttpUploadProgress> observer,
        long minimumBytes, TimeSpan minimumInterval)
    {
        if (inner == null || observer == null) return inner;
        return new UploadProgressContent(inner, attempt, observer, minimumBytes, minimumInterval);
    }

    /// <summary>

    /// 包装请求正文并在序列化时观测上传进度。

    /// </summary>
    private sealed class UploadProgressContent : HttpContent
    {
        /// <summary>
        /// 被进度包装器代理的 HTTP 正文内容。
        /// </summary>
        private readonly HttpContent _inner;
        /// <summary>
        /// 当前正文发送尝试的序号。
        /// </summary>
        private readonly int _attempt;
        /// <summary>
        /// 接收当前上传尝试进度的回调。
        /// </summary>
        private readonly Action<HttpUploadProgress> _observer;
        /// <summary>
        /// 触发下一次上传进度通知所需的字节增量。
        /// </summary>
        private readonly long _minimumBytes;
        /// <summary>
        /// 触发下一次上传进度通知所需的时间间隔。
        /// </summary>
        private readonly TimeSpan _minimumInterval;

        /// <summary>
        /// 初始化一个 <see cref="UploadProgressContent"/> 类型的实例。
        /// </summary>
        public UploadProgressContent(HttpContent inner, int attempt, Action<HttpUploadProgress> observer,
            long minimumBytes, TimeSpan minimumInterval)
        {
            _inner = inner;
            _attempt = attempt;
            _observer = observer;
            _minimumBytes = minimumBytes;
            _minimumInterval = minimumInterval;
            foreach (var header in inner.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            => SerializeToStreamAsyncCore(stream, context, CancellationToken.None);

#if NET5_0_OR_GREATER
        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context,
            CancellationToken cancellationToken)
            => SerializeToStreamAsyncCore(stream, context, cancellationToken);
#endif

        /// <summary>

        /// 复制上传正文并按阈值报告进度。

        /// </summary>
        private async Task SerializeToStreamAsyncCore(Stream stream, TransportContext context,
            CancellationToken cancellationToken)
        {
            var total = _inner.Headers.ContentLength;
            var state = new UploadProgressState(_attempt, _observer, _minimumBytes, _minimumInterval, total);
            state.Report(0, total, false, true);
#if NET5_0_OR_GREATER
            await _inner.CopyToAsync(new ProgressStream(stream, state), context, cancellationToken).ConfigureAwait(false);
#else
            await _inner.CopyToAsync(new ProgressStream(stream, state)).ConfigureAwait(false);
#endif
#if NET5_0_OR_GREATER
            cancellationToken.ThrowIfCancellationRequested();
#endif
            state.Report(state.BytesWritten, total, true, true);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            var contentLength = _inner.Headers.ContentLength;
            length = contentLength.GetValueOrDefault();
            return contentLength.HasValue;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>

    /// 跟踪单次请求正文发送尝试的上传状态。

    /// </summary>
    private sealed class UploadProgressState
    {
        /// <summary>
        /// 当前正文发送尝试的序号。
        /// </summary>
        private readonly int _attempt;
        /// <summary>
        /// 接收当前上传尝试进度的回调。
        /// </summary>
        private readonly Action<HttpUploadProgress> _observer;
        /// <summary>
        /// 触发下一次上传进度通知所需的字节增量。
        /// </summary>
        private readonly long _minimumBytes;
        /// <summary>
        /// 触发下一次上传进度通知所需的时间间隔。
        /// </summary>
        private readonly TimeSpan _minimumInterval;
        /// <summary>
        /// 已知时表示请求正文的总字节数。
        /// </summary>
        private readonly long? _totalBytes;
        /// <summary>
        /// 记录当前上传尝试的经过时间。
        /// </summary>
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        /// <summary>
        /// 上一次已通知的累计上传字节数。
        /// </summary>
        private long _lastBytes;
        /// <summary>
        /// 上一次已通知时的经过时间。
        /// </summary>
        private TimeSpan _lastTime;

        /// <summary>
        /// 初始化一个 <see cref="UploadProgressState"/> 类型的实例。
        /// </summary>
        public UploadProgressState(int attempt, Action<HttpUploadProgress> observer, long minimumBytes, TimeSpan minimumInterval, long? totalBytes)
        {
            _attempt = attempt;
            _observer = observer;
            _minimumBytes = minimumBytes;
            _minimumInterval = minimumInterval;
            _totalBytes = totalBytes;
        }

        /// <summary>

        /// 获取当前发送尝试已经写入的字节数。

        /// </summary>
        public long BytesWritten { get; private set; }

        /// <summary>

        /// 累加已写入字节并按阈值报告进度。

        /// </summary>
        /// <param name="bytes">本次写入的字节数。</param>
        public void Add(long bytes)
        {
            BytesWritten += bytes;
            Report(BytesWritten, _totalBytes, false, false);
        }

        /// <summary>

        /// 按进度阈值向观察者报告上传状态。

        /// </summary>
        /// <param name="bytes">已写入的字节数。</param>
        /// <param name="total">已知时表示正文总字节数。</param>
        /// <param name="completed">是否已完成正文发送。</param>
        /// <param name="force">是否忽略阈值立即报告。</param>
        public void Report(long bytes, long? total, bool completed, bool force)
        {
            var elapsed = _watch.Elapsed;
            if (!force && (bytes - _lastBytes < _minimumBytes || elapsed - _lastTime < _minimumInterval)) return;
            _observer(new HttpUploadProgress(_attempt, bytes, total, completed));
            _lastBytes = bytes;
            _lastTime = elapsed;
        }
    }

    /// <summary>

    /// 代理正文写入并累计上传进度的流。

    /// </summary>
    private sealed class ProgressStream : Stream
    {
        /// <summary>
        /// 被包装并统计写入进度的底层流。
        /// </summary>
        private readonly Stream _inner;
        /// <summary>
        /// 共享当前上传尝试的进度状态。
        /// </summary>
        private readonly UploadProgressState _state;

        /// <summary>
        /// 初始化一个 <see cref="ProgressStream"/> 类型的实例。
        /// </summary>
        public ProgressStream(Stream inner, UploadProgressState state)
        {
            _inner = inner;
            _state = state;
        }

        /// <inheritdoc />
        public override bool CanRead => _inner.CanRead;
        /// <inheritdoc />
        public override bool CanSeek => _inner.CanSeek;
        /// <inheritdoc />
        public override bool CanWrite => _inner.CanWrite;
        /// <inheritdoc />
        public override long Length => _inner.Length;
        /// <inheritdoc />
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        /// <inheritdoc />
        public override void Flush() => _inner.Flush();
        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        /// <inheritdoc />
        public override void SetLength(long value) => _inner.SetLength(value);
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            _state.Add(count);
        }
        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            _state.Add(count);
        }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // HttpContent owns the destination stream; the wrapper must not close it.
            base.Dispose(disposing);
        }
    }
}
