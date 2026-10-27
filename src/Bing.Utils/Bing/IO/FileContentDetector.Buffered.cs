namespace Bing.IO;

/// <summary>提供带有界缓存的文件内容探测入口。</summary>
public static partial class FileContentDetector
{
    /// <summary>从当前源位置读取并缓存至 EOF，随后检测完整内容。</summary>
    /// <param name="source">待读取的可读源流；默认保留，成功后已到达 EOF。</param>
    /// <param name="store">管理溢写临时文件的存储；其生命周期仍由调用方负责。</param>
    /// <param name="maxContentBytes">缓存内容的最大字节数；超过时最多额外读取一个字节并抛出异常。</param>
    /// <param name="options">检测选项；省略时启用 ZIP 容器检查，显式传入时按其值执行。</param>
    /// <param name="memoryThresholdBytes">缓存的内存阈值，默认 1 MiB；超过后转存到临时文件。</param>
    /// <param name="leaveOpen">是否保留源流；验证和初始取消检查通过后，false 会在操作结束时关闭源。</param>
    /// <param name="cancellationToken">用于取消源读取及检测的令牌。</param>
    /// <returns>检测结果和从零开始的只读可定位缓存流；释放结果会清理缓存。</returns>
    /// <exception cref="ArgumentNullException">source 或 store 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">容量上限或内存阈值为负数，或检测选项预算无效。</exception>
    /// <exception cref="InvalidDataException">源内容超过 maxContentBytes。</exception>
    public static FileContentBuffer ProbeBuffered(Stream source, TemporaryFileStore store, long maxContentBytes,
        FileDetectionOptions options = null, int memoryThresholdBytes = 1024 * 1024, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
        => ProbeBufferedCoreAsync(source, store, maxContentBytes, options, memoryThresholdBytes,
            leaveOpen, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>异步读取并缓存当前源位置之后的完整内容，随后检测缓存内容。</summary>
    /// <param name="source">待异步读取的可读源流；默认保留，成功后已到达 EOF。</param>
    /// <param name="store">管理溢写临时文件的存储；其生命周期仍由调用方负责。</param>
    /// <param name="maxContentBytes">缓存内容的最大字节数；超过时最多额外读取一个字节并抛出异常。</param>
    /// <param name="options">检测选项；省略时启用 ZIP 容器检查，显式传入时按其值执行。</param>
    /// <param name="memoryThresholdBytes">缓存的内存阈值，默认 1 MiB；超过后转存到临时文件。</param>
    /// <param name="leaveOpen">是否保留源流；验证和初始取消检查通过后，false 会在操作结束时关闭源。</param>
    /// <param name="cancellationToken">传递给源读取、缓存写入和检测过程的令牌。</param>
    /// <returns>检测结果和从零开始的只读可定位缓存流；释放结果会清理缓存。</returns>
    /// <exception cref="ArgumentNullException">source 或 store 为 null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">容量上限或内存阈值为负数，或检测选项预算无效。</exception>
    /// <exception cref="InvalidDataException">源内容超过 maxContentBytes。</exception>
    public static Task<FileContentBuffer> ProbeBufferedAsync(Stream source, TemporaryFileStore store,
        long maxContentBytes, FileDetectionOptions options = null, int memoryThresholdBytes = 1024 * 1024,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
        => ProbeBufferedCoreAsync(source, store, maxContentBytes, options, memoryThresholdBytes,
            leaveOpen, cancellationToken, true);

    /// <summary>完成所有不接管资源的验证，并在读取源之前固定检测选项。</summary>
    private static FileDetectionOptions ValidateBuffered(Stream source, TemporaryFileStore store,
        long maxContentBytes, FileDetectionOptions options, int memoryThresholdBytes, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (maxContentBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxContentBytes));
        if (memoryThresholdBytes < 0) throw new ArgumentOutOfRangeException(nameof(memoryThresholdBytes));
        if (!source.CanRead) throw new ArgumentException("源流必须可读。", nameof(source));
        var snapshot = FileDetectionOptions.Snapshot(options ?? new FileDetectionOptions { InspectContainers = true });
        token.ThrowIfCancellationRequested();
        return snapshot;
    }

    /// <summary>共享缓存生命周期；同步分支只使用同步源读取和缓存操作。</summary>
    private static async Task<FileContentBuffer> ProbeBufferedCoreAsync(Stream source, TemporaryFileStore store,
        long maxContentBytes, FileDetectionOptions options, int memoryThresholdBytes, bool leaveOpen,
        CancellationToken token, bool asynchronous)
    {
        var snapshot = ValidateBuffered(source, store, maxContentBytes, options, memoryThresholdBytes, token);
        SpilloverStream cache = null;
        FileContentBuffer buffer = null;
        var sourceDisposeAttempted = false;
        try
        {
            cache = new SpilloverStream(store, memoryThresholdBytes, maxContentBytes);
            var transferOptions = new StreamTransferOptions { MaxBytes = maxContentBytes };
            if (asynchronous)
                await StreamTransfer.CopyAsync(source, cache, transferOptions, cancellationToken: token).ConfigureAwait(false);
            else
                StreamTransfer.Copy(source, cache, transferOptions, cancellationToken: token);

            cache.Position = 0;
            var result = asynchronous
                ? await DetectAsync(cache, snapshot, token).ConfigureAwait(false)
                : Detect(cache, snapshot, token);
            cache.Position = 0;
            buffer = new FileContentBuffer(result, cache);
            cache = null;

            if (!leaveOpen)
            {
                sourceDisposeAttempted = true;
                if (asynchronous)
                    await DisposeBufferedSourceAsync(source).ConfigureAwait(false);
                else
                    source.Dispose();
            }

            var completed = buffer;
            buffer = null;
            return completed;
        }
        catch
        {
            if (buffer != null)
            {
                if (asynchronous)
                    await buffer.DisposeAsync().ConfigureAwait(false);
                else
                    buffer.Dispose();
                buffer = null;
            }
            throw;
        }
        finally
        {
            try
            {
                if (cache != null)
                {
                    if (asynchronous)
                        await cache.DisposeAsync().ConfigureAwait(false);
                    else
                        cache.Dispose();
                }
            }
            finally
            {
                if (!leaveOpen && !sourceDisposeAttempted)
                {
                    sourceDisposeAttempted = true;
                    if (asynchronous)
                        await DisposeBufferedSourceAsync(source).ConfigureAwait(false);
                    else
                        source.Dispose();
                }
            }
        }
    }

    /// <summary>在支持的平台异步释放源流；netstandard2.0 使用其可用的同步释放契约。</summary>
    private static ValueTask DisposeBufferedSourceAsync(Stream source)
    {
#if NETSTANDARD2_0
        source.Dispose();
        return default;
#else
        return source.DisposeAsync();
#endif
    }
}
