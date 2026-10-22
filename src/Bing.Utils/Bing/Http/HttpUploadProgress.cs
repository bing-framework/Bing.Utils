namespace Bing.Http;

/// <summary>
/// 单次 HTTP 发送尝试的不可变上传进度。
/// </summary>
public sealed class HttpUploadProgress
{
    /// <summary>
    /// 初始化一个 <see cref="HttpUploadProgress"/> 类型的实例。
    /// </summary>
    /// <param name="attempt">发送尝试序号，从 1 开始。</param>
    /// <param name="bytesWritten">已成功写入传输流的字节数。</param>
    /// <param name="totalBytes">正文总长度；未知时为空。</param>
    /// <param name="isCompleted">正文是否已完整写入传输流。</param>
    public HttpUploadProgress(int attempt, long bytesWritten, long? totalBytes, bool isCompleted)
    {
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt));
        if (bytesWritten < 0) throw new ArgumentOutOfRangeException(nameof(bytesWritten));
        if (totalBytes < 0) throw new ArgumentOutOfRangeException(nameof(totalBytes));
        Attempt = attempt;
        BytesWritten = bytesWritten;
        TotalBytes = totalBytes;
        IsCompleted = isCompleted;
    }

    /// <summary>
    /// 获取发送尝试序号，从 1 开始。
    /// </summary>
    public int Attempt { get; }
    /// <summary>
    /// 获取已成功写入传输流的字节数。
    /// </summary>
    public long BytesWritten { get; }
    /// <summary>
    /// 获取正文总长度；未知时为空。
    /// </summary>
    public long? TotalBytes { get; }
    /// <summary>
    /// 获取正文是否已完整写入传输流。
    /// </summary>
    public bool IsCompleted { get; }
}
