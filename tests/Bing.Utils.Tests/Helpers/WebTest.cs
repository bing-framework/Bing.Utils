using System.IO;
using System.Net;
using System.Threading.Tasks;
using Bing.Helpers;
using Bing.IO;
using Bing.Text;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// Web操作测试
/// </summary>
public class WebTest : TestBase
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public WebTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试客户端上传文件
    /// </summary>
    [Fact(Skip = "未设置上传地址")]
    public async Task Test_Client_UploadFile()
    {
        var result = await Web.Client()
            .Post("")
            .FileData("files", @"")
            .IgnoreSsl()
            .ResultAsync();
        Output.WriteLine(result);
    }

    /// <summary>
    /// 测试客户端网页访问
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Test_Client_WebAccess()
    {
        for (int i = 0; i < 10; i++)
        {
            await WriteFile("https://www.cnblogs.com");
        }
        await WriteFile("https://www.cnblogs.com");
        await WriteFile("https://www.cnblogs.com/artech/p/logging-for-net-core-05.html");
    }

    private async Task WriteFile(string url)
    {
        var path = @"D:\Test\File\";
        DirectoryHelper.CreateIfNotExists(path);
        var result = await Web.Client()
            .Get(url)
            .IgnoreSsl()
            .ResultAsync();
        var key = Bing.Utils.Randoms.GuidRandomGenerator.Instance.Generate();
        await File.WriteAllBytesAsync($"{path}test_{key}.txt", result.ToBytes());
    }

    /// <summary>
    /// 测试获取主机
    /// </summary>
    [Fact]
    public void Test_Host()
    {
        Output.WriteLine(Web.Host);
    }

    /// <summary>
    /// 测试获取客户端IP地址
    /// </summary>
    [Fact]
    public void Test_Ip()
    {
        Output.WriteLine(Web.IP);
    }

    /// <summary>
    /// 测试获取本地IP
    /// </summary>
    [Fact]
    public void Test_LocalIpAddress()
    {
        Output.WriteLine(Web.LocalIpAddress);
    }

    [Fact]
    public void Test_LocalIpAddress_1()
    {
        Output.WriteLine(GetLocalIPAddress());
    }

    private string GetLocalIPAddress()
    {
        string AddressIP = string.Empty;
        foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
            {
                AddressIP = _IPAddress.ToString();
            }
        }
        return AddressIP;
    }
}