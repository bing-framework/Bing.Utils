namespace Bing.Utils.Net.Tests.Integration;

/// <summary>
/// 验证 FTP 客户端与受控 FTP 服务的文件、目录和流操作。
/// </summary>
[Trait("Category", "Integration")]
public class FtpClientIntegrationTest
{
    /// <summary>
    /// 测试资源目录中 FTP 上传用文件的相对路径。
    /// </summary>
    private const string TestFilesPath = "Resources/TestFiles";
    /// <summary>
    /// 测试专用本地工作目录的相对路径。
    /// </summary>
    private const string ActiveFilePath = "Resources/ActiveTestFolder";
    /// <summary>
    /// FTP 文件操作测试使用的第一个资源文件名。
    /// </summary>
    private const string TestFile1 = "Text.txt";
    /// <summary>
    /// FTP 文件操作测试使用的第二个资源文件名。
    /// </summary>
    private const string TestFile2 = "测试文件1.txt";
    /// <summary>
    /// 上传测试使用的远程工作目录名。
    /// </summary>
    private const string TestFolder1 = "TestSubFolder";
    /// <summary>
    /// 目录创建测试使用的远程目录名。
    /// </summary>
    private const string TestFolder3 = "TestSubFolder4";
    /// <summary>
    /// 下载文件在本地工作目录下的子目录名。
    /// </summary>
    private const string TestDownloadFolder = "DownloadFolder";
    /// <summary>
    /// 用于执行集成测试 FTP 操作的客户端。
    /// </summary>
    private readonly IFtpClient _client;
    /// <summary>
    /// 保存测试期间使用的本地工作目录信息。
    /// </summary>
    private readonly DirectoryInfo _activeFolder;

    /// <summary>
    /// 初始化一个 <see cref="FtpClientIntegrationTest"/> 类型的实例。
    /// </summary>
    public FtpClientIntegrationTest()
    {
        var configuration = FtpIntegrationTestSettings.GetConfiguration();
        _client = new FtpClient(configuration.Host, configuration.Port, configuration.UserName, configuration.Password, false, EncryptionType.None);
        Directory.CreateDirectory(GetTestFilePath(ActiveFilePath));
        _activeFolder = new DirectoryInfo(GetTestFilePath(ActiveFilePath));
    }

    /// <summary>
    /// 将测试资源路径片段合并为基目录下的完整路径。
    /// </summary>
    /// <param name="paths">依次组成相对路径的目录或文件名片段。</param>
    /// <returns>相对于应用程序基目录的完整路径。</returns>
    private static string GetTestFilePath(params string[] paths)
    {
        return Path.Combine(AppContext.BaseDirectory, Path.Combine(paths));
    }

    /// <summary>
    /// 验证客户端可以连接到受控 FTP 服务。
    /// </summary>
    [FtpIntegrationFact]
    public void Connect_WhenConfigurationIsValid_ShouldConnect()
    {
        _client.Connect();
        Assert.True(_client.IsConnected);
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以上传并删除文件。
    /// </summary>
    /// <param name="fileName">要上传的测试资源文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void UploadFile_WhenFileExists_ShouldUploadAndDelete(string fileName)
    {
        _client.Connect();
        var result = _client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName);
        Assert.True(result);
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以在工作目录中上传文件。
    /// </summary>
    /// <param name="fileName">要上传的测试资源文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void UploadFile_WhenWorkingDirectoryIsSet_ShouldUploadAndDelete(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TestFolder1);
        _client.SetCurrentDirectory(TestFolder1);
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以在工作目录中上传流。
    /// </summary>
    /// <param name="fileName">要通过流上传的测试资源文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void UploadStream_WhenWorkingDirectoryIsSet_ShouldUploadAndDelete(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TestFolder1);
        _client.SetCurrentDirectory(TestFolder1);
        using var stream = File.OpenRead(GetTestFilePath(TestFilesPath, fileName));
        Assert.True(_client.UploadStream(stream, fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以在工作目录中上传字节数组。
    /// </summary>
    /// <param name="fileName">要读取并上传的测试资源文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void UploadBytes_WhenWorkingDirectoryIsSet_ShouldUploadAndDelete(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TestFolder1);
        _client.SetCurrentDirectory(TestFolder1);
        using var stream = File.OpenRead(GetTestFilePath(TestFilesPath, fileName));
        Assert.True(_client.UploadBytes(stream.GetAllBytes(), fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以批量上传文件。
    /// </summary>
    [FtpIntegrationFact]
    public void UploadFiles_WhenMultipleFilesProvided_ShouldUploadAndDeleteDirectory()
    {
        _client.Connect();
        var paths = new List<string> { GetTestFilePath(TestFilesPath, TestFile1), GetTestFilePath(TestFilesPath, TestFile2) };
        Assert.True(_client.UploadFiles(paths, "test"));
        Assert.True(_client.DeleteDirectory("test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以上传目录。
    /// </summary>
    [FtpIntegrationFact]
    public void UploadDirectory_WhenDirectoryExists_ShouldUploadAndDeleteDirectory()
    {
        _client.Connect();
        _client.CreateDirectory(TestFolder1);
        _client.SetCurrentDirectory(TestFolder1);
        Assert.True(_client.UploadDirectory(GetTestFilePath(TestFilesPath), "test"));
        Assert.True(_client.DeleteDirectory("test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以下载文件。
    /// </summary>
    /// <param name="fileName">要下载的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void DownloadFile_WhenRemoteFileExists_ShouldDownloadAndDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        var localFilePath = Path.Combine(_activeFolder.FullName, TestDownloadFolder, fileName);
        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
        Assert.True(_client.DownloadFile(localFilePath, fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以下载为字节数组。
    /// </summary>
    /// <param name="fileName">要下载为字节数组的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void DownloadBytes_WhenRemoteFileExists_ShouldDownloadAndDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        var localFilePath = Path.Combine(_activeFolder.FullName, TestDownloadFolder, fileName);
        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
        using var stream = File.Open(localFilePath, FileMode.OpenOrCreate, FileAccess.Write);
        Assert.True(_client.DownloadBytes(out var bytes, fileName));
        stream.Write(bytes);
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以批量下载文件。
    /// </summary>
    [FtpIntegrationFact]
    public void DownloadFiles_WhenRemoteFilesExist_ShouldDownloadAndDeleteDirectory()
    {
        var paths = new List<string> { GetTestFilePath(TestFilesPath, TestFile1), GetTestFilePath(TestFilesPath, TestFile2) };
        Assert.True(_client.UploadFiles(paths, "test"));
        var remotePaths = new List<string> { $"/test/{TestFile1}", $"/test/{TestFile2}" };
        Assert.True(_client.DownloadFiles(Path.Combine(_activeFolder.FullName, TestDownloadFolder), remotePaths));
        Assert.True(_client.DeleteDirectory("test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以下载目录。
    /// </summary>
    [FtpIntegrationFact]
    public void DownloadDirectory_WhenRemoteDirectoryExists_ShouldDownloadAndDeleteDirectory()
    {
        var paths = new List<string> { GetTestFilePath(TestFilesPath, TestFile1), GetTestFilePath(TestFilesPath, TestFile2) };
        Assert.True(_client.UploadFiles(paths, "test"));
        Assert.True(_client.DownloadDirectory(Path.Combine(_activeFolder.FullName, TestDownloadFolder), "test"));
        Assert.True(_client.DeleteDirectory("test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以删除文件。
    /// </summary>
    /// <param name="fileName">要删除的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void DeleteFile_WhenRemoteFileExists_ShouldDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以重命名文件。
    /// </summary>
    /// <param name="fileName">要重命名的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void RenameFile_WhenRemoteFileExists_ShouldRenameAndDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        Assert.True(_client.RenameFile(fileName, $"{fileName}_test"));
        Assert.True(_client.DeleteFile($"{fileName}_test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以复制文件。
    /// </summary>
    /// <param name="fileName">要复制的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void CopyFile_WhenRemoteFileExists_ShouldCopyAndDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        Assert.True(_client.CopyFile(fileName, $"{fileName}_test"));
        Assert.True(_client.DeleteFile(fileName));
        Assert.True(_client.DeleteFile($"{fileName}_test"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以移动文件。
    /// </summary>
    /// <param name="fileName">要移动的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void MoveFile_WhenRemoteFileExists_ShouldMoveAndDelete(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        _client.CreateDirectory("/test");
        Assert.True(_client.MoveFile(fileName, $"/test/{fileName}"));
        Assert.True(_client.DeleteFile($"/test/{fileName}"));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以识别已上传的文件。
    /// </summary>
    /// <param name="fileName">要检查是否存在的远程文件名。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFile1)]
    [InlineData(TestFile2)]
    public void FileExists_WhenRemoteFileExists_ShouldReturnTrue(string fileName)
    {
        Assert.True(_client.UploadFile(GetTestFilePath(TestFilesPath, fileName), fileName));
        Assert.True(_client.FileExists(fileName));
        Assert.True(_client.DeleteFile(fileName));
        _client.Dispose();
    }

    /// <summary>
    /// 验证可以创建目录。
    /// </summary>
    /// <param name="path">要创建并删除的远程目录路径。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFolder1)]
    [InlineData(TestFolder3)]
    public void CreateDirectory_WhenDirectoryDoesNotExist_ShouldCreateAndDelete(string path)
    {
        if (_client.DirectoryExists(path))
            _client.DeleteDirectory(path);
        Assert.True(_client.CreateDirectory(path));
        Assert.True(_client.DeleteDirectory(path));
    }

    /// <summary>
    /// 验证可以删除目录。
    /// </summary>
    /// <param name="path">要创建后再删除的远程目录路径。</param>
    [FtpIntegrationTheory]
    [InlineData(TestFolder1)]
    [InlineData(TestFolder3)]
    public void DeleteDirectory_WhenDirectoryExists_ShouldDelete(string path)
    {
        if (_client.DirectoryExists(path))
            _client.DeleteDirectory(path);
        Assert.True(_client.CreateDirectory(path));
        Assert.True(_client.DeleteDirectory(path));
    }
}
