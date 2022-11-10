using System.IO;
using Bing.Net.FTP;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Net.FTP;

public class FtpClientTest : TestBase
{
    /// <summary>
    /// 客户端
    /// </summary>
    private readonly IFtpClient _client;

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public FtpClientTest(ITestOutputHelper output) : base(output)
    {
        _client = new FtpClient("10.186.100.90", 2121, "erp01", "Erp@2022", false, EncryptionType.None);
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
    [Fact]
    public void Test_UploadFile()
    {
        _client.Connect();
        var result = _client.UploadFile("export\\test", Path.Combine("D:\\", "测试导出_20191225120408.xlsx"));
        Assert.True(result);
        _client.Dispose();
    }
}