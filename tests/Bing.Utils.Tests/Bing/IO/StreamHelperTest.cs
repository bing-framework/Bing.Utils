namespace Bing.IO;
/// <summary>
/// 流操作辅助类测试
/// </summary>
public class StreamHelperTest
{
    #region GenerateStreamFromString 测试
    /// <summary>
    /// 测试 - GenerateStreamFromString - 正常字符串转换
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_ValidContent_ReturnsCorrectStream()
    {
        // Arrange
        var content = "Hello, World!";
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.ShouldNotBeNull();
        result.Position.ShouldBe(0);
        result.Length.ShouldBe(expectedBytes.Length);
        result.CanRead.ShouldBeTrue();
        result.CanWrite.ShouldBeTrue();
        result.CanSeek.ShouldBeTrue();
        // 验证内容
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 空字符串处理
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_EmptyString_ReturnsEmptyStream()
    {
        // Arrange
        var content = string.Empty;
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.ShouldNotBeNull();
        result.Position.ShouldBe(0);
        result.Length.ShouldBe(0);
        result.CanRead.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - null 内容应抛出异常
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_NullContent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => StreamHelper.GenerateStreamFromString(null))
            .ParamName.ShouldBe("content");
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 使用默认 UTF-8 编码
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_DefaultEncoding_UsesUTF8()
    {
        // Arrange
        var content = "测试中文内容";
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 使用指定编码
    /// </summary>
    [Theory]
    [InlineData("Hello World")]
    [InlineData("测试中文")]
    [InlineData("Ñiño español")]
    [InlineData("Здравствуй мир")]
    public void GenerateStreamFromString_SpecificEncoding_ReturnsCorrectBytes(string content)
    {
        // Arrange
        var encoding = Encoding.Unicode;
        var expectedBytes = encoding.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content, encoding);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - ASCII 编码
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_ASCIIEncoding_ReturnsCorrectStream()
    {
        // Arrange
        var content = "Hello ASCII";
        var encoding = Encoding.ASCII;
        var expectedBytes = encoding.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content, encoding);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 流可读性验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_StreamReadability_ShouldBeReadable()
    {
        // Arrange
        var content = "Readable stream test";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.CanRead.ShouldBeTrue();
        // 验证可以读取内容
        using var reader = new StreamReader(result, Encoding.UTF8);
        var readContent = reader.ReadToEnd();
        readContent.ShouldBe(content);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 流位置重置验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_StreamPosition_ShouldBeAtStart()
    {
        // Arrange
        var content = "Position test content";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.Position.ShouldBe(0);
        // 读取第一个字节后位置应该改变
        var firstByte = result.ReadByte();
        result.Position.ShouldBe(1);
        // 重置位置
        result.Position = 0;
        result.Position.ShouldBe(0);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 多字节字符处理
    /// </summary>
    [Theory]
    [InlineData("🚀 Emoji test")]
    [InlineData("𝓗𝓮𝓵𝓵𝓸 Unicode")]
    [InlineData("👨‍👩‍👧‍👦 Family emoji")]
    public void GenerateStreamFromString_MultiByte_HandlesCorrectly(string content)
    {
        // Arrange
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
        // 验证可以正确读回
        result.Position = 0;
        using var reader = new StreamReader(result, Encoding.UTF8);
        var readContent = reader.ReadToEnd();
        readContent.ShouldBe(content);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 特殊字符处理
    /// </summary>
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\t")]
    [InlineData("\0")]
    [InlineData("Line1\nLine2\r\nLine3")]
    public void GenerateStreamFromString_SpecialCharacters_HandlesCorrectly(string content)
    {
        // Arrange
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 大文本处理
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_LargeContent_HandlesCorrectly()
    {
        // Arrange
        var content = new string('A', 10000); // 10KB 的文本
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.Length.ShouldBe(expectedBytes.Length);
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 流的可扩展性验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_StreamExpandability_ShouldBeExpandable()
    {
        // Arrange
        var content = "Initial content";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.CanWrite.ShouldBeTrue();
        // 验证流是可扩展的 - 可以追加内容
        result.Seek(0, SeekOrigin.End);
        var additionalBytes = Encoding.UTF8.GetBytes(" - Added");
        // 这个操作不应该抛出异常
        Should.NotThrow(() => result.Write(additionalBytes, 0, additionalBytes.Length));
        // 验证追加后的内容
        result.Position = 0;
        using var reader = new StreamReader(result, Encoding.UTF8);
        var finalContent = reader.ReadToEnd();
        finalContent.ShouldBe("Initial content - Added");
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 多次写入扩展能力
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_MultipleWrites_ShouldExpandCorrectly()
    {
        // Arrange
        var content = "Start";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.Seek(0, SeekOrigin.End);
        // 多次写入以测试扩展能力
        for (int i = 1; i <= 5; i++)
        {
            var appendContent = $" Part{i}";
            var appendBytes = Encoding.UTF8.GetBytes(appendContent);
            Should.NotThrow(() => result.Write(appendBytes, 0, appendBytes.Length));
        }
        // 验证最终内容
        result.Position = 0;
        using var reader = new StreamReader(result, Encoding.UTF8);
        var finalContent = reader.ReadToEnd();
        finalContent.ShouldBe("Start Part1 Part2 Part3 Part4 Part5");
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 内存流类型验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_ReturnType_ShouldBeMemoryStream()
    {
        // Arrange
        var content = "Type test";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        result.ShouldBeOfType<MemoryStream>();
        result.GetType().ShouldBe(typeof(MemoryStream));
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 编码为 null 时使用默认编码
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_NullEncoding_UsesDefaultUTF8()
    {
        // Arrange
        var content = "Default encoding test";
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content, null);
        // Assert
        var actualBytes = result.ToArray();
        actualBytes.ShouldBe(expectedBytes);
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 并发安全性验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var content = "Concurrency test";
        var expectedBytes = Encoding.UTF8.GetBytes(content);
        // Act & Assert
        Should.NotThrow(() =>
        {
            Parallel.For(0, 100, i =>
            {
                using var stream = StreamHelper.GenerateStreamFromString($"{content} {i}");
                stream.ShouldNotBeNull();
                stream.Length.ShouldBeGreaterThan(0);
            });
        });
    }
    /// <summary>
    /// 测试 - GenerateStreamFromString - 容量管理验证
    /// </summary>
    [Fact]
    public void GenerateStreamFromString_CapacityManagement_ShouldHandleCorrectly()
    {
        // Arrange
        var content = "Test";
        // Act
        using var result = StreamHelper.GenerateStreamFromString(content);
        // Assert
        var initialLength = result.Length;
        var initialCapacity = result.Capacity;
        // 写入大量数据以测试容量扩展
        var largeData = new byte[1024]; // 1KB
        result.Seek(0, SeekOrigin.End);
        result.Write(largeData, 0, largeData.Length);
        // 验证容量和长度都正确更新
        result.Length.ShouldBe(initialLength + largeData.Length);
        result.Capacity.ShouldBeGreaterThanOrEqualTo((int)result.Length);
    }
    #endregion
}
