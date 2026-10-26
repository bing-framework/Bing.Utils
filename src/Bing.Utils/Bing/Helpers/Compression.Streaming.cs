using System.IO.Compression;

namespace Bing.Helpers;

public static partial class Compression
{
    /// <summary>从源当前位置直接压缩到目标当前位置；不复位、不截断目标。空输入生成有效 GZip。</summary>
    /// <param name="source">可读源流。</param>
    /// <param name="destination">可写目标流，不得与源为同一对象。</param>
    /// <param name="compressionLevel">GZip 压缩级别。</param>
    /// <param name="leaveOpen">默认保留两个流；false 时进入处理后无论成功失败均释放两者。</param>
    /// <remarks>参数检查失败不接管流。处理失败可能留下部分输出，由调用方清理。</remarks>
    public static void CompressTo(Stream source, Stream destination, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool leaveOpen = true)
    {
        ValidateTransferStreams(source, destination);
        ValidateCompressionLevel(compressionLevel);
        try
        {
            using var gzip = new GZipStream(destination, compressionLevel, true);
            // 显式写入零字节，使旧运行时也初始化空 GZip 包装。
            gzip.Write(Array.Empty<byte>(), 0, 0);
            source.CopyTo(gzip, DefaultBufferSize);
        }
        finally { CloseTransferStreams(source, destination, leaveOpen); }
    }

    /// <summary>异步从源当前位置压缩到目标当前位置；失败可留下部分输出，不复位或截断目标。</summary>
    /// <param name="source">可读源流。</param>
    /// <param name="destination">可写目标流。</param>
    /// <param name="compressionLevel">GZip 压缩级别。</param>
    /// <param name="leaveOpen">是否保留两个流；参数检查或预取消失败不接管资源。</param>
    /// <param name="cancellationToken">所有异步读写的取消令牌。</param>
    public static async Task CompressToAsync(Stream source, Stream destination, CompressionLevel compressionLevel = CompressionLevel.Optimal,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateTransferStreams(source, destination);
        ValidateCompressionLevel(compressionLevel);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var gzip = new GZipStream(destination, compressionLevel, true);
            await gzip.WriteAsync(Array.Empty<byte>(), 0, 0, cancellationToken).ConfigureAwait(false);
            await source.CopyToAsync(gzip, DefaultBufferSize, cancellationToken).ConfigureAwait(false);
        }
        finally { CloseTransferStreams(source, destination, leaveOpen); }
    }

    /// <summary>从当前位置解压到目标流，并限制实际输出；允许失败时保留限额以内的部分输出。</summary>
    /// <param name="source">可读 GZip 流。</param>
    /// <param name="destination">可写目标流，不复位、不截断。</param>
    /// <param name="maxOutputBytes">非负输出上限，包含边界；零仅允许空输出。</param>
    /// <param name="leaveOpen">默认保留两个流；false 时处理开始后无论成功失败均释放两者。</param>
    /// <exception cref="InvalidDataException">数据损坏或实际输出超限。</exception>
    /// <remarks>参数检查不接管资源。限额不等同于内存或 CPU 预算，失败不回滚目标。</remarks>
    public static void DecompressTo(Stream source, Stream destination, long maxOutputBytes, bool leaveOpen = true)
    {
        ValidateTransferStreams(source, destination);
        ValidateOutputLimit(maxOutputBytes);
        try
        {
            using var gzip = new GZipStream(source, CompressionMode.Decompress, true);
            CopyWithLimit(gzip, destination, maxOutputBytes);
        }
        finally { CloseTransferStreams(source, destination, leaveOpen); }
    }

    /// <summary>异步限额解压到目标当前位置；超限不写入额外字节，失败不回滚已写出的内容。</summary>
    /// <param name="source">可读 GZip 流。</param>
    /// <param name="destination">可写目标流，不复位、不截断。</param>
    /// <param name="maxOutputBytes">非负实际输出上限，包含边界。</param>
    /// <param name="leaveOpen">是否保留两个流；参数检查或预取消失败不接管资源。</param>
    /// <param name="cancellationToken">异步读写的取消令牌。</param>
    public static async Task DecompressToAsync(Stream source, Stream destination, long maxOutputBytes, bool leaveOpen = true,
        CancellationToken cancellationToken = default)
    {
        ValidateTransferStreams(source, destination);
        ValidateOutputLimit(maxOutputBytes);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var gzip = new GZipStream(source, CompressionMode.Decompress, true);
            await CopyWithLimitAsync(gzip, destination, maxOutputBytes, cancellationToken).ConfigureAwait(false);
        }
        finally { CloseTransferStreams(source, destination, leaveOpen); }
    }

    /// <summary>在接管资源之前统一验证流参数。</summary>
    private static void ValidateTransferStreams(Stream source, Stream destination)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (ReferenceEquals(source, destination)) throw new ArgumentException("源流和目标流不能为同一对象。", nameof(destination));
        if (!source.CanRead) throw new ArgumentException("源流必须可读。", nameof(source));
        if (!destination.CanWrite) throw new ArgumentException("目标流必须可写。", nameof(destination));
    }

    /// <summary>提前验证当前目标框架支持的压缩级别。</summary>
    private static void ValidateCompressionLevel(CompressionLevel compressionLevel)
    {
        if (compressionLevel == CompressionLevel.Optimal || compressionLevel == CompressionLevel.Fastest || compressionLevel == CompressionLevel.NoCompression) return;
#if NET6_0_OR_GREATER
        if (compressionLevel == CompressionLevel.SmallestSize) return;
#endif
        throw new ArgumentOutOfRangeException(nameof(compressionLevel));
    }

    /// <summary>即使第一个流释放失败，也尝试释放另一个流。</summary>
    private static void CloseTransferStreams(Stream source, Stream destination, bool leaveOpen)
    {
        if (leaveOpen) return;
        try { source.Dispose(); }
        finally { destination.Dispose(); }
    }
}
