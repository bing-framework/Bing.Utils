using System.Buffers;
using System.Security.Cryptography;
using System.Runtime.ExceptionServices;
using Bing.Operations;

namespace Bing.IO;

/// <summary>
/// 配置从当前位置传输流时的字节预算、校验和进度。
/// </summary>
public sealed class StreamTransferOptions
{
    /// <summary>
    /// 获取或设置最多写入的字节数；null 不限制，零只接受空内容。
    /// </summary>
    public long? MaxBytes { get; set; }
    /// <summary>
    /// 获取或设置复制缓冲区大小，默认 81920 字节，必须大于零。
    /// </summary>
    public int BufferSize { get; set; } = 81920;
    /// <summary>
    /// 获取或设置是否返回 SHA-256 十六进制校验值。
    /// </summary>
    public bool ComputeSha256 { get; set; }
    /// <summary>
    /// 获取或设置预期 SHA-256；非 null 时启用计算并在传输后验证。
    /// </summary>
    public string ExpectedSha256 { get; set; }
    /// <summary>
    /// 获取或设置每次成功写入后报告累计字节数的观察器。
    /// </summary>
    public IProgress<long> Progress { get; set; }

    /// <summary>
    /// 获取或设置统一操作进度观察器；旧的 <see cref="Progress"/> 仍会继续报告累计字节数。
    /// </summary>
    public IProgress<OperationProgress> OperationProgress { get; set; }

    /// <summary>
    /// 固定并校验本次传输配置。
    /// </summary>
    internal StreamTransferOptions Snapshot()
    {
        if (MaxBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxBytes));
        if (BufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(BufferSize));
        if (ExpectedSha256 != null && (ExpectedSha256.Length != 64 || ExpectedSha256.Any(c => !Uri.IsHexDigit(c))))
            throw new ArgumentException("预期 SHA-256 必须为 64 位十六进制。", nameof(ExpectedSha256));
        return (StreamTransferOptions)MemberwiseClone();
    }
}

/// <summary>
/// 表示已完成传输的字节数及可选校验值。
/// </summary>
public sealed class StreamTransferResult
{
    /// <summary>
    /// 获取成功写入字节数。
    /// </summary>
    public long BytesTransferred { get; }
    /// <summary>
    /// 获取 SHA-256 小写十六进制；未计算时为 null。
    /// </summary>
    public string Sha256 { get; }
    /// <summary>
    /// 初始化传输结果。
    /// </summary>
    internal StreamTransferResult(long bytesTransferred, string sha256)
    {
        BytesTransferred = bytesTransferred; Sha256 = sha256;
    }
}

/// <summary>
/// 提供有界、可取消和可校验的流到流传输。
/// </summary>
/// <remarks>
/// 从源当前位置读、向目标当前位置写，不复位、不截断。失败可能留下部分输出；探测超限最多额外读取一个字节。
/// 默认保留两个流；leaveOpen=false 在参数检查及开始前取消之后接管资源，无论成功失败均释放。
/// </remarks>
public static class StreamTransfer
{
    /// <summary>
    /// 同步传输流并返回计数及可选校验值。
    /// </summary>
    /// <param name="source">可读源。</param>
    /// <param name="destination">可写目标，不能与源为同一对象。</param>
    /// <param name="options">字节预算、缓冲和校验配置。</param>
    /// <param name="leaveOpen">是否保留两个流。</param>
    /// <param name="cancellationToken">取消令牌，每次读写之间观察。</param>
    /// <returns>成功传输结果。</returns>
    public static StreamTransferResult Copy(Stream source, Stream destination, StreamTransferOptions options = null,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
        => CopyCore(source, destination, options, leaveOpen, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>
    /// 异步传输流并返回计数及可选校验值。
    /// </summary>
    /// <param name="source">可读源。</param>
    /// <param name="destination">可写目标，不能与源为同一对象。</param>
    /// <param name="options">字节预算、缓冲和校验配置。</param>
    /// <param name="leaveOpen">是否保留两个流。</param>
    /// <param name="cancellationToken">传递给源和目标的取消令牌。</param>
    /// <returns>成功传输结果。</returns>
    public static Task<StreamTransferResult> CopyAsync(Stream source, Stream destination, StreamTransferOptions options = null,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
        => CopyCore(source, destination, options, leaveOpen, cancellationToken, true);

    /// <summary>
    /// 共享计数和资源生命周期，同步路径直接使用同步读写。
    /// </summary>
    private static async Task<StreamTransferResult> CopyCore(Stream source, Stream destination, StreamTransferOptions options,
        bool leaveOpen, CancellationToken token, bool asynchronous)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (ReferenceEquals(source, destination)) throw new ArgumentException("源与目标不能是同一个流。", nameof(destination));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        if (!destination.CanWrite) throw new ArgumentException("目标不可写。", nameof(destination));
        var config = (options ?? new StreamTransferOptions()).Snapshot();
        token.ThrowIfCancellationRequested();
        byte[] buffer = null;
        Exception operationError = null;
        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(config.BufferSize);
            using var hash = config.ComputeSha256 || config.ExpectedSha256 != null ? IncrementalHash.CreateHash(HashAlgorithmName.SHA256) : null;
            long total = 0;
            long sequence = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var remaining = (config.MaxBytes ?? long.MaxValue) - total;
                var requested = remaining >= config.BufferSize ? config.BufferSize : (int)remaining + 1;
                var count = asynchronous
                    ? await source.ReadAsync(buffer, 0, requested, token).ConfigureAwait(false)
                    : source.Read(buffer, 0, requested);
                token.ThrowIfCancellationRequested();
                if (count == 0)
                {
                    config.OperationProgress?.Report(new OperationProgress(OperationStage.Read, ProgressUnit.Byte,
                        total, total: null, sequence: sequence++, isFinal: true));
                    break;
                }
                total += count;
                config.OperationProgress?.Report(new OperationProgress(OperationStage.Read, ProgressUnit.Byte,
                    total, total: null, sequence: sequence++));
                token.ThrowIfCancellationRequested();
                if (count > remaining) throw new InvalidDataException($"流传输超过 {config.MaxBytes} 字节限额。");
                if (asynchronous) await destination.WriteAsync(buffer, 0, count, token).ConfigureAwait(false);
                else destination.Write(buffer, 0, count);
                config.OperationProgress?.Report(new OperationProgress(OperationStage.Write, ProgressUnit.Byte,
                    total, total: null, sequence: sequence++));
                hash?.AppendData(buffer, 0, count);
                config.Progress?.Report(total);
            }
            var checksum = hash == null ? null : BitConverter.ToString(hash.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
            if (config.ExpectedSha256 != null && !string.Equals(config.ExpectedSha256, checksum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("流传输 SHA-256 校验不匹配。");
            token.ThrowIfCancellationRequested();
            config.OperationProgress?.Report(new OperationProgress(OperationStage.Write, ProgressUnit.Byte,
                total, total: null, sequence: sequence++, isFinal: true));
            return new StreamTransferResult(total, checksum);
        }
        catch (Exception error) { operationError = error; throw; }
        finally
        {
            if (buffer != null) ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            if (!leaveOpen)
            {
                Exception cleanupError = null;
                try { destination.Dispose(); }
                catch (Exception error) { cleanupError = error; }
                try { source.Dispose(); }
                catch (Exception error) { cleanupError ??= error; }
                if (operationError == null && cleanupError != null) ExceptionDispatchInfo.Capture(cleanupError).Throw();
            }
        }
    }
}
