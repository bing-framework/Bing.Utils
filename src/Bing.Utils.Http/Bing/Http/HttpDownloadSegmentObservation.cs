namespace Bing.Http;

/// <summary>

/// 一次分片完成后的进度信息。

/// </summary>
public sealed class HttpDownloadSegmentProgress
{
    /// <summary>
    /// 初始化一个 <see cref="HttpDownloadSegmentProgress"/> 类型的实例。
    /// </summary>
    public HttpDownloadSegmentProgress(HttpDownloadRange range, int attempt,
        long completedBytes, long totalBytes)
    {
        Range = range;
        Attempt = attempt;
        CompletedBytes = completedBytes;
        TotalBytes = totalBytes;
    }

    /// <summary>

    /// 获取已完成的分片范围。

    /// </summary>
    public HttpDownloadRange Range { get; }

    /// <summary>

    /// 获取该分片使用的尝试序号，从一开始。

    /// </summary>
    public int Attempt { get; }

    /// <summary>

    /// 获取当前所有成功分片累计字节数。

    /// </summary>
    public long CompletedBytes { get; }

    /// <summary>

    /// 获取正文总字节数。

    /// </summary>
    public long TotalBytes { get; }
}

/// <summary>

/// 一个分片最终失败后的诊断信息。

/// </summary>
public sealed class HttpDownloadSegmentFailure
{
    /// <summary>
    /// 初始化一个 <see cref="HttpDownloadSegmentFailure"/> 类型的实例。
    /// </summary>
    public HttpDownloadSegmentFailure(HttpDownloadRange range, int attempts, Exception error)
    {
        Range = range;
        Attempts = attempts;
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>

    /// 获取失败的分片范围。

    /// </summary>
    public HttpDownloadRange Range { get; }

    /// <summary>

    /// 获取已执行的总尝试次数。

    /// </summary>
    public int Attempts { get; }

    /// <summary>

    /// 获取最后一次失败异常。

    /// </summary>
    public Exception Error { get; }
}
