namespace Bing.IO;

/// <summary>
/// 测试文件大小
/// </summary>
public class FileSizeTest
{
    /// <summary>
    /// 测试不同单位的文件大小转换是否正确
    /// </summary>
    [Theory]
    [InlineData(1, FileSizeUnit.Byte, 1)]
    [InlineData(1, FileSizeUnit.K, 1024)]
    [InlineData(1, FileSizeUnit.M, 1024 * 1024)]
    [InlineData(1, FileSizeUnit.G, 1024 * 1024 * 1024)]
    [InlineData(1, FileSizeUnit.T, 1024L * 1024L * 1024L * 1024L)]
    [InlineData(1, FileSizeUnit.P, 1024L * 1024L * 1024L * 1024L * 1024L)]
    public void GetSize_ShouldReturnCorrectSize(long size, FileSizeUnit unit, long expectedSize)
    {
        // Arrange
        var fileSize = new FileSize(size, unit);

        // Act
        var actualSize = fileSize.Size;

        // Assert
        Assert.Equal(expectedSize, actualSize);
    }

    /// <summary>
    /// 测试 ToString 方法的输出是否正确
    /// </summary>
    [Theory]
    [InlineData(1, FileSizeUnit.Byte, "1 B")]
    [InlineData(1024, FileSizeUnit.Byte, "1 KB")]
    [InlineData(1024 * 1024, FileSizeUnit.Byte, "1 MB")]
    [InlineData(1024 * 1024 * 1024, FileSizeUnit.Byte, "1 GB")]
    [InlineData(1024L * 1024L * 1024L * 1024L, FileSizeUnit.Byte, "1 TB")]
    [InlineData(1024L * 1024L * 1024L * 1024L * 1024L, FileSizeUnit.Byte, "1 PB")]
    public void ToString_ShouldReturnCorrectString(long size, FileSizeUnit unit, string expectedString)
    {
        // Arrange
        var fileSize = new FileSize(size, unit);

        // Act
        var actualString = fileSize.ToString();

        // Assert
        Assert.Equal(expectedString, actualString);
    }

    /// <summary>
    /// 测试获取文件大小（单位：K）是否正确
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(512, 0.5)]
    [InlineData(1024, 1.0)]
    [InlineData(2048, 2.0)]
    public void GetSizeByK_ShouldReturnCorrectSizeInK(long size, double expected)
    {
        // Arrange
        var fileSize = new FileSize(size, FileSizeUnit.Byte);

        // Act
        var sizeInK = fileSize.GetSizeByK();

        // Assert
        Assert.Equal(expected, sizeInK);
    }

    /// <summary>
    /// 测试获取文件大小（单位：M）是否正确
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1 * 1024L * 1024L, 1.0)]
    [InlineData(2 * 1024L * 1024L, 2.0)]
    public void GetSizeByM_ShouldReturnCorrectSizeInM(long size, double expected)
    {
        // Arrange
        var fileSize = new FileSize(size, FileSizeUnit.Byte);

        // Act
        var sizeByM = fileSize.GetSizeByM();

        // Assert
        Assert.Equal(expected, sizeByM);
    }

    /// <summary>
    /// 测试获取文件大小（单位：G）是否正确
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1 * 1024L * 1024L * 1024L, 1.0)]
    [InlineData(2 * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByG_ShouldReturnCorrectSizeInG(long size, double expected)
    {
        // Arrange
        var fileSize = new FileSize(size, FileSizeUnit.Byte);

        // Act
        var sizeByG = fileSize.GetSizeByG();

        // Assert
        Assert.Equal(expected, sizeByG);
    }

    /// <summary>
    /// 测试获取文件大小（单位：T）是否正确
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1 * 1024L * 1024L * 1024L * 1024L, 1.0)]
    [InlineData(2 * 1024L * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByT_ShouldReturnCorrectSizeInT(long size, double expected)
    {
        // Arrange
        var fileSize = new FileSize(size, FileSizeUnit.Byte);

        // Act
        var sizeByT = fileSize.GetSizeByT();

        // Assert
        Assert.Equal(expected, sizeByT);
    }

    /// <summary>
    /// 测试获取文件大小（单位：P）是否正确
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1 * 1024L * 1024L * 1024L * 1024L * 1024L, 1.0)]
    [InlineData(2 * 1024L * 1024L * 1024L * 1024L * 1024L, 2.0)]
    public void GetSizeByP_ShouldReturnCorrectSizeInP(long size, double expected)
    {
        // Arrange
        var fileSize = new FileSize(size, FileSizeUnit.Byte);

        // Act
        var sizeByP = fileSize.GetSizeByP();

        // Assert
        Assert.Equal(expected, sizeByP);
    }
}