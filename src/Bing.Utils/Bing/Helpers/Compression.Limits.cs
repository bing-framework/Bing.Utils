using System.IO.Compression;

namespace Bing.Helpers;

public static partial class Compression
{
    /// <summary>按实际输出字节数限制 GZip 解压；原 Decompress 入口不改变默认行为。</summary>
    /// <param name="data">压缩数据，空数组返回空数组。</param>
    /// <param name="maxOutputBytes">允许的最大输出字节数，含边界，零仅允许空输出。</param>
    /// <returns>解压结果；超限不返回部分结果。</returns>
    /// <exception cref="ArgumentOutOfRangeException">限额为负数。</exception>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    public static byte[] DecompressWithLimit(byte[] data, long maxOutputBytes)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        ValidateOutputLimit(maxOutputBytes);
        using var input = new MemoryStream(data);
        return DecompressWithLimit(input, maxOutputBytes);
    }

    /// <summary>异步按实际输出字节数限制 GZip 解压；取消和超限均不返回部分结果。</summary>
    /// <param name="data">压缩数据。</param>
    /// <param name="maxOutputBytes">允许的最大输出字节数，非负且含边界。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解压结果。</returns>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    /// <exception cref="OperationCanceledException">操作被取消。</exception>
    public static async Task<byte[]> DecompressWithLimitAsync(byte[] data, long maxOutputBytes, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        ValidateOutputLimit(maxOutputBytes);
        using var input = new MemoryStream(data);
        return await DecompressWithLimitAsync(input, maxOutputBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>解压 Base64 编码的 GZip 文本，限制解码成文本前的输出字节数。</summary>
    /// <param name="value">压缩文本，空白输入返回空字符串。</param>
    /// <param name="maxOutputBytes">最大解压字节数，不是字符数，非负且含边界。</param>
    /// <param name="encoding">文本编码，默认 UTF-8。</param>
    /// <returns>解压后的文本。</returns>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    /// <remarks>此限额不包含 Base64 输入、编码转换及返回副本占用的内存。</remarks>
    public static string DecompressWithLimit(string value, long maxOutputBytes, Encoding encoding = null)
    {
        ValidateOutputLimit(maxOutputBytes);
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return (encoding ?? Encoding.UTF8).GetString(DecompressWithLimit(Convert.FromBase64String(value), maxOutputBytes));
    }

    /// <summary>异步解压 Base64 编码的 GZip 文本，并限制实际输出字节数。</summary>
    /// <param name="value">压缩文本，空白输入返回空字符串。</param>
    /// <param name="maxOutputBytes">最大解压字节数，不是字符数，非负且含边界。</param>
    /// <param name="encoding">文本编码，默认 UTF-8。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解压后的文本。</returns>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    /// <exception cref="OperationCanceledException">操作被取消。</exception>
    public static async Task<string> DecompressWithLimitAsync(string value, long maxOutputBytes, Encoding encoding = null, CancellationToken cancellationToken = default)
    {
        ValidateOutputLimit(maxOutputBytes);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var bytes = await DecompressWithLimitAsync(Convert.FromBase64String(value), maxOutputBytes, cancellationToken).ConfigureAwait(false);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    /// <summary>从当前位置解压 GZip 流，支持短读和不可定位流，并限制实际输出字节数。</summary>
    /// <param name="stream">输入流；开始解压后成功或失败均关闭，可定位空流或参数检查失败时不关闭。</param>
    /// <param name="maxOutputBytes">最大输出字节数，非负且含边界。</param>
    /// <returns>解压结果；超限不返回部分结果。</returns>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    /// <remarks>限额约束逻辑输出量，不是进程内存、CPU 时间或压缩输入大小的预算。</remarks>
    public static byte[] DecompressWithLimit(Stream stream, long maxOutputBytes)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        ValidateOutputLimit(maxOutputBytes);
        if (stream.CanSeek && stream.Length == 0) return [];
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        CopyWithLimit(gzip, output, maxOutputBytes);
        return output.ToArray();
    }

    /// <summary>异步从当前位置解压 GZip 流，支持短读和不可定位流，并限制实际输出字节数。</summary>
    /// <param name="stream">输入流；开始解压后成功或失败均关闭，可定位空流或开始前取消时不关闭。</param>
    /// <param name="maxOutputBytes">最大输出字节数，非负且含边界。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解压结果；超限不返回部分结果。</returns>
    /// <exception cref="InvalidDataException">数据无效或实际输出超过限额。</exception>
    /// <exception cref="OperationCanceledException">操作被取消。</exception>
    public static async Task<byte[]> DecompressWithLimitAsync(Stream stream, long maxOutputBytes, CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        ValidateOutputLimit(maxOutputBytes);
        cancellationToken.ThrowIfCancellationRequested();
        if (stream.CanSeek && stream.Length == 0) return [];
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await CopyWithLimitAsync(gzip, output, maxOutputBytes, cancellationToken).ConfigureAwait(false);
        return output.ToArray();
    }

    /// <summary>将 ZIP 解压到目录，限制整个归档的累计解压输出字节数。</summary>
    /// <param name="zipFile">ZIP 文件路径。</param>
    /// <param name="targetDir">由调用方控制的目标目录。</param>
    /// <param name="maxOutputBytes">所有文件累计输出上限，非负且含边界；不是单文件上限。</param>
    /// <param name="overwriteFiles">是否覆盖已有文件，默认为 true。</param>
    /// <exception cref="InvalidDataException">路径非法、数据无效或声明/实际输出超过限额。</exception>
    /// <exception cref="IOException">文件读写失败，或目标文件已存在且不允许覆盖。</exception>
    /// <remarks>
    /// 写入前预检全部条目的路径和声明大小，复制时再次累计实际字节。
    /// 每个文件先写入同目录临时文件，完整解压后才覆盖目标；失败时清理当前临时文件。
    /// 同目录移动或替换提交文件；平台不支持替换时回退复制，该回退仍有额外 I/O 且不保证原子性。
    /// 不回滚已完成文件或已创建目录；此限额不约束条目数量、磁盘分配开销或运行时间。
    /// 调用方须避免其他进程并发替换目标目录中的路径。旧 UnZip 入口保持原行为。
    /// </remarks>
    public static void UnZipWithLimit(string zipFile, string targetDir, long maxOutputBytes, bool overwriteFiles = true)
    {
        if (string.IsNullOrWhiteSpace(zipFile)) throw new ArgumentException("ZIP 文件路径不能为空", nameof(zipFile));
        if (string.IsNullOrWhiteSpace(targetDir)) throw new ArgumentException("目标目录路径不能为空", nameof(targetDir));
        ValidateOutputLimit(maxOutputBytes);
        using var archive = ZipFile.OpenRead(zipFile);
        var declaredRemaining = maxOutputBytes;
        foreach (var entry in archive.Entries)
        {
            GetArchiveDestination(targetDir, entry.FullName);
            if (entry.Length > declaredRemaining) ThrowOutputLimitExceeded(maxOutputBytes);
            declaredRemaining -= entry.Length;
            if (IsArchiveDirectory(entry) && entry.Length != 0)
                throw new InvalidDataException("ZIP 目录条目不能包含文件内容。");
        }

        Directory.CreateDirectory(targetDir);
        var remaining = maxOutputBytes;
        foreach (var entry in archive.Entries)
        {
            var destination = GetArchiveDestination(targetDir, entry.FullName);
            if (IsArchiveDirectory(entry))
            {
                Directory.CreateDirectory(destination);
                continue;
            }
            if (!overwriteFiles && File.Exists(destination))
                throw new IOException($"目标文件已存在: {destination}");
            var directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, ".bing-unzip-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var input = entry.Open())
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    remaining -= CopyWithLimit(input, output, remaining);
                // 再次检查目标，避免覆盖在解压当前条目之前就已存在的链接。
                GetArchiveDestination(targetDir, entry.FullName);
                CommitArchiveFile(temporary, destination, overwriteFiles);
            }
            finally
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>同目录提交解压结果；仅在平台明确不支持替换时回退复制，不预先删除旧目标。</summary>
    internal static void CommitArchiveFile(string temporary, string destination, bool overwriteFiles,
        Action<string, string> replace = null)
    {
        if (!overwriteFiles || !File.Exists(destination))
        {
            File.Move(temporary, destination);
            return;
        }
        try
        {
            if (replace != null) replace(temporary, destination);
            else File.Replace(temporary, destination, null);
        }
        catch (NotSupportedException)
        {
            // 此回退不承诺原子性，保留旧平台的兼容写入路径；其他 I/O 错误直接传播。
            File.Copy(temporary, destination, true);
        }
    }

    /// <summary>识别跨平台 ZIP 目录条目。</summary>
    private static bool IsArchiveDirectory(ZipArchiveEntry entry)
        => entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal);

    /// <summary>验证输出限额，允许零以表示只接受空输出。</summary>
    private static void ValidateOutputLimit(long maxOutputBytes)
    {
        if (maxOutputBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxOutputBytes));
    }

    /// <summary>抛出统一的输出超限异常。</summary>
    private static void ThrowOutputLimitExceeded(long maxOutputBytes)
        => throw new InvalidDataException($"解压输出超过允许的 {maxOutputBytes} 字节。");

    /// <summary>只多读取一个探测字节，确保恰好等于限额的输出可以成功，超额字节不会写入目标。</summary>
    private static long CopyWithLimit(Stream input, Stream output, long maxOutputBytes)
    {
        var buffer = new byte[DefaultBufferSize];
        long total = 0;
        while (true)
        {
            var remaining = maxOutputBytes - total;
            var requested = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
            var count = input.Read(buffer, 0, requested);
            if (count == 0) return total;
            if (count > remaining) ThrowOutputLimitExceeded(maxOutputBytes);
            output.Write(buffer, 0, count);
            total += count;
        }
    }

    /// <summary>异步按实际输出计数，每次读写均传递取消令牌。</summary>
    private static async Task CopyWithLimitAsync(Stream input, Stream output, long maxOutputBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[DefaultBufferSize];
        long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = maxOutputBytes - total;
            var requested = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
            var count = await input.ReadAsync(buffer, 0, requested, cancellationToken).ConfigureAwait(false);
            if (count == 0) return;
            if (count > remaining) ThrowOutputLimitExceeded(maxOutputBytes);
            await output.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
            total += count;
        }
    }
}
