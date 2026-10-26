using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 为异步 FTP 客户端提供目录元数据、递归传输和目录管理能力。
/// </summary>
/// <remarks>此接口是可选能力，不会成为旧 IFtpAsyncClient 实现的必需成员。</remarks>
public interface IFtpAsyncDirectoryClient
{
    /// <summary>
    /// 获取远程目录的直接子项及可用元数据。
    /// </summary>
    /// <param name="remoteDirectory">远程目录路径。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>目录项列表；大小或修改时间不可用时对应值为 null。</returns>
    Task<IReadOnlyList<FtpDirectoryEntry>> GetDirectoryEntriesAsync(string remoteDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取远程文件的服务器校验值。
    /// </summary>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="algorithm">首选算法；None 表示使用服务器首个可用算法。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>服务器实际返回的算法和值。</returns>
    /// <exception cref="System.NotSupportedException">服务器未提供可用校验算法。</exception>
    Task<FtpRemoteChecksum> GetFileChecksumAsync(string remotePath,
        FtpHashAlgorithm algorithm = FtpHashAlgorithm.NONE, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归上传本地目录，跳过本地 reparse point。
    /// </summary>
    /// <param name="localDirectory">本地源目录。</param>
    /// <param name="remoteDirectory">远程目标目录。</param>
    /// <param name="conflictPolicy">文件冲突策略；目录按合并处理。</param>
    /// <param name="progress">可选文件传输进度。</param>
    /// <param name="cancellationToken">取消遍历和传输的令牌。</param>
    /// <returns>成功、跳过和失败路径汇总。</returns>
    /// <remarks>
    /// 本地源根目录及其相关祖先必须由可信调用方控制，并在整个操作期间保持稳定，不被其他进程替换。
    /// 路径规范化和 reparse point 检查可防止静态链接导致的路径越界，但不提供基于句柄的安全隔离，无法抵御恶意本地并发替换。
    /// </remarks>
    Task<FtpDirectoryTransferResult> UploadDirectoryAsync(string localDirectory, string remoteDirectory,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error, IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归下载远程目录，拒绝越界名称并跳过链接。
    /// </summary>
    /// <param name="localDirectory">本地目标根目录。</param>
    /// <param name="remoteDirectory">远程源目录。</param>
    /// <param name="conflictPolicy">文件冲突策略；目录按合并处理。</param>
    /// <param name="progress">可选文件传输进度。</param>
    /// <param name="cancellationToken">取消遍历和传输的令牌。</param>
    /// <returns>成功、跳过和失败路径汇总。</returns>
    /// <remarks>
    /// 本地目标根目录及其相关祖先必须由可信调用方控制，并在整个操作期间保持稳定，不被其他进程替换。
    /// 路径规范化和 reparse point 检查可防止静态链接导致的路径越界，但不提供基于句柄的安全隔离，无法抵御恶意本地并发替换。
    /// </remarks>
    Task<FtpDirectoryTransferResult> DownloadDirectoryAsync(string localDirectory, string remoteDirectory,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error, IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在远程服务器上移动目录。
    /// </summary>
    /// <param name="remotePath">源目录路径。</param>
    /// <param name="destinationPath">目标目录路径。</param>
    /// <param name="conflictPolicy">目标冲突策略；Resume 不适用于目录。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>移动成功时为 true；Skip 遇到目标时为 false。</returns>
    Task<bool> MoveDirectoryAsync(string remotePath, string destinationPath,
        FtpConflictPolicy conflictPolicy = FtpConflictPolicy.Error, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归删除远程目录及其内容。
    /// </summary>
    /// <param name="remotePath">远程目录路径。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    Task DeleteDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);
}
