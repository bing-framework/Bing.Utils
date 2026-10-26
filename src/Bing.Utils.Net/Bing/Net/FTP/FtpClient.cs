using System.Net;
using System.Net.Security;
using System.Text;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端
/// </summary>
public partial class FtpClient : IFtpClient
{
    /// <summary>
    /// 重试次数
    /// </summary>
    private const int RETRY_ATTEMPTS_NUMBER = 3;

    /// <summary>
    /// FluentFTP客户端
    /// </summary>
    private FluentFTP.FtpClient _client;

    /// <summary>
    /// FTP客户端配置
    /// </summary>
    private FtpClientConfig _config;

    /// <summary>
    /// 初始化一个 <see cref="FtpClient"/> 类型的实例。
    /// </summary>
    /// <param name="host">主机</param>
    /// <param name="port">端口</param>
    /// <param name="userName">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="enableSsl">启用SSL</param>
    /// <param name="encryptionType">加密方式</param>
    public FtpClient(string host, int port, string userName, string password, bool enableSsl, EncryptionType encryptionType)
        : this(new FtpClientConfig { Host = host, Port = port, UserName = userName, Password = password, EnableSsl = enableSsl, EncryptionType = encryptionType })
    {
    }

    /// <summary>
    /// 初始化一个 <see cref="FtpClient"/> 类型的实例。
    /// </summary>
    /// <param name="config">Ftp客户端配置</param>
    public FtpClient(FtpClientConfig config)
        : this(config, FtpConfigurationMode.Legacy)
    {
    }

    /// <summary>
    /// 按配置策略验证 FTPS 服务器证书，默认只接受无策略错误的证书。
    /// </summary>
    /// <param name="control">触发验证的 FTP 控制客户端。</param>
    /// <param name="e">包含证书、证书链和策略错误的验证参数。</param>
    /// <exception cref="Exception">自定义证书策略抛出异常时，拒绝证书并传播该异常。</exception>
    private void Client_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
    {
        try
        {
            var policy = _config.SslCertificateValidationPolicy;
            e.Accept = policy == null ? e.PolicyErrors == SslPolicyErrors.None : policy(e);
        }
        catch
        {
            e.Accept = false;
            throw;
        }
    }

    /// <summary>
    /// 获取是否已连接
    /// </summary>
    public bool IsConnected => (_client?.IsConnected ?? false) && (_client?.IsAuthenticated ?? false);

    /// <summary>
    /// 连接；完整配置模式下，显式连接不会因 <see cref="FtpClientConfig.KeepAlive"/> 为 false 而立即断开。
    /// </summary>
    /// <exception cref="FtpClientException"></exception>
    public void Connect()
    {
        using var operation = BeginOperation(true, false);
        try
        {
            EnsureConnectedCore();
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"发生连接错误: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Disconnect()
    {
        using var operation = BeginOperation(false, false);
        DisconnectCore();
    }

    #region File(文件操作)

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">路径</param>
    public bool DeleteFile(string path)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        try
        {
            _client.DeleteFile(path);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"删除文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
        return !FileExists(path);
    }

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool RenameFile(string path, string dest)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        dest = GetRemotePath(dest);
        try
        {
            _client.Rename(path, dest);
            return _client.FileExists(dest);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"重命名文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="destinationFile">目标文件路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool CopyFile(string file, string destinationFile)
    {
        using var operation = BeginOperation();
        file = GetRemotePath(file);
        destinationFile = GetRemotePath(destinationFile);
        bool result = false;
        try
        {
            if (_client.FileExists(file))
            {
                using var stream = new MemoryStream();
                _client.DownloadStream(stream, file);
                result = _client.UploadStream(stream, destinationFile, FtpRemoteExists.Overwrite, true) == FtpStatus.Success;
            }
            return result;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"复制文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="destinationFile">目标文件路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool MoveFile(string file, string destinationFile)
    {
        using var operation = BeginOperation();
        file = GetRemotePath(file);
        destinationFile = GetRemotePath(destinationFile);
        bool result = false;
        try
        {
            if (_client.FileExists(file))
                result = _client.MoveFile(file, destinationFile, FtpRemoteExists.Overwrite);
            return result;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"移动文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 判断当前目录下指定的文件是否存在
    /// </summary>
    /// <param name="remoteFileName">远程文件名</param>
    public bool FileExists(string remoteFileName)
    {
        using var operation = BeginOperation();
        remoteFileName = GetRemotePath(remoteFileName);
        return _client.FileExists(remoteFileName);
    }

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    public List<string> GetFiles()
    {
        using var operation = BeginOperation();
        return GetFiles(GetCurrentDirectory());
    }

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    /// <param name="path">路径</param>
    /// <exception cref="FtpClientException"></exception>
    public List<string> GetFiles(string path)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        var files = Enumerable.Empty<string>().ToList();
        try
        {
            var result = _client.GetListing(path).ToList();
            if (result.Count > 0)
                files = result.Where(x => x.Type == FtpObjectType.File).Select(x => x.Name).ToList();
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"获取文件列表 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        return files;
    }

    #endregion

    #region Upload(上传)

    /// <summary>
    /// 上传文件流
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="remotePath">远程路径</param>
    public bool UploadStream(Stream stream, string remotePath)
    {
        using var operation = BeginOperation();
        remotePath = GetRemotePath(remotePath);
        try
        {
            var status = _client.UploadStream(stream, remotePath, FtpRemoteExists.Overwrite, true);
            if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                return status == FtpStatus.Success;
            long remoteFileLength = 0;
            if (status == FtpStatus.Success && _client.FileExists(remotePath))
                remoteFileLength = _client.GetFileSize(remotePath);
            else
                return false;
            if (remoteFileLength != stream.Length)
            {
                _client.DeleteFile(remotePath);
                return false;
            }
            return true;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"上传文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 上传字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="remotePath">远程路径</param>
    public bool UploadBytes(byte[] bytes, string remotePath)
    {
        using var operation = BeginOperation();
        remotePath = GetRemotePath(remotePath);
        try
        {
            var status = _client.UploadBytes(bytes, remotePath, FtpRemoteExists.Overwrite, true);
            if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                return status == FtpStatus.Success;
            long remoteFileLength = 0;
            if (status == FtpStatus.Success && _client.FileExists(remotePath))
                remoteFileLength = _client.GetFileSize(remotePath);
            else
                return false;
            if (remoteFileLength != bytes.Length)
            {
                _client.DeleteFile(remotePath);
                return false;
            }
            return true;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"上传文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 上传文件夹
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remoteDir">远程目录</param>
    public bool UploadDirectory(string localDir, string remoteDir)
    {
        using var operation = BeginOperation();
        localDir = GetLocalPath(localDir);
        remoteDir = GetRemotePath(remoteDir);
        try
        {
            var result = _configurationMode == FtpConfigurationMode.Complete && !_config.UseBinary
                ? _client.UploadDirectory(localDir, remoteDir, verifyOptions: FtpVerify.None)
                : _client.UploadDirectory(localDir, remoteDir);
            return _configurationMode != FtpConfigurationMode.Complete || TransfersSucceeded(result);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"上传文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="localPath">本地路径</param>
    /// <param name="remotePath">远程路径</param>
    public bool UploadFile(string localPath, string remotePath)
    {
        using var operation = BeginOperation();
        localPath = GetLocalPath(localPath);
        remotePath = GetRemotePath(remotePath);
        try
        {
            long remoteFileLength = 0;
            var status = _client.UploadFile(
                localPath,
                remotePath,
                FtpRemoteExists.Overwrite,
                true,
                GetTransferVerification(_configurationMode, _config.UseBinary, FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw));
            if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                return status == FtpStatus.Success;
            if (status == FtpStatus.Success && _client.FileExists(remotePath))
                remoteFileLength = _client.GetFileSize(remotePath);
            else
                return false;
            if (remoteFileLength != new FileInfo(localPath).Length)
            {
                _client.DeleteFile(remotePath);
                return false;
            }
            return true;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"上传文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 批量上传文件
    /// </summary>
    /// <param name="localPaths">本地路径列表</param>
    /// <param name="remoteDir">远程目录</param>
    public bool UploadFiles(IEnumerable<string> localPaths, string remoteDir)
    {
        using var operation = BeginOperation(false);
        var resolvedLocalPaths = _configurationMode == FtpConfigurationMode.Complete
            ? localPaths.Select(GetLocalPath).ToList()
            : null;
        if (resolvedLocalPaths != null ? resolvedLocalPaths.Count == 0 : !localPaths.Any())
            return false;
        if (_configurationMode == FtpConfigurationMode.Complete)
            EnsureConnectedCore();
        remoteDir = GetRemotePath(remoteDir);
        try
        {
            var result = _client.UploadFiles(
                resolvedLocalPaths ?? localPaths,
                remoteDir,
                FtpRemoteExists.Overwrite,
                true,
                GetTransferVerification(_configurationMode, _config.UseBinary, FtpVerify.Retry));
            var sourceCount = resolvedLocalPaths?.Count ?? localPaths.Count();
            return sourceCount == result;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"上传文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    #endregion

    #region Download(下载)

    /// <summary>
    /// 下载文件流
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="remotePath">远程路径</param>
    public bool DownloadStream(Stream stream, string remotePath)
    {
        using var operation = BeginOperation();
        remotePath = GetRemotePath(remotePath);
        long remoteFileLength = 0;
        try
        {
            var status = _client.DownloadStream(stream, remotePath);
            if (!ShouldAcceptDownloadStatus(_configurationMode, status))
                return false;
            if (ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
        {
            if (_client.FileExists(remotePath))
                return true;
        }
        else if (stream.Length > 0)
        {
            if (remoteFileLength != stream.Length)
                throw new FtpClientException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
            return true;
        }

        throw new FileNotFoundException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
    }

    /// <summary>
    /// 下载字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="remotePath">远程路径</param>
    public bool DownloadBytes(out byte[] bytes, string remotePath)
    {
        using var operation = BeginOperation();
        remotePath = GetRemotePath(remotePath);
        long remoteFileLength = 0;
        try
        {
            var status = _client.DownloadBytes(out bytes, remotePath);
            if (!ShouldAcceptDownloadStatus(_configurationMode, status))
                return false;
            if (ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
        {
            if (_client.FileExists(remotePath))
                return true;
        }
        else if (bytes.Length > 0)
        {
            if (remoteFileLength != bytes.Length)
                throw new FtpClientException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
            return true;
        }

        throw new FileNotFoundException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
    }

    /// <summary>
    /// 下载文件夹
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remoteDir">远程目录</param>
    public bool DownloadDirectory(string localDir, string remoteDir)
    {
        using var operation = BeginOperation();
        localDir = GetLocalPath(localDir);
        remoteDir = GetRemotePath(remoteDir);
        try
        {
            var result = _configurationMode == FtpConfigurationMode.Complete && !_config.UseBinary
                ? _client.DownloadDirectory(localDir, remoteDir, verifyOptions: FtpVerify.None)
                : _client.DownloadDirectory(localDir, remoteDir);
            return _configurationMode != FtpConfigurationMode.Complete || TransfersSucceeded(result);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="localPath">本地路径</param>
    /// <param name="remotePath">远程路径</param>
    public bool DownloadFile(string localPath, string remotePath)
    {
        using var operation = BeginOperation();
        localPath = GetLocalPath(localPath);
        remotePath = GetRemotePath(remotePath);
        long remoteFileLength = 0;
        try
        {
            var status = _client.DownloadFile(
                localPath,
                remotePath,
                FtpLocalExists.Overwrite,
                GetTransferVerification(_configurationMode, _config.UseBinary, FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw));
            if (status != FtpStatus.Success)
                return false;
            if (ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (File.Exists(localPath))
        {
            if (!ShouldCheckTransferLength(_configurationMode, _config.UseBinary))
                return true;
            var localFileInfo = new FileInfo(localPath);
            if (remoteFileLength != localFileInfo.Length)
            {
                File.Delete(localPath);
                throw new FtpClientException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
            }

            return true;
        }

        throw new FileNotFoundException($"对文件 {remotePath} 执行 下载文件 操作时出错，操作不成功");
    }

    /// <summary>
    /// 批量下载文件
    /// </summary>
    /// <param name="localDir">本地目录</param>
    /// <param name="remotePaths">远程路径列表</param>
    public bool DownloadFiles(string localDir, IEnumerable<string> remotePaths)
    {
        using var operation = BeginOperation(false);
        var resolvedRemotePaths = _configurationMode == FtpConfigurationMode.Complete
            ? remotePaths.Select(GetRemotePath).ToList()
            : null;
        if (resolvedRemotePaths != null ? resolvedRemotePaths.Count == 0 : !remotePaths.Any())
            return false;
        if (_configurationMode == FtpConfigurationMode.Complete)
            EnsureConnectedCore();
        localDir = GetLocalPath(localDir);
        try
        {
            var result = _client.DownloadFiles(
                localDir,
                resolvedRemotePaths ?? remotePaths,
                FtpLocalExists.Overwrite,
                GetTransferVerification(_configurationMode, _config.UseBinary, FtpVerify.Retry));
            var sourceCount = resolvedRemotePaths?.Count ?? remotePaths.Count();
            return sourceCount == result;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    #endregion

    #region Directory(目录操作)

    /// <summary>
    /// 创建文件夹，以递归的方式将缺失的文件夹创建到路径中。例如：/fold1/fold2
    /// </summary>
    /// <param name="path">路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool CreateDirectory(string path)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        try
        {
            return _client.CreateDirectory(path, true);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"创建目录 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 删除文件夹，不递归
    /// </summary>
    /// <param name="path">路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool DeleteDirectory(string path)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        try
        {
            var items = _client.GetListing(path).Where(x => x.Type == FtpObjectType.Directory).ToList();
            if (items.Count == 0)
                _client.DeleteDirectory(path);
            else
                throw new FtpClientException($"删除文件夹时出错 {path}，文件夹不为空");
            return !_client.DirectoryExists(path);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"删除文件夹 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="deleteRecursively">是否递归删除</param>
    /// <exception cref="FtpClientException"></exception>
    public bool DeleteDirectory(string path, bool deleteRecursively)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        if (deleteRecursively)
        {
            try
            {
                var items = _client.GetListing(path).Where(x => x.Type == FtpObjectType.Directory).ToList();
                if (items.Count == 0)
                {
                    _client.DeleteDirectory(path);
                }
                else
                {
                    foreach (var item in items.Where(x => x.Type == FtpObjectType.Directory))
                        DeleteDirectory(item.FullName, true);
                    _client.DeleteDirectory(path);
                }
            }
            catch (FluentFTP.FtpException e)
            {
                throw new FtpClientException($"删除文件夹 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
            }
        }
        else
        {
            DeleteDirectory(path);
        }

        return !_client.DirectoryExists(path);
    }

    /// <summary>
    /// 设置当前文件夹
    /// </summary>
    /// <param name="newDirPath">新目录路径</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public void SetCurrentDirectory(string newDirPath)
    {
        using var operation = BeginOperation();
        newDirPath = GetRemotePath(newDirPath);
        if (_client.DirectoryExists(newDirPath))
        {
            _client.SetWorkingDirectory(newDirPath);
            SetRemoteDirectoryBase(newDirPath);
        }
        else
            throw new DirectoryNotFoundException($"设置目录 操作引发错误，找不到路径 {newDirPath}");
    }

    /// <summary>
    /// 获取当前文件夹的路径；完整模式返回相对路径解析基准，历史模式查询服务器工作目录。
    /// </summary>
    public string GetCurrentDirectory()
    {
        if (_configurationMode == FtpConfigurationMode.Complete)
        {
            using var scope = BeginOperation(false, false);
            return _remoteDirectory;
        }
        using var operation = BeginOperation();
        return _client.GetWorkingDirectory();
    }

    /// <summary>
    /// 重命名文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool RenameDirectory(string path, string dest)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        dest = GetRemotePath(dest);
        try
        {
            _client.Rename(path, dest);
            return _client.DirectoryExists(dest);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"重命名文件夹 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 移动文件夹
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dest">目标路径</param>
    /// <exception cref="FtpClientException"></exception>
    public bool MoveDirectory(string path, string dest)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        dest = GetRemotePath(dest);
        bool result = false;
        try
        {
            if (_client.DirectoryExists(path))
                result = _client.MoveDirectory(path, dest, FtpRemoteExists.Overwrite);
            return result;
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"移动文件夹 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
    }

    /// <summary>
    /// 判断当前目录下指定的子目录是否存在
    /// </summary>
    /// <param name="remoteDirPath">指定的目录名</param>
    public bool DirectoryExists(string remoteDirPath)
    {
        using var operation = BeginOperation();
        remoteDirPath = GetRemotePath(remoteDirPath);
        return _client.DirectoryExists(remoteDirPath);
    }

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    public string[] GetDirectories()
    {
        using var operation = BeginOperation();
        return GetDirectories(GetCurrentDirectory());
    }

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    /// <param name="path">指定的目录</param>
    /// <exception cref="FtpClientException"></exception>
    public string[] GetDirectories(string path)
    {
        using var operation = BeginOperation();
        path = GetRemotePath(path);
        string[] directories = null;

        try
        {
            var result = _client.GetListing(path).ToList();
            if (result.Count > 0)
                directories = result.Where(x => x.Type == FtpObjectType.Directory).Select(x => x.Name).ToArray();
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"获取文件夹列表 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }
        return directories;
    }

    #endregion

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        using var operation = BeginOperation(false, false);
        if (_client != null)
        {
            _client.Dispose();
            _client = null;
        }
    }
}
