using System.IO.Compression;
using Bing.IO;

namespace Bing.Helpers;

/// <summary>
/// 配置 ZIP 解压的输出预算和覆盖策略。
/// </summary>
public sealed class ZipExtractionOptions
{
    /// <summary>
    /// 获取或设置累计文件输出上限；null 不限制。
    /// </summary>
    public long? MaxTotalBytes { get; set; }
    /// <summary>
    /// 获取或设置单文件输出上限；null 不限制。
    /// </summary>
    public long? MaxEntryBytes { get; set; }
    /// <summary>
    /// 获取或设置归档条目上限，包含目录条目；null 不限制。
    /// </summary>
    public int? MaxEntries { get; set; }
    /// <summary>
    /// 获取或设置相对目录层数上限，根目录文件为零层；null 不限制。
    /// </summary>
    public int? MaxDirectoryDepth { get; set; }
    /// <summary>
    /// 获取或设置是否覆盖已有文件，默认允许。
    /// </summary>
    public bool OverwriteFiles { get; set; } = true;

    /// <summary>
    /// 固定并校验本次解压预算。
    /// </summary>
    internal ZipExtractionOptions Snapshot()
    {
        if (MaxTotalBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxTotalBytes));
        if (MaxEntryBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxEntryBytes));
        if (MaxEntries < 0) throw new ArgumentOutOfRangeException(nameof(MaxEntries));
        if (MaxDirectoryDepth < 0) throw new ArgumentOutOfRangeException(nameof(MaxDirectoryDepth));
        return (ZipExtractionOptions)MemberwiseClone();
    }
}

public static partial class Compression
{
    /// <summary>
    /// 按多维预算解压 ZIP，并在读取条目和文件复制时观察取消。
    /// </summary>
    /// <param name="zipFile">ZIP 路径。</param>
    /// <param name="targetDir">调用方控制的目标目录。</param>
    /// <param name="options">显式预算，不能为 null。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <remarks>写入前预检路径和声明大小，逐文件临时写入并提交，不回滚已提交文件。替换仅在平台不支持时沿用复制降级。</remarks>
    public static void UnZipWithOptions(string zipFile, string targetDir, ZipExtractionOptions options, CancellationToken cancellationToken = default)
        => ExtractWithOptions(zipFile, targetDir, options, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>
    /// 异步复制 ZIP 条目并按累计、单文件、条目数及目录深度限制解压。
    /// </summary>
    /// <param name="zipFile">ZIP 路径。</param>
    /// <param name="targetDir">调用方控制的目标目录。</param>
    /// <param name="options">显式预算，不能为 null。</param>
    /// <param name="cancellationToken">取消令牌，取消不提交当前临时文件。</param>
    /// <remarks>中央目录读取和文件提交为同步操作；整个归档不具备事务性。</remarks>
    public static Task UnZipWithOptionsAsync(string zipFile, string targetDir, ZipExtractionOptions options, CancellationToken cancellationToken = default)
        => ExtractWithOptions(zipFile, targetDir, options, cancellationToken, true);

    /// <summary>
    /// 共享预检和提交逻辑，避免同步/异步路径安全规则分叉。
    /// </summary>
    private static async Task ExtractWithOptions(string zipFile, string targetDir, ZipExtractionOptions options, CancellationToken token, bool asynchronous)
    {
        if (string.IsNullOrWhiteSpace(zipFile)) throw new ArgumentException("ZIP 路径不能为空。", nameof(zipFile));
        if (string.IsNullOrWhiteSpace(targetDir)) throw new ArgumentException("目标目录不能为空。", nameof(targetDir));
        var config = (options ?? throw new ArgumentNullException(nameof(options))).Snapshot();
        token.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(zipFile);
        if (config.MaxEntries.HasValue && archive.Entries.Count > config.MaxEntries.Value)
            throw new InvalidDataException("ZIP 条目数量超过限额。");
        var declaredRemaining = config.MaxTotalBytes ?? long.MaxValue;
        foreach (var entry in archive.Entries)
        {
            token.ThrowIfCancellationRequested();
            GetArchiveDestination(targetDir, entry.FullName);
            var directory = IsArchiveDirectory(entry);
            var parts = entry.FullName.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var depth = Math.Max(0, parts.Length - (directory ? 0 : 1));
            if (config.MaxDirectoryDepth.HasValue && depth > config.MaxDirectoryDepth.Value)
                throw new InvalidDataException("ZIP 目录层数超过限额。");
            if (directory && entry.Length != 0) throw new InvalidDataException("目录条目不能包含数据。");
            if (entry.Length > declaredRemaining || config.MaxEntryBytes.HasValue && entry.Length > config.MaxEntryBytes.Value)
                throw new InvalidDataException("ZIP 声明大小超过解压限额。");
            declaredRemaining -= entry.Length;
        }
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(targetDir);
        var remaining = config.MaxTotalBytes ?? long.MaxValue;
        foreach (var entry in archive.Entries)
        {
            token.ThrowIfCancellationRequested();
            var destination = GetArchiveDestination(targetDir, entry.FullName);
            if (IsArchiveDirectory(entry)) { Directory.CreateDirectory(destination); continue; }
            if (!config.OverwriteFiles && File.Exists(destination)) throw new IOException($"目标文件已存在：{destination}");
            var parent = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(parent);
            var temporary = Path.Combine(parent, ".bing-unzip-" + Guid.NewGuid().ToString("N") + ".tmp");
            var created = false;
            try
            {
                using (var input = entry.Open())
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                    asynchronous ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.SequentialScan))
                {
                    created = true;
                    var budget = new StreamTransferOptions { MaxBytes = Math.Min(remaining, config.MaxEntryBytes ?? long.MaxValue) };
                    var result = asynchronous
                        ? await StreamTransfer.CopyAsync(input, output, budget, cancellationToken: token).ConfigureAwait(false)
                        : StreamTransfer.Copy(input, output, budget, cancellationToken: token);
                    if (result.BytesTransferred != entry.Length) throw new InvalidDataException("ZIP 实际长度与声明长度不一致。");
                    remaining -= result.BytesTransferred;
                }
                token.ThrowIfCancellationRequested();
                GetArchiveDestination(targetDir, entry.FullName);
                CommitArchiveFile(temporary, destination, config.OverwriteFiles);
            }
            finally { if (created) File.Delete(temporary); }
        }
    }
}
