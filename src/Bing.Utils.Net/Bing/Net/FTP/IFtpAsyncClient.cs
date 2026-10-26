using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 提供带取消和进度报告的异步 FTP 操作。
/// </summary>
/// <remarks>同一实例内的 FTP 命令会串行执行；在等待队列时取消不会打断当前操作。</remarks>
public interface IFtpAsyncClient : IDisposable
{
    /// <summary>
    /// 获取底层 FTP 连接是否已连接并完成认证。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 异步连接并认证 FTP 服务器。
    /// </summary>
    /// <param name="cancellationToken">用于取消连接的令牌。</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步断开 FTP 服务器连接。
    /// </summary>
    /// <param name="cancellationToken">用于取消断开的令牌。</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查远程文件是否存在。
    /// </summary>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>文件存在时为 true。</returns>
    Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查远程目录是否存在。
    /// </summary>
    /// <param name="remotePath">远程目录路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>目录存在时为 true。</returns>
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步枚举远程目录中的文件名。
    /// </summary>
    /// <param name="remoteDirectory">远程目录路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>目录中的文件名列表。</returns>
    Task<IReadOnlyList<string>> GetFilesAsync(string remoteDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步枚举远程目录中的子目录名。
    /// </summary>
    /// <param name="remoteDirectory">远程目录路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>目录中的子目录名列表。</returns>
    Task<IReadOnlyList<string>> GetDirectoriesAsync(string remoteDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步上传本地文件；远程同名文件会被覆盖。
    /// </summary>
    /// <param name="localPath">本地文件路径。</param>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="progress">可选传输进度接收器。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>传输成功时为 true；跳过或失败状态返回 false。</returns>
    Task<bool> UploadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步下载远程文件；本地同名文件会被覆盖。
    /// </summary>
    /// <param name="localPath">本地目标文件路径。</param>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="progress">可选传输进度接收器。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>传输成功时为 true；跳过或失败状态返回 false。</returns>
    Task<bool> DownloadFileAsync(
        string localPath,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步上传流；远程同名文件会被覆盖。
    /// </summary>
    /// <remarks>流由调用方拥有。操作期间调用方应保持流可读，客户端不会关闭该流。</remarks>
    /// <param name="source">可读取的源流。</param>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="progress">可选传输进度接收器。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>传输成功时为 true。</returns>
    Task<bool> UploadStreamAsync(
        Stream source,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步将远程文件写入目标流。
    /// </summary>
    /// <remarks>流由调用方拥有。操作期间调用方应保持流可写，客户端不会关闭该流。</remarks>
    /// <param name="destination">可写的目标流。</param>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="progress">可选传输进度接收器。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>传输成功时为 true。</returns>
    Task<bool> DownloadStreamAsync(
        Stream destination,
        string remotePath,
        IProgress<FtpProgress> progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除远程文件。
    /// </summary>
    /// <param name="remotePath">远程文件路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步递归创建远程目录。
    /// </summary>
    /// <param name="remotePath">远程目录路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>目录创建成功时为 true。</returns>
    Task<bool> CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步移动远程文件；目标同名文件会被覆盖。
    /// </summary>
    /// <param name="remotePath">源文件路径。</param>
    /// <param name="destinationPath">目标文件路径。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>文件移动成功时为 true。</returns>
    Task<bool> MoveFileAsync(string remotePath, string destinationPath, CancellationToken cancellationToken = default);
}
