using System.Collections.Generic;
using System.IO;
using Bing.Extensions;
using Bing.IO;
using Bing.Net.FTP;
using FluentFTP.Helpers;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Net.FTP;

public class FtpClientTest : TestBase
{
    /// <summary>
    /// 客户端
    /// </summary>
    private readonly IFtpClient _client;

    // 测试路径
    private const string TEST_FILES_PATH = "Resources/TestFiles";
    private const string ACTIVE_FILE_PATH = "Resources/ActiveTestFolder";

    // 测试文件
    private const string TEST_FILE_1 = "Text.txt";
    private const string TEST_FILE_2 = "测试文件1.txt";
    private const string TEST_FOLDER_1 = "TestSubFolder";
    private const string TEST_FOLDER_2 = "TestSubFolder2\\TestSubFolder3";
    private const string TEST_FOLDER_3 = "TestSubFolder4";
    private const string TEST_DOWNLOAD_FOLDER = "DownloadFolder";

    private readonly DirectoryInfo _activeFolder;

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public FtpClientTest(ITestOutputHelper output) : base(output)
    {
        _client = new FtpClient("10.186.100.90", 2121, "erp01", "Erp@2022", false, EncryptionType.None);
        DirectoryHelper.CreateIfNotExists(GetTestFilePath(ACTIVE_FILE_PATH));
        _activeFolder = new DirectoryInfo(GetTestFilePath(ACTIVE_FILE_PATH));
    }

    /// <summary>
    /// 测试 - 连接
    /// </summary>
    [Fact]
    public void Test_Connect()
    {
        _client.Connect();
        Assert.True(_client.IsConnected);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 上传文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_UploadFile(string fileName)
    {
        _client.Connect();
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 上传文件 - 工作目录
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_UploadFile_SubFolder(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TEST_FOLDER_1);
        _client.SetCurrentDirectory(TEST_FOLDER_1);
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 上传文件流 - 工作目录
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_UploadStream_SubFolder(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TEST_FOLDER_1);
        _client.SetCurrentDirectory(TEST_FOLDER_1);
        using var stream = File.OpenRead(GetTestFilePath(TEST_FILES_PATH, fileName));
        var result = _client.UploadStream(stream, fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 上传字节数组 - 工作目录
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_UploadBytes_SubFolder(string fileName)
    {
        _client.Connect();
        _client.CreateDirectory(TEST_FOLDER_1);
        _client.SetCurrentDirectory(TEST_FOLDER_1);
        using var stream = File.OpenRead(GetTestFilePath(TEST_FILES_PATH, fileName));
        var bytes = stream.GetAllBytes();
        var result = _client.UploadBytes(bytes, fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 批量上传文件 - 工作目录
    /// </summary>
    [Fact]
    public void Test_UploadFiles_SubFolder()
    {
        _client.Connect();
        _client.CreateDirectory(TEST_FOLDER_1);
        _client.SetCurrentDirectory(TEST_FOLDER_1);
        var paths = new List<string>
        {
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_1),
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_2),
        };
        var remotePath = "test";
        var result = _client.UploadFiles(paths, remotePath);
        Assert.True(result);
        result = _client.DeleteDirectory(remotePath);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 上传文件夹
    /// </summary>
    [Fact]
    public void Test_UploadDirectory_SubFolder()
    {
        _client.Connect();
        _client.CreateDirectory(TEST_FOLDER_1);
        _client.SetCurrentDirectory(TEST_FOLDER_1);
        
        var remotePath = "test";
        var result = _client.UploadDirectory(GetTestFilePath(TEST_FILES_PATH), remotePath);
        Assert.True(result);
        result = _client.DeleteDirectory(remotePath);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 下载文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_DownloadFile(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        var localFilePath = Path.Combine(_activeFolder.FullName, TEST_DOWNLOAD_FOLDER, fileName);
        if(File.Exists(localFilePath))
            File.Delete(localFilePath);
        result = _client.DownloadFile(localFilePath, fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 下载字节数组
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_DownloadStream(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        var localFilePath = Path.Combine(_activeFolder.FullName, TEST_DOWNLOAD_FOLDER, fileName);
        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
        using var stream = File.Open(localFilePath, FileMode.OpenOrCreate, FileAccess.Write);
        result = _client.DownloadBytes(out var bytes, fileName);
        stream.Write(bytes);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 批量下载文件
    /// </summary>
    [Fact]
    public void Test_DownloadFiles()
    {
        var paths = new List<string>
        {
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_1),
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_2),
        };
        var remotePath = "test";
        var result = _client.UploadFiles(paths, remotePath);
        Assert.True(result);

        var remotePaths = new List<string>
        {
            $"/test/{TEST_FILE_1}",
            $"/test/{TEST_FILE_2}",
        };
        var localPath= Path.Combine(_activeFolder.FullName, TEST_DOWNLOAD_FOLDER);
        result = _client.DownloadFiles(localPath, remotePaths);
        Assert.True(result);
        result = _client.DeleteDirectory(remotePath);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 下载文件夹
    /// </summary>
    [Fact]
    public void Test_DownloadDirectory()
    {
        var paths = new List<string>
        {
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_1),
            GetTestFilePath(TEST_FILES_PATH, TEST_FILE_2),
        };
        var remotePath = "test";
        var result = _client.UploadFiles(paths, remotePath);
        Assert.True(result);

        var localPath = Path.Combine(_activeFolder.FullName, TEST_DOWNLOAD_FOLDER);
        result = _client.DownloadDirectory(localPath, remotePath);
        Assert.True(result);
        result = _client.DeleteDirectory(remotePath);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 删除文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_DeleteFile(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 重命名文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_RenameFile(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.RenameFile(fileName, $"{fileName}_test");
        Assert.True(result);
        result = _client.DeleteFile($"{fileName}_test");
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 复制文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_CopyFile(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.CopyFile(fileName, $"{fileName}_test");
        Assert.True(result);
        result = _client.DeleteFile($"{fileName}");
        Assert.True(result);
        result = _client.DeleteFile($"{fileName}_test");
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 移动文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_MoveFile(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.CreateDirectory("/test");
        result = _client.MoveFile(fileName, $"/test/{fileName}");
        Assert.True(result);
        result = _client.DeleteFile($"/test/{fileName}");
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 是否存在文件
    /// </summary>
    [Theory]
    [InlineData(TEST_FILE_1)]
    [InlineData(TEST_FILE_2)]
    public void Test_FileExists(string fileName)
    {
        var result = _client.UploadFile(GetTestFilePath(TEST_FILES_PATH, fileName), fileName);
        Assert.True(result);
        result = _client.FileExists(fileName);
        Assert.True(result);
        result = _client.DeleteFile(fileName);
        Assert.True(result);
        _client.Dispose();
    }

    /// <summary>
    /// 测试 - 创建文件夹
    /// </summary>
    [Theory]
    [InlineData(TEST_FOLDER_1)]
    [InlineData(TEST_FOLDER_3)]
    public void Test_CreateDirectory(string path)
    {
        if (_client.DirectoryExists(path))
            _client.DeleteDirectory(path);
        var result = _client.CreateDirectory(path);
        Assert.True(result);
        _client.DeleteDirectory(path);
        Assert.True(result);
    }

    /// <summary>
    /// 测试 -删除文件夹
    /// </summary>
    [Theory]
    [InlineData(TEST_FOLDER_1)]
    [InlineData(TEST_FOLDER_3)]
    public void Test_DeleteDirectory(string path)
    {
        if (_client.DirectoryExists(path))
            _client.DeleteDirectory(path);
        var result = _client.CreateDirectory(path);
        Assert.True(result);
        _client.DeleteDirectory(path);
        Assert.True(result);
    }
}