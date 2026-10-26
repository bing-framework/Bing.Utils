using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// FTP 目录遍历和传输前文件大小的预算。
/// </summary>
/// <remarks>各项默认无限制，非负且包含边界。超限停止后续传输，已创建目录和完成文件不会回滚。
/// 文件大小预算使用本地文件信息或服务器 listing，不能限制服务器谎报、传输中变更或重试造成的实际流量。</remarks>
public sealed class FtpDirectoryTransferLimits
{
    /// <summary>
    /// 获取或设置遍历到的普通文件数上限，包含因冲突而跳过的文件，不包含链接。
    /// </summary>
    public long? MaxFiles { get; set; }
    /// <summary>
    /// 获取或设置目录深度上限；根目录为零，零仍允许根目录中的文件。
    /// </summary>
    public int? MaxDepth { get; set; }
    /// <summary>
    /// 获取或设置获准传输文件的完整大小之和；续传也计完整大小，跳过的文件不计。
    /// </summary>
    /// <remarks>启用时大小未知或为负的文件会在传输前失败。失败传输仍占用本次操作预算。</remarks>
    public long? MaxTotalFileBytes { get; set; }

    /// <summary>
    /// 验证并固定一次操作的预算配置。
    /// </summary>
    /// <returns>独立配置快照。</returns>
    internal FtpDirectoryTransferLimits Snapshot()
    {
        if (MaxFiles < 0) throw new ArgumentOutOfRangeException(nameof(MaxFiles));
        if (MaxDepth < 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (MaxTotalFileBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxTotalFileBytes));
        return (FtpDirectoryTransferLimits)MemberwiseClone();
    }
}

/// <summary>
/// 在旧目录接口之外提供显式预算传输的可选能力。
/// </summary>
public interface IFtpAsyncDirectoryLimitClient
{
    /// <summary>
    /// 在遍历和文件传输前检查预算后递归上传目录。
    /// </summary>
    /// <param name="localDirectory">本地源根目录。</param>
    /// <param name="remoteDirectory">远程目标根目录。</param>
    /// <param name="limits">非 null 的预算配置，调用时复制。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">各文件的传输进度。</param>
    /// <param name="cancellationToken">取消遍历和传输。</param>
    /// <returns>已完成、跳过和失败的路径汇总。</returns>
    /// <remarks>
    /// 本地源根目录及其相关祖先必须由可信调用方控制，并在整个操作期间保持稳定，不被其他进程替换。
    /// 路径规范化和 reparse point 检查可防止静态链接导致的路径越界，但不提供基于句柄的安全隔离，无法抵御恶意本地并发替换。
    /// </remarks>
    Task<FtpDirectoryTransferResult> UploadDirectoryWithLimitsAsync(string localDirectory, string remoteDirectory,
        FtpDirectoryTransferLimits limits, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在遍历和文件传输前检查预算后递归下载目录。
    /// </summary>
    /// <param name="localDirectory">本地目标根目录。</param>
    /// <param name="remoteDirectory">远程源根目录。</param>
    /// <param name="limits">非 null 的预算配置，调用时复制。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">各文件的传输进度。</param>
    /// <param name="cancellationToken">取消遍历和传输。</param>
    /// <returns>已完成、跳过和失败的路径汇总。</returns>
    /// <remarks>
    /// 本地目标根目录及其相关祖先必须由可信调用方控制，并在整个操作期间保持稳定，不被其他进程替换。
    /// 路径规范化和 reparse point 检查可防止静态链接导致的路径越界，但不提供基于句柄的安全隔离，无法抵御恶意本地并发替换。
    /// </remarks>
    Task<FtpDirectoryTransferResult> DownloadDirectoryWithLimitsAsync(string localDirectory, string remoteDirectory,
        FtpDirectoryTransferLimits limits, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default);
}

public sealed partial class AsyncFtpClient : IFtpAsyncDirectoryLimitClient
{
    /// <inheritdoc />
    public Task<FtpDirectoryTransferResult> UploadDirectoryWithLimitsAsync(string localDirectory, string remoteDirectory,
        FtpDirectoryTransferLimits limits, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default)
    {
        var budget = new DirectoryBudget((limits ?? throw new ArgumentNullException(nameof(limits))).Snapshot());
        ValidatePath(localDirectory, nameof(localDirectory)); ValidatePath(remoteDirectory, nameof(remoteDirectory));
        ValidateConflictPolicy(conflictPolicy);
        var root = Path.GetFullPath(localDirectory);
        if (IsExistingReparsePoint(root)) throw new IOException("本地源根目录不能是 reparse point。");
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        return ExecuteAsync(token => UploadDirectoryCoreAsync(root, remoteDirectory, conflictPolicy, progress, token, budget), cancellationToken);
    }

    /// <inheritdoc />
    public Task<FtpDirectoryTransferResult> DownloadDirectoryWithLimitsAsync(string localDirectory, string remoteDirectory,
        FtpDirectoryTransferLimits limits, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default)
    {
        var budget = new DirectoryBudget((limits ?? throw new ArgumentNullException(nameof(limits))).Snapshot());
        ValidatePath(localDirectory, nameof(localDirectory)); ValidatePath(remoteDirectory, nameof(remoteDirectory));
        ValidateConflictPolicy(conflictPolicy);
        var root = Path.GetFullPath(localDirectory);
        if (IsExistingReparsePoint(root)) throw new IOException("本地目标根目录不能是 reparse point。");
        if (File.Exists(root)) throw new IOException("本地目标根路径是文件。");
        return ExecuteAsync(token => DownloadDirectoryCoreAsync(root, remoteDirectory, conflictPolicy, progress, token, budget), cancellationToken);
    }

    /// <summary>
    /// 一次串行目录操作的累计预算，不跨调用共享。
    /// </summary>
    private sealed class DirectoryBudget
    {
        /// <summary>
        /// 不可变的操作配置快照。
        /// </summary>
        private readonly FtpDirectoryTransferLimits _limits;
        /// <summary>
        /// 已枚举的普通文件数。
        /// </summary>
        private long _files;
        /// <summary>
        /// 已允许传输的文件完整大小之和。
        /// </summary>
        private long _bytes;
        /// <summary>
        /// 初始化一次操作的预算。
        /// </summary>
        internal DirectoryBudget(FtpDirectoryTransferLimits limits) => _limits = limits;
        /// <summary>
        /// 在创建子目录或递归之前拒绝过深路径。
        /// </summary>
        internal void CheckDepth(int depth, string path)
        {
            if (_limits.MaxDepth.HasValue && depth > _limits.MaxDepth.Value)
                throw new InvalidDataException("FTP 目录超过深度预算：" + path);
        }
        /// <summary>
        /// 在处理普通文件之前消耗一个遍历名额。
        /// </summary>
        internal void CountFile(string path)
        {
            if (_limits.MaxFiles.HasValue && _files >= _limits.MaxFiles.Value)
                throw new InvalidDataException("FTP 目录超过文件数预算：" + path);
            _files++;
        }
        /// <summary>
        /// 在启动传输之前核验文件完整大小和剩余预算。
        /// </summary>
        internal void ReserveFile(long? length, string path)
        {
            if (!_limits.MaxTotalFileBytes.HasValue) return;
            if (!length.HasValue || length < 0)
                throw new InvalidDataException("启用 FTP 大小预算时文件大小必须已知：" + path);
            if (length.Value > _limits.MaxTotalFileBytes.Value - _bytes)
                throw new InvalidDataException("FTP 目录超过累计文件大小预算：" + path);
            _bytes += length.Value;
        }
    }
}
