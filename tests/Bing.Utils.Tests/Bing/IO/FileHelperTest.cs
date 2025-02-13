using Bing.Helpers;

namespace Bing.IO;

/// <summary>
/// 文件操作辅助类 单元测试
/// </summary>
public class FileHelperTest
{
    /// <summary>
    /// 测试 - 读取文件到字符串
    /// </summary>
    [Fact]
    public void Test_ReadToString()
    {
        var filePath = Common.GetPhysicalPath("/Samples/FileSample.txt");
        Assert.Equal("test", FileHelper.ReadToString(filePath));
    }

    /// <summary>
    /// 测试 - 读取文件到字符串
    /// </summary>
    [Fact]
    public async Task Test_ReadToStringAsync()
    {
        var filePath = Common.GetPhysicalPath("/Samples/FileSample.txt");
        Assert.Equal("test", await FileHelper.ReadToStringAsync(filePath));
    }
}