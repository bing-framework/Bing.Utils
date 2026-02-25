namespace Bing.IO;
/// <summary>
/// 路径操作辅助类 测试
/// </summary>
public class PathHelperTest
{
    #region GetPhysicalPath
    /// <summary>
    /// 测试 - GetPhysicalPath - 正常场景
    /// </summary>
    [Theory]
    [InlineData("a/b.txt")]
    [InlineData("/a/b.txt")]
    [InlineData("\\a\\b.txt")]
    [InlineData("~a/b.txt")]
    [InlineData("~/test/file.txt")]
    public void GetPhysicalPath_WithValidInput_ShouldReturnCorrectPath(string relativePath)
    {
        // Act
        var result = PathHelper.GetPhysicalPath(relativePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldStartWith(AppContext.BaseDirectory);
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 空输入应抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void GetPhysicalPath_WithNullOrEmptyInput_ShouldThrowArgumentNullException(string relativePath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.GetPhysicalPath(relativePath))
            .ParamName.ShouldBe("relativePath");
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 自定义基础路径
    /// </summary>
    [Fact]
    public void GetPhysicalPath_WithCustomBasePath_ShouldUseCustomBase()
    {
        // Arrange
        var customBasePath = Path.GetTempPath();
        var relativePath = "test/file.txt";
        // Act
        var result = PathHelper.GetPhysicalPath(relativePath, customBasePath);
        // Assert
        result.ShouldStartWith(customBasePath);
        result.ShouldEndWith("file.txt");
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 复杂路径处理
    /// </summary>
    [Theory]
    [InlineData("folder/subfolder/file.txt")]
    [InlineData("~/folder/subfolder/file.txt")]
    [InlineData("/folder\\subfolder/file.txt")]
    [InlineData("~folder/subfolder/file.txt")]
    [InlineData("\\folder/subfolder\\file.txt")]
    public void GetPhysicalPath_WithComplexPaths_ShouldNormalizeProperly(string relativePath)
    {
        // Act
        var result = PathHelper.GetPhysicalPath(relativePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("folder");
        result.ShouldContain("subfolder");
        result.ShouldEndWith("file.txt");
    }
    #endregion
    #region GetWebRootPath
    /// <summary>
    /// 测试 - GetWebRootPath - 正常场景
    /// </summary>
    [Theory]
    [InlineData("css/style.css")]
    [InlineData("/js/script.js")]
    [InlineData("\\images\\logo.png")]
    [InlineData("~/assets/main.css")]
    public void GetWebRootPath_WithValidInput_ShouldReturnWebRootPath(string relativePath)
    {
        // Act
        var result = PathHelper.GetWebRootPath(relativePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("wwwroot");
    }
    /// <summary>
    /// 测试 - GetWebRootPath - 空输入
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void GetWebRootPath_WithNullOrEmptyInput_ShouldReturnEmpty(string relativePath)
    {
        // Act
        var result = PathHelper.GetWebRootPath(relativePath);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// <summary>
    /// 测试 - GetWebRootPath - 自定义基础路径
    /// </summary>
    [Fact]
    public void GetWebRootPath_WithCustomBasePath_ShouldUseCustomBase()
    {
        // Arrange
        var customBasePath = Path.GetTempPath();
        var relativePath = "assets/style.css";
        // Act
        var result = PathHelper.GetWebRootPath(relativePath, customBasePath);
        // Assert
        result.ShouldStartWith(customBasePath);
        result.ShouldContain("wwwroot");
        result.ShouldEndWith("style.css");
    }
    #endregion
    #region 路径转换测试
    /// <summary>
    /// 测试 - ConvertWindowsPathToUnixPath - 各种Windows路径格式
    /// </summary>
    [Theory]
    [InlineData(@"C:\folder\file.txt", "C:/folder/file.txt")]
    [InlineData(@"C:\folder\subfolder\file.txt", "C:/folder/subfolder/file.txt")]
    [InlineData(@"D:\another-folder\file.txt", "D:/another-folder/file.txt")]
    [InlineData(@"D:\folder with space\file.txt", "D:/folder with space/file.txt")]
    [InlineData(@"E:\", "E:/")]
    [InlineData(@"F:\file.txt", "F:/file.txt")]
    [InlineData(@"\relative\path", "/relative/path")]
    public void ConvertWindowsPathToUnixPath_WithValidPaths_ShouldConvertCorrectly(string windowsPath, string expected)
    {
        // Act
        var result = PathHelper.ConvertWindowsPathToUnixPath(windowsPath);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - ConvertWindowsPathToUnixPath - 空或无效输入
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("\t")]
    [InlineData("   ")]
    public void ConvertWindowsPathToUnixPath_WithInvalidInput_ShouldReturnNull(string windowsPath)
    {
        // Act
        var result = PathHelper.ConvertWindowsPathToUnixPath(windowsPath);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - ConvertUnixPathToWindowsPath - 各种Unix路径格式
    /// </summary>
    [Theory]
    [InlineData("C:/folder/file.txt", @"C:\folder\file.txt")]
    [InlineData("C:/folder/subfolder/file.txt", @"C:\folder\subfolder\file.txt")]
    [InlineData("D:/another-folder/file.txt", @"D:\another-folder\file.txt")]
    [InlineData("D:/folder with space/file.txt", @"D:\folder with space\file.txt")]
    [InlineData("E:/", @"E:\")]
    [InlineData("F:/file.txt", @"F:\file.txt")]
    [InlineData("/relative/path", @"\relative\path")]
    public void ConvertUnixPathToWindowsPath_WithValidPaths_ShouldConvertCorrectly(string unixPath, string expected)
    {
        // Act
        var result = PathHelper.ConvertUnixPathToWindowsPath(unixPath);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - ConvertUnixPathToWindowsPath - 空或无效输入
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("\t")]
    [InlineData("   ")]
    public void ConvertUnixPathToWindowsPath_WithInvalidInput_ShouldReturnNull(string unixPath)
    {
        // Act
        var result = PathHelper.ConvertUnixPathToWindowsPath(unixPath);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - AutoPathConvert - 空输入
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void AutoPathConvert_WithNullOrEmptyInput_ShouldReturnNull(string path)
    {
        // Act
        var result = PathHelper.AutoPathConvert(path);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - AutoPathConvert - 有效路径
    /// </summary>
    [Theory]
    [InlineData("test/path")]
    [InlineData("test\\path")]
    [InlineData("C:/folder/file.txt")]
    [InlineData(@"C:\folder\file.txt")]
    public void AutoPathConvert_WithValidPath_ShouldConvertToSystemFormat(string path)
    {
        // Act
        var result = PathHelper.AutoPathConvert(path);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        // 验证结果使用了正确的系统路径分隔符
        if (Path.DirectorySeparatorChar == '/')
        {
            // Linux/Unix 系统：结果应该使用 '/' 分隔符
            if (path.Contains('\\'))
            {
                // 如果输入包含Windows分隔符，应该被转换
                result.ShouldNotContain('\\');
                result.ShouldContain('/');
            }
            else
            {
                // 如果输入已经是Unix格式，可能不会改变
                result.ShouldNotContain('\\');
            }
        }
        else
        {
            // Windows 系统：结果应该使用 '\' 分隔符
            if (path.Contains('/'))
            {
                // 如果输入包含Unix分隔符，应该被转换
                result.ShouldNotContain('/');
                result.ShouldContain('\\');
            }
            else
            {
                // 如果输入已经是Windows格式，可能不会改变
                result.ShouldNotContain('/');
            }
        }
    }
    /// <summary>
    /// 测试 - NormalizePath - 各种路径格式
    /// </summary>
    [Theory]
    [InlineData("test/path")]
    [InlineData("test\\path")]
    [InlineData("C:/folder/file.txt")]
    [InlineData(@"C:\folder\file.txt")]
    public void NormalizePath_WithVariousFormats_ShouldUseSystemSeparator(string path)
    {
        // Act
        var result = PathHelper.NormalizePath(path);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        // 结果应该只包含系统的目录分隔符
        if (Path.DirectorySeparatorChar == '\\')
        {
            result.ShouldNotContain('/');
        }
        else
        {
            result.ShouldNotContain('\\');
        }
    }
    /// <summary>
    /// 测试 - NormalizePath - 空输入
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void NormalizePath_WithEmptyInput_ShouldReturnInput(string path)
    {
        // Act
        var result = PathHelper.NormalizePath(path);
        // Assert
        result.ShouldBe(path);
    }
    #endregion
    #region 路径验证测试
    /// <summary>
    /// 测试 - IsValidRelativePath - 有效相对路径
    /// </summary>
    [Theory]
    [InlineData("test/file.txt")]
    [InlineData("folder\\document.pdf")]
    [InlineData("images/photo.jpg")]
    [InlineData("assets/css/style.css")]
    [InlineData("folder_name/file-name.ext")]
    public void IsValidRelativePath_WithValidPaths_ShouldReturnTrue(string path)
    {
        // Act
        var result = PathHelper.IsValidRelativePath(path);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsValidRelativePath - 无效路径
    /// </summary>
    [Theory]
    [InlineData("C:\\absolute\\path")]
    [InlineData("/absolute/path")]
    [InlineData("../dangerous/path")]
    [InlineData("test/../other")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("path|with|pipes")]
    public void IsValidRelativePath_WithInvalidPaths_ShouldReturnFalse(string path)
    {
        // Act
        var result = PathHelper.IsValidRelativePath(path);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - SafeCombinePaths - 正常场景
    /// </summary>
    [Fact]
    public void SafeCombinePaths_WithValidInput_ShouldReturnCombinedPath()
    {
        // Arrange
        var basePath = Path.GetTempPath();
        var relativePath = "test/file.txt";
        // Act
        var result = PathHelper.SafeCombinePaths(basePath, relativePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldStartWith(basePath);
        result.ShouldContain("file.txt");
    }
    /// <summary>
    /// 测试 - SafeCombinePaths - 空基础路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SafeCombinePaths_WithNullOrEmptyBasePath_ShouldThrowArgumentNullException(string basePath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.SafeCombinePaths(basePath, "test.txt"))
            .ParamName.ShouldBe("basePath");
    }
    /// <summary>
    /// 测试 - SafeCombinePaths - 空相对路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SafeCombinePaths_WithNullOrEmptyRelativePath_ShouldThrowArgumentNullException(string relativePath)
    {
        // Arrange
        var basePath = Path.GetTempPath();
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.SafeCombinePaths(basePath, relativePath))
            .ParamName.ShouldBe("relativePath");
    }
    /// <summary>
    /// 测试 - SafeCombinePaths - 路径遍历攻击
    /// </summary>
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("test/../../../dangerous")]
    public void SafeCombinePaths_WithPathTraversal_ShouldThrowArgumentException(string maliciousPath)
    {
        // Arrange
        var basePath = Path.GetTempPath();
        // Act & Assert
        Should.Throw<ArgumentException>(() => PathHelper.SafeCombinePaths(basePath, maliciousPath));
    }
    #endregion
    #region GetRelativePath 测试
    /// <summary>
    /// 测试 - GetRelativePath - 正常场景
    /// </summary>
    [Fact]
    public void GetRelativePath_WithValidPaths_ShouldReturnRelativePath()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var fromPath = tempDir;
        var toPath = Path.Combine(tempDir, "subfolder", "file.txt");
        // Act
        var result = PathHelper.GetRelativePath(fromPath, toPath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("subfolder");
        result.ShouldContain("file.txt");
    }
    /// <summary>
    /// 测试 - GetRelativePath - 空起始路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRelativePath_WithNullOrEmptyFromPath_ShouldThrowArgumentNullException(string fromPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.GetRelativePath(fromPath, "C:/temp"))
            .ParamName.ShouldBe("fromPath");
    }
    /// <summary>
    /// 测试 - GetRelativePath - 空目标路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRelativePath_WithNullOrEmptyToPath_ShouldThrowArgumentNullException(string toPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.GetRelativePath("C:/temp", toPath))
            .ParamName.ShouldBe("toPath");
    }
    #endregion
    #region PathInfo 测试
    /// <summary>
    /// 测试 - GetPathInfo - 有效路径
    /// </summary>
    [Fact]
    public void GetPathInfo_WithValidPath_ShouldReturnPathInfo()
    {
        // Arrange
        var testPath = Path.Combine("test", "folder", "file.txt");
        // Act
        var result = PathHelper.GetPathInfo(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.FileName.ShouldBe("file.txt");
        result.FileNameWithoutExtension.ShouldBe("file");
        result.Extension.ShouldBe(".txt");
        result.FullPath.ShouldNotBeNullOrEmpty();
    }
    /// <summary>
    /// 测试 - GetPathInfo - 空路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPathInfo_WithNullOrEmptyPath_ShouldReturnEmptyPathInfo(string path)
    {
        // Act
        var result = PathHelper.GetPathInfo(path);
        // Assert
        result.ShouldNotBeNull();
        result.FullPath.ShouldBeNull();
        result.FileName.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - GetPathInfo - 绝对路径
    /// </summary>
    [Fact]
    public void GetPathInfo_WithAbsolutePath_ShouldSetIsAbsoluteTrue()
    {
        // Arrange
        var absolutePath = Path.Combine(Path.GetTempPath(), "test.txt");
        // Act
        var result = PathHelper.GetPathInfo(absolutePath);
        // Assert
        result.ShouldNotBeNull();
        result.IsAbsolute.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - GetPathInfo - 相对路径
    /// </summary>
    [Fact]
    public void GetPathInfo_WithRelativePath_ShouldSetIsAbsoluteFalse()
    {
        // Arrange
        var relativePath = "folder/test.txt";
        // Act
        var result = PathHelper.GetPathInfo(relativePath);
        // Assert
        result.ShouldNotBeNull();
        result.IsAbsolute.ShouldBeFalse();
    }
    #endregion
    #region 目录和文件操作测试
    /// <summary>
    /// 测试 - EnsureDirectoryExists - 新目录
    /// </summary>
    [Fact]
    public void EnsureDirectoryExists_WithNewDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            // Act
            var result = PathHelper.EnsureDirectoryExists(tempDir);
            // Assert
            result.ShouldNotBeNull();
            result.Exists.ShouldBeTrue();
            Directory.Exists(tempDir).ShouldBeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }
    /// <summary>
    /// 测试 - EnsureDirectoryExists - 已存在目录
    /// </summary>
    [Fact]
    public void EnsureDirectoryExists_WithExistingDirectory_ShouldReturnDirectoryInfo()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        // Act
        var result = PathHelper.EnsureDirectoryExists(tempDir);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - EnsureDirectoryExists - 空路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureDirectoryExists_WithNullOrEmptyPath_ShouldThrowArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.EnsureDirectoryExists(directoryPath))
            .ParamName.ShouldBe("directoryPath");
    }
    /// <summary>
    /// 测试 - EnsureDirectoryExists - 验证 DirectoryInfo.Exists 属性更新
    /// </summary>
    [Fact]
    public void EnsureDirectoryExists_WithNewDirectory_ShouldUpdateExistsProperty()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // 确保目录不存在
        Directory.Exists(tempDir).ShouldBeFalse();
        try
        {
            // Act
            var result = PathHelper.EnsureDirectoryExists(tempDir);
            // Assert
            result.ShouldNotBeNull();
            // 验证 DirectoryInfo.Exists 属性已正确更新
            result.Exists.ShouldBeTrue("DirectoryInfo.Exists 属性应该在创建目录后更新为 true");
            // 验证目录确实存在于文件系统中
            Directory.Exists(tempDir).ShouldBeTrue("目录应该在文件系统中实际存在");
            // 验证路径匹配
            result.FullName.ShouldBe(Path.GetFullPath(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }
    /// <summary>
    /// 测试 - EnsureDirectoryExists - 验证缓存问题
    /// </summary>
    [Fact]
    public void EnsureDirectoryExists_DirectoryInfoCaching_ShouldHandleCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            // 先创建一个 DirectoryInfo 对象（此时目录不存在）
            var dirInfoBefore = new DirectoryInfo(tempDir);
            dirInfoBefore.Exists.ShouldBeFalse("目录应该不存在");
            // Act - 通过 PathHelper 创建目录
            var result = PathHelper.EnsureDirectoryExists(tempDir);
            // Assert
            result.Exists.ShouldBeTrue("EnsureDirectoryExists 返回的 DirectoryInfo.Exists 应该为 true");
            // 验证原来的 DirectoryInfo 对象仍然缓存旧状态（除非刷新）
            dirInfoBefore.Exists.ShouldBeFalse("原始 DirectoryInfo 对象的 Exists 属性仍然是缓存的旧值");
            // 刷新后应该更新
            dirInfoBefore.Refresh();
            dirInfoBefore.Exists.ShouldBeTrue("刷新后 DirectoryInfo.Exists 应该更新");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }
    /// <summary>
    /// 测试 - GetUniqueFilePath - 文件不存在
    /// </summary>
    [Fact]
    public void GetUniqueFilePath_WithNonExistingFile_ShouldReturnOriginalPath()
    {
        // Arrange
        var nonExistingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        // Act
        var result = PathHelper.GetUniqueFilePath(nonExistingFile);
        // Assert
        result.ShouldBe(nonExistingFile);
    }
    /// <summary>
    /// 测试 - GetUniqueFilePath - 文件已存在
    /// </summary>
    [Fact]
    public void GetUniqueFilePath_WithExistingFile_ShouldReturnUniquePath()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            var result = PathHelper.GetUniqueFilePath(tempFile);
            // Assert
            result.ShouldNotBe(tempFile);
            result.ShouldContain("(1)");
            File.Exists(result).ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
    /// <summary>
    /// 测试 - GetUniqueFilePath - 空路径
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetUniqueFilePath_WithNullOrEmptyPath_ShouldThrowArgumentNullException(string filePath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PathHelper.GetUniqueFilePath(filePath))
            .ParamName.ShouldBe("filePath");
    }
    /// <summary>
    /// 测试 - GetUniqueFilePath - 多个重复文件
    /// </summary>
    [Fact]
    public void GetUniqueFilePath_WithMultipleExistingFiles_ShouldReturnCorrectNumber()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var baseFileName = "test.txt";
        var originalFile = Path.Combine(tempDir, baseFileName);
        var firstDuplicate = Path.Combine(tempDir, "test(1).txt");
        var secondDuplicate = Path.Combine(tempDir, "test(2).txt");
        try
        {
            // 创建原始文件和重复文件
            File.WriteAllText(originalFile, "test");
            File.WriteAllText(firstDuplicate, "test");
            File.WriteAllText(secondDuplicate, "test");
            // Act
            var result = PathHelper.GetUniqueFilePath(originalFile);
            // Assert
            result.ShouldContain("test(3).txt");
            File.Exists(result).ShouldBeFalse();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
    #endregion
    #region 辅助方法测试
    /// <summary>
    /// 测试 - PathEquals - 相同路径
    /// </summary>
    [Theory]
    [InlineData("C:/temp/file.txt", "C:\\temp\\file.txt")]
    [InlineData("temp/file.txt", "temp\\file.txt")]
    [InlineData(null, null)]
    public void PathEquals_WithSamePaths_ShouldReturnTrue(string path1, string path2)
    {
        // Act
        var result = PathHelper.PathEquals(path1, path2);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - PathEquals - 不同路径
    /// </summary>
    [Theory]
    [InlineData("C:/temp/file1.txt", "C:/temp/file2.txt")]
    [InlineData("C:/temp", "D:/temp")]
    [InlineData("temp", null)]
    [InlineData(null, "temp")]
    public void PathEquals_WithDifferentPaths_ShouldReturnFalse(string path1, string path2)
    {
        // Act
        var result = PathHelper.PathEquals(path1, path2);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsPathUnderBase - 路径在基础路径下
    /// </summary>
    [Fact]
    public void IsPathUnderBase_WithPathUnderBase_ShouldReturnTrue()
    {
        // Arrange
        var basePath = Path.GetTempPath();
        var subPath = Path.Combine(basePath, "subfolder", "file.txt");
        // Act
        var result = PathHelper.IsPathUnderBase(subPath, basePath);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsPathUnderBase - 路径不在基础路径下
    /// </summary>
    [Fact]
    public void IsPathUnderBase_WithPathNotUnderBase_ShouldReturnFalse()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base");
        var otherPath = Path.Combine(Path.GetTempPath(), "other", "file.txt");
        // Act
        var result = PathHelper.IsPathUnderBase(otherPath, basePath);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsPathUnderBase - 空输入
    /// </summary>
    [Theory]
    [InlineData(null, "C:/temp")]
    [InlineData("", "C:/temp")]
    [InlineData("C:/temp/file.txt", null)]
    [InlineData("C:/temp/file.txt", "")]
    [InlineData(null, null)]
    public void IsPathUnderBase_WithNullOrEmptyInput_ShouldReturnFalse(string path, string basePath)
    {
        // Act
        var result = PathHelper.IsPathUnderBase(path, basePath);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region 边界值和异常测试
    /// <summary>
    /// 测试 - 路径包含特殊字符
    /// </summary>
    [Theory]
    [InlineData("file with spaces.txt")]
    [InlineData("file-with-dashes.txt")]
    [InlineData("file_with_underscores.txt")]
    [InlineData("file.with.dots.txt")]
    public void PathOperations_WithSpecialCharacters_ShouldHandleCorrectly(string fileName)
    {
        // Arrange
        var relativePath = $"folder/{fileName}";
        // Act & Assert
        Should.NotThrow(() => PathHelper.GetPhysicalPath(relativePath));
        Should.NotThrow(() => PathHelper.GetWebRootPath(relativePath));
        Should.NotThrow(() => PathHelper.GetPathInfo(relativePath));
    }
    /// <summary>
    /// 测试 - 极长路径处理
    /// </summary>
    [Fact]
    public void PathOperations_WithVeryLongPath_ShouldHandleGracefully()
    {
        // Arrange
        var longFileName = new string('a', 200) + ".txt";
        var longPath = $"folder/{longFileName}";
        // Act & Assert
        Should.NotThrow(() => PathHelper.IsValidRelativePath(longPath));
        Should.NotThrow(() => PathHelper.GetPathInfo(longPath));
    }
    /// <summary>
    /// 测试 - Unicode字符路径
    /// </summary>
    [Theory]
    [InlineData("文件夹/文件.txt")]
    [InlineData("папка/файл.txt")]
    [InlineData("フォルダ/ファイル.txt")]
    public void PathOperations_WithUnicodeCharacters_ShouldHandleCorrectly(string unicodePath)
    {
        // Act & Assert
        Should.NotThrow(() => PathHelper.GetPhysicalPath(unicodePath));
        Should.NotThrow(() => PathHelper.ConvertWindowsPathToUnixPath(unicodePath));
        Should.NotThrow(() => PathHelper.ConvertUnixPathToWindowsPath(unicodePath));
    }
    #endregion
    #region 性能和压力测试
    /// <summary>
    /// 测试 - 大量路径转换性能
    /// </summary>
    [Fact]
    public void PathConversion_WithManyPaths_ShouldPerformReasonably()
    {
        // Arrange
        var paths = new string[1000];
        for (int i = 0; i < paths.Length; i++)
        {
            paths[i] = $"C:\\folder{i}\\subfolder{i}\\file{i}.txt";
        }
        // Act & Assert
        Should.NotThrow(() =>
        {
            foreach (var path in paths)
            {
                PathHelper.ConvertWindowsPathToUnixPath(path);
                PathHelper.ConvertUnixPathToWindowsPath(path.Replace('\\', '/'));
            }
        });
    }
    #endregion
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
