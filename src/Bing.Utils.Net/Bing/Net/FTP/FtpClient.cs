using System.Net;
using System.Text;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端
/// </summary>
public class FtpClient : IFtpClient
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
    /// 初始化一个<see cref="FtpClient"/>类型的实例
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
    /// 初始化一个<see cref="FtpClient"/>类型的实例
    /// </summary>
    /// <param name="config">Ftp客户端配置</param>
    public FtpClient(FtpClientConfig config)
    {
        _config = config;

        var credentials = new NetworkCredential(config.UserName, config.Password);
        _client = new FluentFTP.FtpClient(config.Host, credentials, config.Port);
        _client.Config.EncryptionMode = Convert(config.EncryptionType);
        _client.Config.RetryAttempts = RETRY_ATTEMPTS_NUMBER;
        _client.Encoding = Encoding.GetEncoding(config.CharsetName);
        if (config.EnableSsl)
            _client.ValidateCertificate += Client_ValidateCertificate;
    }

    /// <summary>
    /// 转换为FTP加密模式
    /// </summary>
    /// <param name="type">加密方式</param>
    private FtpEncryptionMode Convert(EncryptionType type)
    {
        switch (type)
        {
            case EncryptionType.None:
                return FtpEncryptionMode.None;
            case EncryptionType.Implicit:
                return FtpEncryptionMode.Implicit;
            case EncryptionType.Explicit:
                return FtpEncryptionMode.Explicit;
            default:
                return FtpEncryptionMode.None;
        }
    }

    /// <summary>
    /// 客户端的证书验证
    /// </summary>
    private void Client_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
    {
        e.Accept = true;
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => (_client?.IsConnected ?? false) && (_client?.IsAuthenticated ?? false);

    /// <summary>
    /// 连接
    /// </summary>
    /// <exception cref="FtpClientException"></exception>
    public void Connect()
    {
        try
        {
            if (!_client.IsConnected)
                _client.Connect();
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
        if (_client.IsConnected)
            _client.Disconnect();
    }

    #region File(文件操作)

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path">路径</param>
    public bool DeleteFile(string path)
    {
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
        try
        {
            _client.Rename(path,dest);
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
        return _client.FileExists(remoteFileName);
    }

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    public List<string> GetFiles()
    {
        return GetFiles(GetCurrentDirectory());
    }

    /// <summary>
    /// 获取当前目录下文件列表（仅文件）
    /// </summary>
    /// <param name="path">路径</param>
    /// <exception cref="FtpClientException"></exception>
    public List<string> GetFiles(string path)
    {
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
        try
        {
            var status = _client.UploadStream(stream, remotePath, FtpRemoteExists.Overwrite, true);
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
        try
        {
            var status = _client.UploadBytes(bytes, remotePath, FtpRemoteExists.Overwrite, true);
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
        try
        {
            var result = _client.UploadDirectory(localDir, remoteDir);
            return true;
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
        try
        {
            long remoteFileLength = 0;
            var status = _client.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, true, FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw);
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
        if (!localPaths.Any())
            return false;
        try
        {
            var result = _client.UploadFiles(localPaths, remoteDir, FtpRemoteExists.Overwrite, true, FtpVerify.Retry);
            return localPaths.Count() == result;
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
        long remoteFileLength = 0;
        try
        {
            _client.DownloadStream(stream, remotePath);
            remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (stream.Length > 0)
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
        long remoteFileLength = 0;
        try
        {
            _client.DownloadBytes(out bytes, remotePath);
            remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (bytes.Length > 0)
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
        try
        {
            var result = _client.DownloadDirectory(localDir, remoteDir);
            return true;
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
        long remoteFileLength = 0;
        try
        {
            var status = _client.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw);
            if (status != FtpStatus.Success)
                return false;
            remoteFileLength = _client.GetFileSize(remotePath);
        }
        catch (FluentFTP.FtpException e)
        {
            throw new FtpClientException($"下载文件 操作时出错: {(e.InnerException != null ? e.InnerException.Message : e.Message)}", e);
        }

        if (File.Exists(localPath))
        {
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
        if (!remotePaths.Any())
            return false;
        try
        {
            var result = _client.DownloadFiles(localDir, remotePaths, FtpLocalExists.Overwrite, FtpVerify.Retry);
            return remotePaths.Count() == result;
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
        if (_client.DirectoryExists(newDirPath))
            _client.SetWorkingDirectory(newDirPath);
        else
            throw new DirectoryNotFoundException($"设置目录 操作引发错误，找不到路径 {newDirPath}");
    }

    /// <summary>
    /// 获取当前文件夹的路径
    /// </summary>
    public string GetCurrentDirectory()
    {
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
        return _client.DirectoryExists(remoteDirPath);
    }

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    public string[] GetDirectories()
    {
        return GetDirectories(GetCurrentDirectory());
    }

    /// <summary>
    /// 获取当前目录下所有的文件夹列表（仅文件夹）
    /// </summary>
    /// <param name="path">指定的目录</param>
    /// <exception cref="FtpClientException"></exception>
    public string[] GetDirectories(string path)
    {
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
        if (_client != null)
        {
            _client.Dispose();
            _client = null;
        }
    }
}