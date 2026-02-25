namespace Bing.IO;
/// <summary>
/// 目录操作辅助类 测试
/// </summary>
public class DirectoryHelperTest : TestBase, IDisposable
{
    #region 初始化
    /// <summary>
    /// 测试根目录路径
    /// </summary>
    private readonly string _testRootPath;
    /// <summary>
    /// 创建目录列表
    /// </summary>
    private readonly List<string> _createdDirectories;
    /// <summary>
    /// 创建文件列表
    /// </summary>
    private readonly List<string> _createdFiles;
    /// <summary>
    /// 测试初始化
    /// </summary>
    public DirectoryHelperTest(ITestOutputHelper output) : base(output)
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), "DirectoryHelperTest", Guid.NewGuid().ToString());
        _createdDirectories = new List<string>();
        _createdFiles = new List<string>();
        // 创建测试根目录
        Directory.CreateDirectory(_testRootPath);
        _createdDirectories.Add(_testRootPath);
    }
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 清理测试创建的文件和目录
        try
        {
            foreach (var file in _createdFiles.Where(File.Exists))
            {
                File.Delete(file);
            }
            // 按照深度倒序删除目录
            foreach (var dir in _createdDirectories.OrderByDescending(d => d.Split(Path.DirectorySeparatorChar).Length))
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Output.WriteLine($"清理测试资源时出错: {ex.Message}");
        }
    }
    /// <summary>
    /// 创建测试目录
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns>目录路径</returns>
    private string CreateTestDirectory(string relativePath = null)
    {
        var path = string.IsNullOrEmpty(relativePath)
            ? Path.Combine(_testRootPath, Guid.NewGuid().ToString())
            : Path.Combine(_testRootPath, relativePath);
        Directory.CreateDirectory(path);
        _createdDirectories.Add(path);
        return path;
    }
    /// <summary>
    /// 创建测试文件
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="fileName">文件名</param>
    /// <param name="content">内容</param>
    /// <returns>文件路径</returns>
    private string CreateTestFile(string directoryPath, string fileName = null, string content = "test content")
    {
        fileName ??= Guid.NewGuid().ToString() + ".txt";
        var filePath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(filePath, content);
        _createdFiles.Add(filePath);
        return filePath;
    }
    #endregion
    #region CreateDirectory 测试
    /// <summary>
    /// 测试 - CreateDirectory - 创建目录成功
    /// </summary>
    [Fact]
    public void CreateDirectory_ValidDirectoryPath_CreatesDirectory()
    {
        // Arrange
        var testPath = Path.Combine(_testRootPath, "CreateDirectoryTest");
        // Act
        var result = DirectoryHelper.CreateDirectory(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(testPath);
        _createdDirectories.Add(testPath);
    }
    /// <summary>
    /// 测试 - CreateDirectory - 通过文件路径创建目录
    /// </summary>
    [Fact]
    public void CreateDirectory_ValidFilePath_CreatesParentDirectory()
    {
        // Arrange
        var directoryPath = Path.Combine(_testRootPath, "FilePathTest");
        var filePath = Path.Combine(directoryPath, "test.txt");
        // Act
        var result = DirectoryHelper.CreateDirectory(filePath);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(directoryPath);
        _createdDirectories.Add(directoryPath);
    }
    /// <summary>
    /// 测试 - CreateDirectory - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDirectory_NullOrEmptyPath_ThrowsArgumentException(string path)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DirectoryHelper.CreateDirectory(path));
    }
    /// <summary>
    /// 测试 - CreateDirectory - 已存在目录不重复创建
    /// </summary>
    [Fact]
    public void CreateDirectory_ExistingDirectory_ReturnsExistingDirectory()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.CreateDirectory(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(testPath);
    }
    #endregion
    #region CreateIfNotExists 测试
    /// <summary>
    /// 测试 - CreateIfNotExists - 字符串参数创建不存在的目录
    /// </summary>
    [Fact]
    public void CreateIfNotExists_NonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var testPath = Path.Combine(_testRootPath, "CreateIfNotExistsTest");
        // Act
        var result = DirectoryHelper.CreateIfNotExists(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(testPath);
        _createdDirectories.Add(testPath);
    }
    /// <summary>
    /// 测试 - CreateIfNotExists - 字符串参数返回已存在目录
    /// </summary>
    [Fact]
    public void CreateIfNotExists_ExistentDirectory_ReturnsExistingDirectory()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.CreateIfNotExists(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(testPath);
    }
    /// <summary>
    /// 测试 - CreateIfNotExists - 空字符串返回null
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateIfNotExists_NullOrEmptyString_ReturnsNull(string directory)
    {
        // Act
        var result = DirectoryHelper.CreateIfNotExists(directory);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - CreateIfNotExists - DirectoryInfo参数创建不存在的目录
    /// </summary>
    [Fact]
    public void CreateIfNotExists_NonExistentDirectoryInfo_CreatesDirectory()
    {
        // Arrange
        var testPath = Path.Combine(_testRootPath, "CreateIfNotExistsDirectoryInfoTest");
        var directoryInfo = new DirectoryInfo(testPath);
        // Act
        var result = DirectoryHelper.CreateIfNotExists(directoryInfo);
        // Assert
        result.ShouldNotBeNull();
        result.Exists.ShouldBeTrue();
        result.FullName.ShouldBe(testPath);
        _createdDirectories.Add(testPath);
    }
    /// <summary>
    /// 测试 - CreateIfNotExists - DirectoryInfo参数为null返回null
    /// </summary>
    [Fact]
    public void CreateIfNotExists_NullDirectoryInfo_ReturnsNull()
    {
        // Act
        var result = DirectoryHelper.CreateIfNotExists((DirectoryInfo)null);
        // Assert
        result.ShouldBeNull();
    }
    #endregion
    #region DeleteIfExists 测试
    /// <summary>
    /// 测试 - DeleteIfExists - 删除存在的目录
    /// </summary>
    [Fact]
    public void DeleteIfExists_ExistentDirectory_DeletesAndReturnsTrue()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.DeleteIfExists(testPath);
        // Assert
        result.ShouldBeTrue();
        Directory.Exists(testPath).ShouldBeFalse();
        _createdDirectories.Remove(testPath);
    }
    /// <summary>
    /// 测试 - DeleteIfExists - 删除不存在的目录返回false
    /// </summary>
    [Fact]
    public void DeleteIfExists_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var testPath = Path.Combine(_testRootPath, "NonExistentDirectory");
        // Act
        var result = DirectoryHelper.DeleteIfExists(testPath);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - DeleteIfExists - 递归删除包含文件的目录
    /// </summary>
    [Fact]
    public void DeleteIfExists_DirectoryWithFiles_DeletesRecursively()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.txt");
        // Act
        var result = DirectoryHelper.DeleteIfExists(testPath, true);
        // Assert
        result.ShouldBeTrue();
        Directory.Exists(testPath).ShouldBeFalse();
        _createdDirectories.Remove(testPath);
    }
    /// <summary>
    /// 测试 - DeleteIfExists - 空路径返回false
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteIfExists_NullOrEmptyPath_ReturnsFalse(string directory)
    {
        // Act
        var result = DirectoryHelper.DeleteIfExists(directory);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region IsSubDirectoryOf 测试
    /// <summary>
    /// 测试 - IsSubDirectoryOf - 字符串参数判断子目录关系
    /// </summary>
    [Fact]
    public void IsSubDirectoryOf_ValidChildDirectory_ReturnsTrue()
    {
        // Arrange
        var parentPath = CreateTestDirectory("parent");
        var childPath = CreateTestDirectory("parent/child");
        // Act
        var result = DirectoryHelper.IsSubDirectoryOf(parentPath, childPath);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsSubDirectoryOf - 相同目录返回true
    /// </summary>
    [Fact]
    public void IsSubDirectoryOf_SameDirectory_ReturnsTrue()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.IsSubDirectoryOf(testPath, testPath);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsSubDirectoryOf - 非子目录返回false
    /// </summary>
    [Fact]
    public void IsSubDirectoryOf_NotChildDirectory_ReturnsFalse()
    {
        // Arrange
        var parentPath = CreateTestDirectory("parent");
        var nonChildPath = CreateTestDirectory("notchild");
        // Act
        var result = DirectoryHelper.IsSubDirectoryOf(parentPath, nonChildPath);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsSubDirectoryOf - DirectoryInfo参数判断子目录关系
    /// </summary>
    [Fact]
    public void IsSubDirectoryOf_DirectoryInfoValidChild_ReturnsTrue()
    {
        // Arrange
        var parentPath = CreateTestDirectory("parent");
        var childPath = CreateTestDirectory("parent/child");
        var parentInfo = new DirectoryInfo(parentPath);
        var childInfo = new DirectoryInfo(childPath);
        // Act
        var result = DirectoryHelper.IsSubDirectoryOf(parentInfo, childInfo);
        // Assert
        result.ShouldBeTrue();
    }
    #endregion
    #region ChangeCurrentDirectory 测试
    /// <summary>
    /// 测试 - ChangeCurrentDirectory - 更改当前目录
    /// </summary>
    [Fact]
    public void ChangeCurrentDirectory_ValidDirectory_ChangesAndReturnsDisposable()
    {
        // Arrange
        var originalDirectory = Directory.GetCurrentDirectory();
        var testPath = CreateTestDirectory();
        // Act
        using var disposable = DirectoryHelper.ChangeCurrentDirectory(testPath);
        // Assert
        disposable.ShouldNotBeNull();
        Directory.GetCurrentDirectory().ShouldBe(testPath);
        // Dispose 后应该恢复原目录
        disposable.Dispose();
        Directory.GetCurrentDirectory().ShouldBe(originalDirectory);
    }
    /// <summary>
    /// 测试 - ChangeCurrentDirectory - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeCurrentDirectory_NullOrEmptyPath_ThrowsArgumentNullException(string targetDirectory)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.ChangeCurrentDirectory(targetDirectory));
    }
    /// <summary>
    /// 测试 - ChangeCurrentDirectory - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void ChangeCurrentDirectory_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.ChangeCurrentDirectory(nonExistentPath));
    }
    #endregion
    #region GetFiles 测试
    /// <summary>
    /// 测试 - GetFiles - 获取目录中的文件
    /// </summary>
    [Fact]
    public void GetFiles_ValidDirectory_ReturnsFiles()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var file1 = CreateTestFile(testPath, "test1.txt");
        var file2 = CreateTestFile(testPath, "test2.log");
        // Act
        var result = DirectoryHelper.GetFiles(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain(file1);
        result.ShouldContain(file2);
    }
    /// <summary>
    /// 测试 - GetFiles - 使用模式匹配获取文件
    /// </summary>
    [Fact]
    public void GetFiles_WithPattern_ReturnsMatchingFiles()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var txtFile = CreateTestFile(testPath, "test.txt");
        CreateTestFile(testPath, "test.log");
        // Act
        var result = DirectoryHelper.GetFiles(testPath, "*.txt");
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
        result.ShouldContain(txtFile);
    }
    /// <summary>
    /// 测试 - GetFiles - 包含子目录搜索
    /// </summary>
    [Fact]
    public void GetFiles_IncludeSubDirectories_ReturnsAllFiles()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var subPath = CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub"));
        var file1 = CreateTestFile(testPath, "test1.txt");
        var file2 = CreateTestFile(subPath, "test2.txt");
        // Act
        var result = DirectoryHelper.GetFiles(testPath, "*", true);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain(file1);
        result.ShouldContain(file2);
    }
    /// <summary>
    /// 测试 - GetFiles - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFiles_NullOrEmptyPath_ThrowsArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.GetFiles(directoryPath));
    }
    /// <summary>
    /// 测试 - GetFiles - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void GetFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.GetFiles(nonExistentPath));
    }
    #endregion
    #region GetFileNames 测试
    /// <summary>
    /// 测试 - GetFileNames - 获取文件名列表
    /// </summary>
    [Fact]
    public void GetFileNames_ValidDirectory_ReturnsFileNames()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test1.txt");
        CreateTestFile(testPath, "test2.log");
        // Act
        var result = DirectoryHelper.GetFileNames(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain("test1.txt");
        result.ShouldContain("test2.log");
    }
    /// <summary>
    /// 测试 - GetFileNames - 使用模式匹配获取文件名
    /// </summary>
    [Fact]
    public void GetFileNames_WithPattern_ReturnsMatchingFileNames()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.txt");
        CreateTestFile(testPath, "test.log");
        // Act
        var result = DirectoryHelper.GetFileNames(testPath, "*.txt");
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
        result.ShouldContain("test.txt");
    }
    #endregion
    #region GetDirectories 测试
    /// <summary>
    /// 测试 - GetDirectories - 获取子目录列表
    /// </summary>
    [Fact]
    public void GetDirectories_ValidDirectory_ReturnsDirectories()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var subDir1 = CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub1"));
        var subDir2 = CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub2"));
        // Act
        var result = DirectoryHelper.GetDirectories(testPath);
        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain(subDir1);
        result.ShouldContain(subDir2);
    }
    /// <summary>
    /// 测试 - GetDirectories - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetDirectories_NullOrEmptyPath_ThrowsArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.GetDirectories(directoryPath));
    }
    /// <summary>
    /// 测试 - GetDirectories - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void GetDirectories_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.GetDirectories(nonExistentPath));
    }
    #endregion
    #region Contains 测试
    /// <summary>
    /// 测试 - Contains - 目录包含匹配文件返回true
    /// </summary>
    [Fact]
    public void Contains_DirectoryContainsMatchingFiles_ReturnsTrue()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.txt");
        // Act
        var result = DirectoryHelper.Contains(testPath, "*.txt");
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - Contains - 目录不包含匹配文件返回false
    /// </summary>
    [Fact]
    public void Contains_DirectoryDoesNotContainMatchingFiles_ReturnsFalse()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.log");
        // Act
        var result = DirectoryHelper.Contains(testPath, "*.txt");
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region IsEmptyDirectory 测试
    /// <summary>
    /// 测试 - IsEmptyDirectory - 空目录返回true
    /// </summary>
    [Fact]
    public void IsEmptyDirectory_EmptyDirectory_ReturnsTrue()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.IsEmptyDirectory(testPath);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - IsEmptyDirectory - 包含文件的目录返回false
    /// </summary>
    [Fact]
    public void IsEmptyDirectory_DirectoryWithFiles_ReturnsFalse()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.txt");
        // Act
        var result = DirectoryHelper.IsEmptyDirectory(testPath);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsEmptyDirectory - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsEmptyDirectory_NullOrEmptyPath_ThrowsArgumentNullException(string folderPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.IsEmptyDirectory(folderPath));
    }
    /// <summary>
    /// 测试 - IsEmptyDirectory - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void IsEmptyDirectory_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.IsEmptyDirectory(nonExistentPath));
    }
    #endregion
    #region IsOverdueDirectory 测试
    /// <summary>
    /// 测试 - IsOverdueDirectory - 新创建的目录不过期
    /// </summary>
    [Fact]
    public void IsOverdueDirectory_NewDirectory_ReturnsFalse()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.IsOverdueDirectory(testPath, 1);
        // Assert
        result.ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - IsOverdueDirectory - 负数天数抛出异常
    /// </summary>
    [Fact]
    public void IsOverdueDirectory_NegativeDays_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => DirectoryHelper.IsOverdueDirectory(testPath, -1));
    }
    /// <summary>
    /// 测试 - IsOverdueDirectory - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsOverdueDirectory_NullOrEmptyPath_ThrowsArgumentNullException(string folderPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.IsOverdueDirectory(folderPath, 1));
    }
    #endregion
    #region GetDirectoryPath 测试
    /// <summary>
    /// 测试 - GetDirectoryPath - 获取目录路径
    /// </summary>
    [Theory]
    [InlineData("C:\\Users\\A\\", "C:/Users/A/")]
    [InlineData("C:/Users/A/", "C:/Users/A/")]
    [InlineData("/home/user/", "/home/user/")]
    [InlineData("single", "")]
    [InlineData("/", "")]
    public void GetDirectoryPath_ValidPath_ReturnsDirectoryPath(string input, string expected)
    {
        // Act
        var result = DirectoryHelper.GetDirectoryPath(input);
        // Assert
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - GetDirectoryPath - 空路径返回空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetDirectoryPath_NullOrEmptyPath_ReturnsEmpty(string path)
    {
        // Act
        var result = DirectoryHelper.GetDirectoryPath(path);
        // Assert
        result.ShouldBe(string.Empty);
    }
    #endregion
    #region Copy 测试
    /// <summary>
    /// 测试 - Copy - 递归复制文件夹
    /// </summary>
    [Fact]
    public void Copy_ValidDirectories_CopiesSuccessfully()
    {
        // Arrange
        var sourcePath = CreateTestDirectory("source");
        var targetPath = Path.Combine(_testRootPath, "target");
        var subDir = CreateTestDirectory("source/subdir");
        var file1 = CreateTestFile(sourcePath, "test1.txt");
        var file2 = CreateTestFile(subDir, "test2.txt");
        // Act
        DirectoryHelper.Copy(sourcePath, targetPath);
        // Assert
        Directory.Exists(targetPath).ShouldBeTrue();
        File.Exists(Path.Combine(targetPath, "test1.txt")).ShouldBeTrue();
        Directory.Exists(Path.Combine(targetPath, "subdir")).ShouldBeTrue();
        File.Exists(Path.Combine(targetPath, "subdir", "test2.txt")).ShouldBeTrue();
        _createdDirectories.Add(targetPath);
        _createdDirectories.Add(Path.Combine(targetPath, "subdir"));
    }
    /// <summary>
    /// 测试 - Copy - 使用搜索模式复制特定文件
    /// </summary>
    [Fact]
    public void Copy_WithSearchPatterns_CopiesMatchingFiles()
    {
        // Arrange
        var sourcePath = CreateTestDirectory("source");
        var targetPath = Path.Combine(_testRootPath, "target");
        CreateTestFile(sourcePath, "test.txt");
        CreateTestFile(sourcePath, "test.log");
        var searchPatterns = new[] { "*.txt" };
        // Act
        DirectoryHelper.Copy(sourcePath, targetPath, searchPatterns);
        // Assert
        Directory.Exists(targetPath).ShouldBeTrue();
        File.Exists(Path.Combine(targetPath, "test.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(targetPath, "test.log")).ShouldBeFalse();
        _createdDirectories.Add(targetPath);
    }
    /// <summary>
    /// 测试 - Copy - 源目录不存在抛出异常
    /// </summary>
    [Fact]
    public void Copy_NonExistentSourceDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var sourcePath = Path.Combine(_testRootPath, "NonExistent");
        var targetPath = Path.Combine(_testRootPath, "target");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.Copy(sourcePath, targetPath));
    }
    /// <summary>
    /// 测试 - Copy - 空源路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Copy_NullOrEmptySourcePath_ThrowsArgumentNullException(string sourcePath)
    {
        // Arrange
        var targetPath = Path.Combine(_testRootPath, "target");
        // Act & Assert
        Should.Throw<ArgumentException>(() => DirectoryHelper.Copy(sourcePath, targetPath));
    }
    #endregion
    #region Delete 测试
    /// <summary>
    /// 测试 - Delete - 递归删除目录
    /// </summary>
    [Fact]
    public void Delete_ValidDirectory_DeletesRecursively()
    {
        // Arrange
        var testPath = CreateTestDirectory("deletetest");
        var subDir = CreateTestDirectory("deletetest/subdir");
        CreateTestFile(testPath, "test.txt");
        CreateTestFile(subDir, "subtest.txt");
        // Act
        var result = DirectoryHelper.Delete(testPath);
        // Assert
        result.ShouldBeTrue();
        Directory.Exists(testPath).ShouldBeFalse();
        _createdDirectories.Remove(testPath);
        _createdDirectories.Remove(subDir);
    }
    /// <summary>
    /// 测试 - Delete - 不删除根目录
    /// </summary>
    [Fact]
    public void Delete_NotDeleteRoot_KeepsRootDirectory()
    {
        // Arrange
        var testPath = CreateTestDirectory("deleteroottest");
        CreateTestFile(testPath, "test.txt");
        // Act
        var result = DirectoryHelper.Delete(testPath, false);
        // Assert
        result.ShouldBeTrue();
        Directory.Exists(testPath).ShouldBeTrue();
        DirectoryHelper.IsEmptyDirectory(testPath).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - Delete - 不存在的目录返回false
    /// </summary>
    [Fact]
    public void Delete_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act
        var result = DirectoryHelper.Delete(nonExistentPath);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region TryClearFolder 测试
    /// <summary>
    /// 测试 - TryClearFolder - 清理文件夹内容
    /// </summary>
    [Fact]
    public void TryClearFolder_ValidDirectory_ClearsContents()
    {
        // Arrange
        var testPath = CreateTestDirectory("cleartest");
        var subDir = CreateTestDirectory("cleartest/subdir");
        CreateTestFile(testPath, "test.txt");
        CreateTestFile(subDir, "subtest.txt");
        // Act
        var result = DirectoryHelper.TryClearFolder(testPath);
        // Assert
        result.ShouldBeTrue();
        Directory.Exists(testPath).ShouldBeTrue();
        DirectoryHelper.IsEmptyDirectory(testPath).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - TryClearFolder - 不存在的目录返回false
    /// </summary>
    [Fact]
    public void TryClearFolder_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act
        var result = DirectoryHelper.TryClearFolder(nonExistentPath);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region TryClearOverdueFolder 测试
    /// <summary>
    /// 测试 - TryClearOverdueFolder - 清理过期文件夹
    /// </summary>
    [Fact]
    public void TryClearOverdueFolder_ValidDirectory_ReturnsTrue()
    {
        // Arrange
        var testPath = CreateTestDirectory("overduetest");
        CreateTestFile(testPath, "test.txt");
        // Act
        var result = DirectoryHelper.TryClearOverdueFolder(testPath, 365);
        // Assert
        result.ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - TryClearOverdueFolder - 不存在的目录返回false
    /// </summary>
    [Fact]
    public void TryClearOverdueFolder_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act
        var result = DirectoryHelper.TryClearOverdueFolder(nonExistentPath, 1);
        // Assert
        result.ShouldBeFalse();
    }
    #endregion
    #region SetAttributes 测试
    /// <summary>
    /// 测试 - SetAttributes - 设置目录属性
    /// </summary>
    [Fact]
    public void SetAttributes_ValidDirectory_SetsAttributes()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var originalAttributes = new DirectoryInfo(testPath).Attributes;
        // Act
        DirectoryHelper.SetAttributes(testPath, FileAttributes.ReadOnly, true);
        // Assert
        var newAttributes = new DirectoryInfo(testPath).Attributes;
        (newAttributes & FileAttributes.ReadOnly).ShouldBe(FileAttributes.ReadOnly);
        // 清理 - 移除只读属性
        DirectoryHelper.SetAttributes(testPath, FileAttributes.ReadOnly, false);
    }
    /// <summary>
    /// 测试 - SetAttributes - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetAttributes_NullOrEmptyPath_ThrowsArgumentNullException(string directory)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.SetAttributes(directory, FileAttributes.ReadOnly, true));
    }
    /// <summary>
    /// 测试 - SetAttributes - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void SetAttributes_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.SetAttributes(nonExistentPath, FileAttributes.ReadOnly, true));
    }
    #endregion
    #region SetCurrentDirectory 测试
    /// <summary>
    /// 测试 - SetCurrentDirectory - 设置当前目录
    /// </summary>
    [Fact]
    public void SetCurrentDirectory_ValidPath_SetsCurrentDirectory()
    {
        // Arrange
        var originalDirectory = DirectoryHelper.GetCurrentDirectory();
        var testPath = CreateTestDirectory();
        // Act
        var result = DirectoryHelper.SetCurrentDirectory(testPath);
        // Assert
        result.ShouldBe(testPath);
        DirectoryHelper.GetCurrentDirectory().ShouldBe(testPath);
        // 恢复原目录
        DirectoryHelper.SetCurrentDirectory(originalDirectory);
    }
    #endregion
    #region GetCurrentDirectory 测试
    /// <summary>
    /// 测试 - GetCurrentDirectory - 获取当前目录
    /// </summary>
    [Fact]
    public void GetCurrentDirectory_ReturnsCurrentDirectory()
    {
        // Act
        var result = DirectoryHelper.GetCurrentDirectory();
        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        Directory.Exists(result).ShouldBeTrue();
    }
    #endregion
    #region GetDirectorySize 测试
    /// <summary>
    /// 测试 - GetDirectorySize - 获取目录大小
    /// </summary>
    [Fact]
    public void GetDirectorySize_ValidDirectory_ReturnsSize()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var file1 = CreateTestFile(testPath, "test1.txt", "hello");
        var file2 = CreateTestFile(testPath, "test2.txt", "world");
        // Act
        var result = DirectoryHelper.GetDirectorySize(testPath);
        // Assert
        result.ShouldBeGreaterThan(0);
        result.ShouldBe(10); // "hello" + "world" = 10 bytes
    }
    /// <summary>
    /// 测试 - GetDirectorySize - DirectoryInfo参数获取目录大小
    /// </summary>
    [Fact]
    public void GetDirectorySize_DirectoryInfo_ReturnsSize()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test.txt", "content");
        var directoryInfo = new DirectoryInfo(testPath);
        // Act
        var result = DirectoryHelper.GetDirectorySize(directoryInfo);
        // Assert
        result.ShouldBeGreaterThan(0);
        result.ShouldBe(7); // "content" = 7 bytes
    }
    /// <summary>
    /// 测试 - GetDirectorySize - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetDirectorySize_NullOrEmptyPath_ThrowsArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.GetDirectorySize(directoryPath));
    }
    /// <summary>
    /// 测试 - GetDirectorySize - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void GetDirectorySize_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.GetDirectorySize(nonExistentPath));
    }
    /// <summary>
    /// 测试 - GetDirectorySize - null DirectoryInfo返回0
    /// </summary>
    [Fact]
    public void GetDirectorySize_NullDirectoryInfo_ReturnsZero()
    {
        // Act
        var result = DirectoryHelper.GetDirectorySize((DirectoryInfo)null);
        // Assert
        result.ShouldBe(0);
    }
    #endregion
    #region Move 测试
    /// <summary>
    /// 测试 - Move - 移动目录到新位置
    /// </summary>
    [Fact]
    public void Move_ValidDirectories_MovesSuccessfully()
    {
        // Arrange
        var sourcePath = CreateTestDirectory("movesource");
        var destinationPath = Path.Combine(_testRootPath, "movedest");
        CreateTestFile(sourcePath, "test.txt");
        // Act
        DirectoryHelper.Move(sourcePath, destinationPath);
        // Assert
        Directory.Exists(sourcePath).ShouldBeFalse();
        Directory.Exists(destinationPath).ShouldBeTrue();
        File.Exists(Path.Combine(destinationPath, "test.txt")).ShouldBeTrue();
        _createdDirectories.Remove(sourcePath);
        _createdDirectories.Add(destinationPath);
    }
    /// <summary>
    /// 测试 - Move - 目标目录已存在且不允许覆盖抛出异常
    /// </summary>
    [Fact]
    public void Move_DestinationExistsNoOverwrite_ThrowsInvalidOperationException()
    {
        // Arrange
        var sourcePath = CreateTestDirectory("movesource2");
        var destinationPath = CreateTestDirectory("movedest2");
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => DirectoryHelper.Move(sourcePath, destinationPath, false));
    }
    /// <summary>
    /// 测试 - Move - 允许覆盖目标目录
    /// </summary>
    [Fact]
    public void Move_DestinationExistsWithOverwrite_MovesSuccessfully()
    {
        // Arrange
        var sourcePath = CreateTestDirectory("movesource3");
        var destinationPath = CreateTestDirectory("movedest3");
        CreateTestFile(sourcePath, "test.txt");
        // Act
        DirectoryHelper.Move(sourcePath, destinationPath, true);
        // Assert
        Directory.Exists(sourcePath).ShouldBeFalse();
        Directory.Exists(destinationPath).ShouldBeTrue();
        File.Exists(Path.Combine(destinationPath, "test.txt")).ShouldBeTrue();
        _createdDirectories.Remove(sourcePath);
    }
    /// <summary>
    /// 测试 - Move - 空源路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Move_NullOrEmptySourcePath_ThrowsArgumentNullException(string sourcePath)
    {
        // Arrange
        var destinationPath = Path.Combine(_testRootPath, "destination");
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.Move(sourcePath, destinationPath));
    }
    /// <summary>
    /// 测试 - Move - 空目标路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Move_NullOrEmptyDestinationPath_ThrowsArgumentNullException(string destinationPath)
    {
        // Arrange
        var sourcePath = CreateTestDirectory();
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.Move(sourcePath, destinationPath));
    }
    /// <summary>
    /// 测试 - Move - 源目录不存在抛出异常
    /// </summary>
    [Fact]
    public void Move_NonExistentSourceDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var sourcePath = Path.Combine(_testRootPath, "NonExistent");
        var destinationPath = Path.Combine(_testRootPath, "destination");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.Move(sourcePath, destinationPath));
    }
    #endregion
    #region GetFileCount 测试
    /// <summary>
    /// 测试 - GetFileCount - 获取文件数量
    /// </summary>
    [Fact]
    public void GetFileCount_ValidDirectory_ReturnsCount()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestFile(testPath, "test1.txt");
        CreateTestFile(testPath, "test2.txt");
        // Act
        var result = DirectoryHelper.GetFileCount(testPath);
        // Assert
        result.ShouldBe(2);
    }
    /// <summary>
    /// 测试 - GetFileCount - 包含子目录获取文件数量
    /// </summary>
    [Fact]
    public void GetFileCount_IncludeSubDirectories_ReturnsAllCount()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var subDir = CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub"));
        CreateTestFile(testPath, "test1.txt");
        CreateTestFile(subDir, "test2.txt");
        // Act
        var result = DirectoryHelper.GetFileCount(testPath, true);
        // Assert
        result.ShouldBe(2);
    }
    /// <summary>
    /// 测试 - GetFileCount - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileCount_NullOrEmptyPath_ThrowsArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.GetFileCount(directoryPath));
    }
    /// <summary>
    /// 测试 - GetFileCount - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void GetFileCount_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.GetFileCount(nonExistentPath));
    }
    #endregion
    #region GetDirectoryCount 测试
    /// <summary>
    /// 测试 - GetDirectoryCount - 获取子目录数量
    /// </summary>
    [Fact]
    public void GetDirectoryCount_ValidDirectory_ReturnsCount()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub1"));
        CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub2"));
        // Act
        var result = DirectoryHelper.GetDirectoryCount(testPath);
        // Assert
        result.ShouldBe(2);
    }
    /// <summary>
    /// 测试 - GetDirectoryCount - 包含子目录获取目录数量
    /// </summary>
    [Fact]
    public void GetDirectoryCount_IncludeSubDirectories_ReturnsAllCount()
    {
        // Arrange
        var testPath = CreateTestDirectory();
        var subDir1 = CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub1"));
        CreateTestDirectory(Path.Combine(Path.GetFileName(testPath), "sub1", "subsub"));
        // Act
        var result = DirectoryHelper.GetDirectoryCount(testPath, true);
        // Assert
        result.ShouldBe(2); // sub1 + subsub
    }
    /// <summary>
    /// 测试 - GetDirectoryCount - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetDirectoryCount_NullOrEmptyPath_ThrowsArgumentNullException(string directoryPath)
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => DirectoryHelper.GetDirectoryCount(directoryPath));
    }
    /// <summary>
    /// 测试 - GetDirectoryCount - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void GetDirectoryCount_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRootPath, "NonExistent");
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => DirectoryHelper.GetDirectoryCount(nonExistentPath));
    }
    #endregion
}
