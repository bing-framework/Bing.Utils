using Bing.IO;

namespace Bing.Utils.Tests.IO;

/// <summary>
/// 路径操作辅助类 测试
/// </summary>
public class PathHelperTest
{
    /// <summary>
    /// 测试 - 获取物理路径
    /// </summary>
    [Theory]
    [InlineData("a/b.txt", "a/b.txt")]
    [InlineData("/a/b.txt", "a/b.txt")]
    [InlineData("\\a\\b.txt", "a\\b.txt")]
    [InlineData("~a/b.txt", "a/b.txt")]
    public void Test_GetPhysicalPath_1(string relativePath, string target)
    {
        var path = PathHelper.GetPhysicalPath(relativePath);
        var result = $"{System.AppContext.BaseDirectory}{target}";
        Assert.Equal(result, path);
    }

    /// <summary>
    /// 测试 -  将 Windows 路径转换为 Unix 路径
    /// </summary>
    [Theory]
    [InlineData(@"C:\folder\file.txt", "C:/folder/file.txt")]
    [InlineData(@"C:\folder\subfolder\file.txt", "C:/folder/subfolder/file.txt")]
    [InlineData(@"D:\another-folder\file.txt", "D:/another-folder/file.txt")]
    [InlineData(@"D:\folder with space\file.txt", "D:/folder with space/file.txt")]
    [InlineData(@"E:\", "E:/")]
    [InlineData(@"F:\file.txt", "F:/file.txt")]
    [InlineData(@"", null)]
    [InlineData(null, null)]
    [InlineData("\t", null)]
    [InlineData("   ", null)]
    public void Test_ConvertWindowsPathToUnixPath_1(string windowsPath, string expectedUnixPath)
    {
        // Arrange

        // Act
        var result = PathHelper.ConvertWindowsPathToUnixPath(windowsPath);

        // Assert
        Assert.Equal(expectedUnixPath, result);
    }

    /// <summary>
    /// 测试 - 将 Unix 路径转换为 Windows 路径
    /// </summary>
    [Theory]
    [InlineData("C:/folder/file.txt", @"C:\folder\file.txt")]
    [InlineData("C:/folder/subfolder/file.txt", @"C:\folder\subfolder\file.txt")]
    [InlineData("D:/another-folder/file.txt", @"D:\another-folder\file.txt")]
    [InlineData("D:/folder with space/file.txt", @"D:\folder with space\file.txt")]
    [InlineData("E:/", @"E:\")]
    [InlineData("F:/file.txt", @"F:\file.txt")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("\t", null)]
    [InlineData("   ", null)]
    public void Test_ConvertUnixPathToWindowsPath_1(string unixPath, string expectedWindowsPath)
    {
        // Arrange

        // Act
        var result = PathHelper.ConvertUnixPathToWindowsPath(unixPath);

        // Assert
        Assert.Equal(expectedWindowsPath, result);
    }
}