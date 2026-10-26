using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 为旧异步 FTP 接口调用方转发可选目录能力。
/// </summary>
public static class FtpAsyncDirectoryExtensions
{
    /// <summary>
    /// 通过可选预算能力递归上传目录；旧接口实现无需增加成员。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="localDirectory">本地源根目录。</param>
    /// <param name="remoteDirectory">远程目标根目录。</param>
    /// <param name="limits">目录预算。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">各文件传输进度。</param>
    /// <param name="cancellationToken">取消操作。</param>
    /// <returns>目录传输汇总。</returns>
    public static Task<FtpDirectoryTransferResult> UploadDirectoryWithLimitsAsync(this IFtpAsyncClient client,
        string localDirectory, string remoteDirectory, FtpDirectoryTransferLimits limits,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error, IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default) =>
        RequireLimits(client).UploadDirectoryWithLimitsAsync(localDirectory, remoteDirectory, limits, conflictPolicy, progress, cancellationToken);

    /// <summary>
    /// 通过可选预算能力递归下载目录；不支持该能力的实现会明确失败。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="localDirectory">本地目标根目录。</param>
    /// <param name="remoteDirectory">远程源根目录。</param>
    /// <param name="limits">目录预算。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">各文件传输进度。</param>
    /// <param name="cancellationToken">取消操作。</param>
    /// <returns>目录传输汇总。</returns>
    public static Task<FtpDirectoryTransferResult> DownloadDirectoryWithLimitsAsync(this IFtpAsyncClient client,
        string localDirectory, string remoteDirectory, FtpDirectoryTransferLimits limits,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error, IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default) =>
        RequireLimits(client).DownloadDirectoryWithLimitsAsync(localDirectory, remoteDirectory, limits, conflictPolicy, progress, cancellationToken);

    /// <summary>
    /// 检查客户端的预算能力，不退回无预算传输。
    /// </summary>
    /// <returns>支持目录预算的实现。</returns>
    private static IFtpAsyncDirectoryLimitClient RequireLimits(IFtpAsyncClient client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        return client as IFtpAsyncDirectoryLimitClient ?? throw new NotSupportedException("此 FTP 客户端未实现可选目录预算能力。");
    }

    /// <summary>
    /// 获取异步 FTP 客户端的可选目录能力接口。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <returns>目录操作接口。</returns>
    /// <exception cref="NotSupportedException">客户端未实现目录能力。</exception>
    public static IFtpAsyncDirectoryClient DirectoryOperations(this IFtpAsyncClient client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        return client as IFtpAsyncDirectoryClient ??
               throw new NotSupportedException("此 FTP 客户端未实现可选目录能力。");
    }

    /// <summary>
    /// 获取远程目录的直接子项及可用元数据。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="remoteDirectory">远程目录路径。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>目录项列表。</returns>
    public static Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(this IFtpAsyncClient client,
        string remoteDirectory, CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).GetDirectoryEntriesAsync(remoteDirectory, cancellationToken);

    /// <summary>
    /// 获取远程文件的服务器校验值。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="algorithm">首选校验算法。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>服务器实际返回的算法和值。</returns>
    public static Task<FtpRemoteChecksum> GetFileChecksumAsync(this IFtpAsyncClient client, string remotePath,
        FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).GetFileChecksumAsync(remotePath, algorithm, cancellationToken);

    /// <summary>
    /// 递归上传本地目录。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="localDirectory">本地源目录。</param>
    /// <param name="remoteDirectory">远程目标目录。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">可选传输进度。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>目录传输结果。</returns>
    public static Task<FtpDirectoryTransferResult> UploadDirectoryAsync(this IFtpAsyncClient client,
        string localDirectory, string remoteDirectory, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).UploadDirectoryAsync(localDirectory, remoteDirectory, conflictPolicy, progress,
            cancellationToken);

    /// <summary>
    /// 递归下载远程目录。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="localDirectory">本地目标根目录。</param>
    /// <param name="remoteDirectory">远程源目录。</param>
    /// <param name="conflictPolicy">文件冲突策略。</param>
    /// <param name="progress">可选传输进度。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>目录传输结果。</returns>
    public static Task<FtpDirectoryTransferResult> DownloadDirectoryAsync(this IFtpAsyncClient client,
        string localDirectory, string remoteDirectory, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        IProgress<FtpProgress> progress = null, CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).DownloadDirectoryAsync(localDirectory, remoteDirectory, conflictPolicy, progress,
            cancellationToken);

    /// <summary>
    /// 在远程服务器上移动目录。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="remotePath">源目录路径。</param>
    /// <param name="destinationPath">目标目录路径。</param>
    /// <param name="conflictPolicy">目标冲突策略。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>移动成功时为 true。</returns>
    public static Task<bool> MoveDirectoryAsync(this IFtpAsyncClient client, string remotePath,
        string destinationPath, FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error,
        CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).MoveDirectoryAsync(remotePath, destinationPath, conflictPolicy, cancellationToken);

    /// <summary>
    /// 递归删除远程目录及其内容。
    /// </summary>
    /// <param name="client">异步 FTP 客户端。</param>
    /// <param name="remotePath">远程目录路径。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    public static Task DeleteDirectoryAsync(this IFtpAsyncClient client, string remotePath,
        CancellationToken cancellationToken = default) =>
        DirectoryOperations(client).DeleteDirectoryAsync(remotePath, cancellationToken);
}
