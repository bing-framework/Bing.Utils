namespace Bing.Http;

/// <summary>
/// 下载正文的不可变进度；正文完成不表示目标文件已提交。
/// </summary>
public sealed class HttpDownloadProgress
{
    /// <summary>
    /// 初始化一个 <see cref="HttpDownloadProgress"/> 类型的实例。
    /// </summary>
    /// <param name="bytesWritten">已成功写入目标的字节数，不能为负。</param>
    /// <param name="totalBytes">可用的正文长度；未知时为空，不能为负。</param>
    /// <param name="isCompleted">正文是否已到达 EOF。</param>
    public HttpDownloadProgress(long bytesWritten, long? totalBytes, bool isCompleted)
    {
        if (bytesWritten < 0) throw new ArgumentOutOfRangeException(nameof(bytesWritten));
        if (totalBytes < 0) throw new ArgumentOutOfRangeException(nameof(totalBytes));
        BytesWritten = bytesWritten;
        TotalBytes = totalBytes;
        IsCompleted = isCompleted;
    }

    /// <summary>
    /// 获取实际成功写入字节数。
    /// </summary>
    public long BytesWritten { get; }
    /// <summary>
    /// 获取响应描述的正文长度；未知时为空。
    /// </summary>
    public long? TotalBytes { get; }
    /// <summary>
    /// 获取正文到达 EOF；文件最终保存状态由下载结果提供。
    /// </summary>
    public bool IsCompleted { get; }
}
