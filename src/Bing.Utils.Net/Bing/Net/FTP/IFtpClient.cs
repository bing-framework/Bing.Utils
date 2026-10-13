namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端
/// </summary>
public interface IFtpClient : IDisposable
{
    /// <summary>
    /// 获取是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接
    /// </summary>
    void Connect();

    /// <summary>
    /// 关闭连接
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DeleteFile(string path);

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool RenameFile(string path, string dest);

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="destinationFile">目标文件路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool CopyFile(string file, string destinationFile);

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="destinationFile">目标文件路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool MoveFile(string file, string destinationFile);

    /// <summary>
    /// 判断当前目录下指定的文件是否存在
    /// </summary>
    /// <param name="remoteFileName">远程文件名</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool FileExists(string remoteFileName);

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    /// <returns>当前目录下的文件路径列表。</returns>
    List<string> GetFiles();

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>当前目录下的文件路径列表。</returns>
    List<string> GetFiles(string path);

    /// <summary>
    /// 上传文件流
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool UploadStream(Stream stream, string remotePath);

    /// <summary>
    /// 上传字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool UploadBytes(byte[] bytes, string remotePath);

    /// <summary>
    /// 上传文件夹
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remoteDir">远程目录</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool UploadDirectory(string localDir, string remoteDir);

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="localPath">本地路径</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool UploadFile(string localPath, string remotePath);

    /// <summary>
    /// 批量上传文件
    /// </summary>
    /// <param name="localPaths">本地路径列表</param>
    /// <param name="remoteDir">远程目录</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool UploadFiles(IEnumerable<string> localPaths, string remoteDir);

    /// <summary>
    /// 下载文件流
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DownloadStream(Stream stream, string remotePath);

    /// <summary>
    /// 下载字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DownloadBytes(out byte[] bytes, string remotePath);

    /// <summary>
    /// 下载文件夹
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remoteDir">远程目录</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DownloadDirectory(string localDir, string remoteDir);

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="localPath">本地路径</param>
    /// <param name="remotePath">远程路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DownloadFile(string localPath, string remotePath);

    /// <summary>
    /// 批量下载文件
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remotePaths">远程路径列表</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DownloadFiles(string localDir, IEnumerable<string> remotePaths);

    /// <summary>
    /// 创建文件夹，以递归的方式将缺失的文件夹创建到路径中。例如：/fold1/fold2
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool CreateDirectory(string path);

    /// <summary>
    /// 删除文件夹，不递归
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DeleteDirectory(string path);

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="deleteRecursively">是否递归删除</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DeleteDirectory(string path, bool deleteRecursively);

    /// <summary>
    /// 设置当前文件夹
    /// </summary>
    /// <param name="newDirPath">新目录路径</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    void SetCurrentDirectory(string newDirPath);

    /// <summary>
    /// 获取当前文件夹的路径
    /// </summary>
    /// <returns>当前目录路径。</returns>
    string GetCurrentDirectory();

    /// <summary>
    /// 重命名文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <exception cref="FtpClientException"></exception>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool RenameDirectory(string path, string dest);

    /// <summary>
    /// 移动文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <exception cref="FtpClientException"></exception>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool MoveDirectory(string path, string dest);

    /// <summary>
    /// 判断当前目录下指定的子目录是否存在
    /// </summary>
    /// <param name="remoteDirPath">指定的目录名</param>
    /// <returns>操作成功时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    bool DirectoryExists(string remoteDirPath);

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    /// <returns>当前目录下的目录路径数组。</returns>
    string[] GetDirectories();

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    /// <param name="path">指定的目录</param>
    /// <exception cref="FtpClientException"></exception>
    /// <returns>当前目录下的目录路径数组。</returns>
    string[] GetDirectories(string path);

}
