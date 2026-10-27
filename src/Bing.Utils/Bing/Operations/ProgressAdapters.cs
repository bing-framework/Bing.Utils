using Bing.Http;

namespace Bing.Operations;

/// <summary>
/// 将现有字节和 HTTP 进度通知转换为统一操作进度。
/// </summary>
public static class ProgressAdapters
{
    /// <summary>
    /// 将旧式累计字节进度转换为统一写入进度。
    /// </summary>
    /// <param name="progress">统一进度观察器；为空时返回 null。</param>
    /// <returns>可传给旧式字节进度 API 的观察器。</returns>
    public static IProgress<long> FromBytes(IProgress<OperationProgress> progress)
        => progress == null ? null : new CallbackProgress<long>(new SequenceReporter(progress).ReportBytes);

    /// <summary>
    /// 将 HTTP 下载进度转换为统一写入进度。
    /// </summary>
    /// <param name="progress">统一进度观察器；为空时返回 null。</param>
    /// <returns>可传给 HTTP 下载进度 API 的观察器。</returns>
    public static IProgress<HttpDownloadProgress> FromHttpDownload(IProgress<OperationProgress> progress)
        => progress == null ? null : new CallbackProgress<HttpDownloadProgress>(new SequenceReporter(progress).ReportDownload);

    /// <summary>
    /// 将 HTTP 上传进度转换为统一写入进度。
    /// </summary>
    /// <param name="progress">统一进度观察器；为空时返回 null。</param>
    /// <returns>可传给 HTTP 上传进度 API 的观察器。</returns>
    public static IProgress<HttpUploadProgress> FromHttpUpload(IProgress<OperationProgress> progress)
        => progress == null ? null : new CallbackProgress<HttpUploadProgress>(new SequenceReporter(progress).ReportUpload);

    private sealed class SequenceReporter
    {
        private readonly IProgress<OperationProgress> _progress;
        private long _sequence;

        internal SequenceReporter(IProgress<OperationProgress> progress) => _progress = progress;

        internal void ReportBytes(long bytes)
        {
            _progress.Report(new OperationProgress(OperationStage.Write, ProgressUnit.Byte, bytes,
                total: null, sequence: NextSequence()));
        }

        internal void ReportDownload(HttpDownloadProgress value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _progress.Report(new OperationProgress(OperationStage.Write, ProgressUnit.Byte, value.BytesWritten,
                value.TotalBytes, attempt: 1, sequence: NextSequence(), isFinal: value.IsCompleted));
        }

        internal void ReportUpload(HttpUploadProgress value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _progress.Report(new OperationProgress(OperationStage.Write, ProgressUnit.Byte, value.BytesWritten,
                value.TotalBytes, attempt: value.Attempt, sequence: NextSequence(), isFinal: value.IsCompleted));
        }

        private long NextSequence() => Interlocked.Increment(ref _sequence) - 1;
    }

    private sealed class CallbackProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        internal CallbackProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }
}
