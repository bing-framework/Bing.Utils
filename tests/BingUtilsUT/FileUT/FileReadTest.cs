using Bing.Helpers;
using Bing.IO;

namespace BingUtilsUT.FileUT;

/// <summary>
/// 文件读取测试
/// </summary>
[Trait("FileUT","FileHelper.Read")]
public class FileReadTest
{
    /// <summary>
    /// 测试 - 读取文件到字符串中
    /// </summary>
    [Fact]
    public void Test_ReadToString()
    {
        var filePath = Common.GetPhysicalPath("/Samples/FileSample.txt");
        Assert.Equal("test", FileHelper.ReadToString(filePath));
    }

    /// <summary>
    /// 测试 - 读取文件到字符串中
    /// </summary>
    [Fact]
    public async Task Test_ReadToStringAsync()
    {
        var filePath = Common.GetPhysicalPath("/Samples/FileSample.txt");
        Assert.Equal("test", await FileHelper.ReadToStringAsync(filePath));
    }
}