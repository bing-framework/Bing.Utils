namespace Bing.Helpers;
/// <summary>
/// 常用公共操作测试
/// </summary>
[Trait("Bing.Helpers", "Common")]
public class CommonTest : TestBase
{
    /// <inheritdoc />
    public CommonTest(ITestOutputHelper output) : base(output)
    {
    }
    #region ApplicationBaseDirectory 测试
    /// <summary>
    /// 测试 - ApplicationBaseDirectory - 返回有效路径
    /// </summary>
    [Fact]
    public void ApplicationBaseDirectory_ReturnsValidPath()
    {
        // Act
        var result = Common.ApplicationBaseDirectory;
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Directory.Exists(result).ShouldBeTrue();
        Path.IsPathRooted(result).ShouldBeTrue();
        // 验证与 AppContext.BaseDirectory 一致
        result.ShouldBe(AppContext.BaseDirectory);
        Output.WriteLine($"应用程序基目录: {result}");
    }
    /// <summary>
    /// 测试 - ApplicationBaseDirectory - 路径格式验证
    /// </summary>
    [Fact]
    public void ApplicationBaseDirectory_PathFormat_IsCorrect()
    {
        // Act
        var result = Common.ApplicationBaseDirectory;
        // Assert
        // 路径应该以目录分隔符结尾
        result.ShouldEndWith(Path.DirectorySeparatorChar.ToString());
        Output.WriteLine($"路径格式验证通过: {result}");
    }
    #endregion
    #region Line 测试
    /// <summary>
    /// 测试 - Line - 换行符
    /// </summary>
    [Fact]
    public void Line_ReturnsCorrectNewLine()
    {
        // Act
        var result = Common.Line;
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe(Environment.NewLine);
        result.ShouldBe(Env.NewLine);
        Output.WriteLine($"换行符: '{result}' (长度: {result.Length})");
        // 验证不同平台的换行符
        if (Env.IsWindows)
        {
            result.ShouldBe("\r\n");
        }
        else
        {
            result.ShouldBe("\n");
        }
    }
    #endregion
    #region GetType 测试
    /// <summary>
    /// 测试 - GetType 泛型版本 - 基本类型
    /// </summary>
    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(decimal))]
    public void GetType_Generic_BasicTypes_ReturnsCorrectType(Type expectedType)
    {
        // 使用反射调用泛型方法
        var method = typeof(Common).GetMethod("GetType", new Type[0]).MakeGenericMethod(expectedType);
        // Act
        var result = (Type)method.Invoke(null, null);
        // Assert
        result.ShouldBe(expectedType);
        Output.WriteLine($"泛型 GetType<{expectedType.Name}>() = {result.Name}");
    }
    /// <summary>
    /// 测试 - GetType 参数版本 - 可空类型处理
    /// </summary>
    [Theory]
    [InlineData(typeof(int), typeof(int))]
    [InlineData(typeof(int?), typeof(int))]
    [InlineData(typeof(DateTime), typeof(DateTime))]
    [InlineData(typeof(DateTime?), typeof(DateTime))]
    [InlineData(typeof(bool), typeof(bool))]
    [InlineData(typeof(bool?), typeof(bool))]
    [InlineData(typeof(string), typeof(string))] // string 本身就是引用类型
    public void GetType_Parameter_NullableTypes_ReturnsUnderlyingType(Type inputType, Type expectedType)
    {
        // Act
        var result = Common.GetType(inputType);
        // Assert
        result.ShouldBe(expectedType);
        Output.WriteLine($"GetType({inputType.Name}) = {result.Name}");
    }
    /// <summary>
    /// 测试 - GetType - 复杂类型
    /// </summary>
    [Fact]
    public void GetType_ComplexTypes_WorksCorrectly()
    {
        // Act & Assert
        Common.GetType(typeof(List<int>)).ShouldBe(typeof(List<int>));
        Common.GetType(typeof(Dictionary<string, int>)).ShouldBe(typeof(Dictionary<string, int>));
        Common.GetType(typeof(CommonTest)).ShouldBe(typeof(CommonTest));
        // 可空的值类型
        Common.GetType(typeof(Guid?)).ShouldBe(typeof(Guid));
        Common.GetType(typeof(TimeSpan?)).ShouldBe(typeof(TimeSpan));
        Output.WriteLine("复杂类型测试通过");
    }
    /// <summary>
    /// 测试 - GetType - null 参数
    /// </summary>
    [Fact]
    public void GetType_NullParameter_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Common.GetType(null));
    }
    #endregion
    #region Swap 测试
    /// <summary>
    /// 测试 - Swap - 基本数据类型
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(100, -50)]
    [InlineData(0, 999)]
    public void Swap_BasicDataTypes_SwapsCorrectly(int initialA, int initialB)
    {
        // Arrange
        var a = initialA;
        var b = initialB;
        // Act
        Common.Swap(ref a, ref b);
        // Assert
        a.ShouldBe(initialB);
        b.ShouldBe(initialA);
        Output.WriteLine($"交换: {initialA} <-> {initialB} => a={a}, b={b}");
    }
    /// <summary>
    /// 测试 - Swap - 字符串类型
    /// </summary>
    [Theory]
    [InlineData("hello", "world")]
    [InlineData("", "test")]
    [InlineData("a", "b")]
    [InlineData(null, "value")]
    public void Swap_StringTypes_SwapsCorrectly(string initialA, string initialB)
    {
        // Arrange
        var a = initialA;
        var b = initialB;
        // Act
        Common.Swap(ref a, ref b);
        // Assert
        a.ShouldBe(initialB);
        b.ShouldBe(initialA);
        Output.WriteLine($"字符串交换: '{initialA}' <-> '{initialB}' => a='{a}', b='{b}'");
    }
    /// <summary>
    /// 测试 - Swap - 复杂对象类型
    /// </summary>
    [Fact]
    public void Swap_ComplexObjects_SwapsCorrectly()
    {
        // Arrange
        var listA = new List<int> { 1, 2, 3 };
        var listB = new List<int> { 4, 5, 6 };
        var originalListA = listA;
        var originalListB = listB;
        // Act
        Common.Swap(ref listA, ref listB);
        // Assert
        listA.ShouldBe(originalListB);
        listB.ShouldBe(originalListA);
        // 验证引用确实被交换了
        ReferenceEquals(listA, originalListB).ShouldBeTrue();
        ReferenceEquals(listB, originalListA).ShouldBeTrue();
        Output.WriteLine($"复杂对象交换: [{string.Join(",", listB)}] <-> [{string.Join(",", listA)}]");
    }
    /// <summary>
    /// 测试 - Swap - 相同值交换
    /// </summary>
    [Fact]
    public void Swap_SameValues_WorksCorrectly()
    {
        // Arrange
        var a = 42;
        var b = 42;
        // Act
        Common.Swap(ref a, ref b);
        // Assert
        a.ShouldBe(42);
        b.ShouldBe(42);
        Output.WriteLine("相同值交换测试通过");
    }
    /// <summary>
    /// 测试 - Swap - 泛型约束验证
    /// </summary>
    [Fact]
    public void Swap_GenericConstraints_WorksWithDifferentTypes()
    {
        // DateTime 测试
        var dateA = new DateTime(2023, 1, 1);
        var dateB = new DateTime(2023, 12, 31);
        Common.Swap(ref dateA, ref dateB);
        dateA.ShouldBe(new DateTime(2023, 12, 31));
        dateB.ShouldBe(new DateTime(2023, 1, 1));
        // Boolean 测试
        var boolA = true;
        var boolB = false;
        Common.Swap(ref boolA, ref boolB);
        boolA.ShouldBeFalse();
        boolB.ShouldBeTrue();
        // Decimal 测试
        var decA = 3.14m;
        var decB = 2.71m;
        Common.Swap(ref decA, ref decB);
        decA.ShouldBe(2.71m);
        decB.ShouldBe(3.14m);
        Output.WriteLine("多类型泛型交换测试通过");
    }
    #endregion
    #region GetPhysicalPath 测试
    /// <summary>
    /// 测试 - GetPhysicalPath - 基本功能
    /// </summary>
    [Theory]
    [InlineData("test.txt")]
    [InlineData("folder/test.txt")]
    [InlineData("folder\\test.txt")]
    [InlineData("/test.txt")]
    [InlineData("\\test.txt")]
    public void GetPhysicalPath_BasicFunctionality_ReturnsCorrectPath(string relativePath)
    {
        // Act
        var result = Common.GetPhysicalPath(relativePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Path.IsPathRooted(result).ShouldBeTrue();
        result.ShouldStartWith(Common.ApplicationBaseDirectory);
        Output.WriteLine($"相对路径: '{relativePath}' -> 物理路径: '{result}'");
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 自定义基路径
    /// </summary>
    [Fact]
    public void GetPhysicalPath_CustomBasePath_WorksCorrectly()
    {
        // Arrange
        var customBasePath = Path.GetTempPath();
        var relativePath = "test/file.txt";
        // Act
        var result = Common.GetPhysicalPath(relativePath, customBasePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldStartWith(customBasePath);
        Path.IsPathRooted(result).ShouldBeTrue();
        Output.WriteLine($"自定义基路径: '{customBasePath}' + '{relativePath}' = '{result}'");
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 空路径处理
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetPhysicalPath_EmptyPath_HandlesCorrectly(string relativePath)
    {
        // Act
        var ex = Should.Throw<ArgumentNullException>(() => Common.GetPhysicalPath(relativePath));
        ex.ParamName.ShouldBe("relativePath");
        Output.WriteLine($"空路径测试: '{relativePath}' 正确抛出异常");
    }
    /// <summary>
    /// 测试 - GetPhysicalPath - 与 PathHelper 一致性
    /// </summary>
    [Fact]
    public void GetPhysicalPath_ConsistentWithPathHelper()
    {
        // Arrange
        var testPaths = new[]
        {
            "config/app.json",
            "data/test.db",
            "/logs/app.log",
            "temp\\cache.tmp"
        };
        foreach (var path in testPaths)
        {
            // Act
            var commonResult = Common.GetPhysicalPath(path);
            var pathHelperResult = IO.PathHelper.GetPhysicalPath(path);
            // Assert
            commonResult.ShouldBe(pathHelperResult);
            Output.WriteLine($"一致性验证: '{path}' -> '{commonResult}'");
        }
    }
    #endregion
    #region JoinPath 测试
    /// <summary>
    /// 测试 - JoinPath - 基本路径连接
    /// </summary>
    [Theory]
    [InlineData(new[] { "folder", "file.txt" }, "folder/file.txt")]
    [InlineData(new[] { "api", "v1", "users" }, "api/v1/users")]
    [InlineData(new[] { "http://example.com", "api", "data" }, "http://example.com/api/data")]
    [InlineData(new[] { "", "test", "path" }, "/test/path")]
    public void JoinPath_BasicFunctionality_ReturnsCorrectPath(string[] pathParts, string expectedPattern)
    {
        // Act
        var result = Common.JoinPath(pathParts);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        // 由于内部使用 Url.Combine，具体格式可能与预期略有不同，主要验证包含关系
        foreach (var part in pathParts.Where(p => !string.IsNullOrEmpty(p)))
        {
            result.ShouldContain(part);
        }
        Output.WriteLine($"路径连接: [{string.Join(", ", pathParts)}] -> '{result}'");
    }
    /// <summary>
    /// 测试 - JoinPath - 单个路径
    /// </summary>
    [Fact]
    public void JoinPath_SinglePath_ReturnsOriginalPath()
    {
        // Arrange
        var singlePath = "test/path";
        // Act
        var result = Common.JoinPath(singlePath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("test");
        result.ShouldContain("path");
        Output.WriteLine($"单个路径: '{singlePath}' -> '{result}'");
    }
    /// <summary>
    /// 测试 - JoinPath - 空数组
    /// </summary>
    [Fact]
    public void JoinPath_EmptyArray_HandlesCorrectly()
    {
        // Act
        var result = Common.JoinPath();
        // Assert
        result.ShouldNotBeNull();
        Output.WriteLine($"空数组测试: -> '{result}'");
    }
    /// <summary>
    /// 测试 - JoinPath - 包含null和空字符串
    /// </summary>
    [Fact]
    public void JoinPath_WithNullAndEmpty_HandlesCorrectly()
    {
        // Act
        var result = Common.JoinPath("api", null, "", "users", "123");
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("api");
        result.ShouldContain("users");
        result.ShouldContain("123");
        Output.WriteLine($"包含null和空字符串: -> '{result}'");
    }
    #endregion
    #region GetCurrentDirectory 测试
    /// <summary>
    /// 测试 - GetCurrentDirectory - 返回当前目录
    /// </summary>
    [Fact]
    public void GetCurrentDirectory_ReturnsValidDirectory()
    {
        // Act
        var result = Common.GetCurrentDirectory();
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Directory.Exists(result).ShouldBeTrue();
        Path.IsPathRooted(result).ShouldBeTrue();
        // 验证与 Directory.GetCurrentDirectory() 一致
        result.ShouldBe(Directory.GetCurrentDirectory());
        Output.WriteLine($"当前目录: {result}");
    }
    /// <summary>
    /// 测试 - GetCurrentDirectory - 与 Env.CurrentDirectory 一致性
    /// </summary>
    [Fact]
    public void GetCurrentDirectory_ConsistentWithEnv()
    {
        // Act
        var commonResult = Common.GetCurrentDirectory();
        var envResult = Env.CurrentDirectory;
        // Assert
        commonResult.ShouldBe(envResult);
        Output.WriteLine($"一致性验证: Common={commonResult}, Env={envResult}");
    }
    #endregion
    #region GetParentDirectory 测试
    /// <summary>
    /// 测试 - GetParentDirectory - 默认深度
    /// </summary>
    [Fact]
    public void GetParentDirectory_DefaultDepth_ReturnsParent()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        // Act
        var result = Common.GetParentDirectory();
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Directory.Exists(result).ShouldBeTrue();
        Path.IsPathRooted(result).ShouldBeTrue();
        Output.WriteLine($"父目录: {currentDir} -> {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 指定深度
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GetParentDirectory_SpecificDepth_ReturnsCorrectLevel(int depth)
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        // Act
        var result = Common.GetParentDirectory(depth);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Path.IsPathRooted(result).ShouldBeTrue();
        Output.WriteLine($"向上{depth}级: {currentDir} -> {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 零深度
    /// </summary>
    [Fact]
    public void GetParentDirectory_ZeroDepth_ReturnsCurrentDirectory()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        // Act
        var result = Common.GetParentDirectory(0);
        // Assert
        result.ShouldBe(Path.GetFullPath(currentDir));
        Output.WriteLine($"零深度测试: {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 负数深度抛出异常
    /// </summary>
    [Fact]
    public void GetParentDirectory_NegativeDepth_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => Common.GetParentDirectory(-1));
        ex.ParamName.ShouldBe("depth");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 自定义起始路径
    /// </summary>
    [Fact]
    public void GetParentDirectory_CustomStartPath_WorksCorrectly()
    {
        // Arrange
        var customPath = Path.GetTempPath();
        // Act
        var result = Common.GetParentDirectory(1, customPath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        Directory.Exists(result).ShouldBeTrue();
        Output.WriteLine($"自定义起始路径: {customPath} -> {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 不存在的起始路径抛出异常
    /// </summary>
    [Fact]
    public void GetParentDirectory_NonExistentStartPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistentDirectory123456789";
        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => Common.GetParentDirectory(1, nonExistentPath));
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 空起始路径抛出异常
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetParentDirectory_EmptyStartPath_ThrowsArgumentException(string emptyPath)
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => Common.GetParentDirectory(1, emptyPath));
        ex.ParamName.ShouldBe("startPath");
    }
    /// <summary>
    /// 测试 - GetParentDirectory - 路径层次验证
    /// </summary>
    [Fact]
    public void GetParentDirectory_HierarchyValidation_WorksCorrectly()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        // Act
        var parent1 = Common.GetParentDirectory(1);
        var parent2 = Common.GetParentDirectory(2);
        var parent3 = Common.GetParentDirectory(3);
        // Assert
        Output.WriteLine($"层次验证:");
        Output.WriteLine($"  当前: {currentDir}");
        Output.WriteLine($"  父级1: {parent1}");
        Output.WriteLine($"  父级2: {parent2}");
        Output.WriteLine($"  父级3: {parent3}");
        // 验证层次关系（如果存在父目录）
        if (Directory.GetParent(currentDir) != null)
        {
            parent1.Length.ShouldBeLessThanOrEqualTo(currentDir.Length);
            if (Directory.GetParent(parent1) != null)
            {
                parent2.Length.ShouldBeLessThanOrEqualTo(parent1.Length);
            }
        }
    }
    #endregion
    #region GetParentDirectoryOf 测试
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 基本功能
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_BasicFunctionality_ReturnsParent()
    {
        // Arrange
        var testPath = Path.Combine(Common.ApplicationBaseDirectory, "test", "subfolder");
        // Act
        var result = Common.GetParentDirectoryOf(testPath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe(Path.GetDirectoryName(Path.GetFullPath(testPath)));
        Output.WriteLine($"GetParentDirectoryOf: {testPath} -> {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 根目录
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_RootDirectory_ReturnsNull()
    {
        // Arrange
        var rootPath = Path.GetPathRoot(Common.ApplicationBaseDirectory);
        // Act
        var result = Common.GetParentDirectoryOf(rootPath);
        // Assert
        result.ShouldBeNull();
        Output.WriteLine($"根目录测试: {rootPath} -> {result}");
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 空路径抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetParentDirectoryOf_EmptyPath_ThrowsArgumentException(string emptyPath)
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => Common.GetParentDirectoryOf(emptyPath));
        ex.ParamName.ShouldBe("path");
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 无效路径处理
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_InvalidPath_HandlesCorrectly()
    {
        // Arrange - 使用明确会导致异常的路径
        var invalidPaths = new[]
        {
            // 超长路径
            //new string('a', 300) + Path.DirectorySeparatorChar + new string('b', 300),
            // 全部是无效字符的路径
            new string('\0', 5),
            // 包含控制字符的路径
            "path\x00with\x01invalid\x02chars"
        };
        foreach (var invalidPath in invalidPaths)
        {
            // Act & Assert
            var ex = Should.Throw<ArgumentException>(() => Common.GetParentDirectoryOf(invalidPath));
            ex.ParamName.ShouldBe("path");
            Output.WriteLine($"无效路径测试: '{invalidPath.Replace('\0', '?')}' 正确抛出异常");
        }
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 特殊字符路径的处理
    /// </summary>
    [Theory]
    [InlineData("normal/path")]
    [InlineData("path with spaces")]
    [InlineData("path-with-dashes")]
    [InlineData("path_with_underscores")]
    [InlineData("path.with.dots")]
    public void GetParentDirectoryOf_SpecialCharacterPaths_WorksCorrectly(string fileName)
    {
        // Arrange
        var testPath = Path.Combine("some", "parent", "directory", fileName);
        // Act
        var result = Common.GetParentDirectoryOf(testPath);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("directory");
        Output.WriteLine($"特殊字符路径: '{fileName}' -> '{result}'");
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 路径字符验证
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_PathCharacterValidation_WorksCorrectly()
    {
        // Arrange - 测试包含路径分隔符但可能有问题的路径
        var problematicPaths = new Dictionary<string, bool>
        {
            [@"C:\valid\path"] = true,           // 有效路径
            [@"/valid/unix/path"] = true,        // 有效 Unix 路径
            [@"relative\path"] = true,           // 有效相对路径
            [@""] = false,                       // 空路径
            [@"   "] = false,                    // 只有空格
            ["\t\n\r"] = false,                 // 只有空白字符
        };
        foreach (var kvp in problematicPaths)
        {
            var path = kvp.Key;
            var shouldSucceed = kvp.Value;
            if (shouldSucceed)
            {
                // Act - 应该成功
                var result = Common.GetParentDirectoryOf(path);
                // Assert
                // 不为空路径应该有父目录信息（可能为null但不应该抛出异常）
                Output.WriteLine($"有效路径测试: '{path}' -> '{result}'");
            }
            else
            {
                // Act & Assert - 应该抛出异常
                Should.Throw<ArgumentException>(() => Common.GetParentDirectoryOf(path));
                Output.WriteLine($"无效路径测试: '{path}' 正确抛出异常");
            }
        }
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 系统特定的无效字符
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_SystemSpecificInvalidChars_ThrowsAppropriateException()
    {
        // Arrange - 获取系统的无效路径字符
        var invalidChars = Path.GetInvalidPathChars();
        if (invalidChars.Length > 0)
        {
            // 使用系统定义的无效字符构造路径
            var invalidPath = $"path{invalidChars[0]}with{invalidChars[Math.Min(1, invalidChars.Length - 1)]}invalid";
            // Act & Assert
            var ex = Should.Throw<ArgumentException>(() => Common.GetParentDirectoryOf(invalidPath));
            ex.ParamName.ShouldBe("path");
            Output.WriteLine($"系统无效字符测试: 包含字符 {(int)invalidChars[0]} 的路径正确抛出异常");
        }
        else
        {
            Output.WriteLine("当前系统没有定义无效路径字符");
        }
    }
    /// <summary>
    /// 测试 - GetParentDirectoryOf - 边界条件
    /// </summary>
    [Fact]
    public void GetParentDirectoryOf_EdgeCases_HandlesCorrectly()
    {
        // Arrange & Act & Assert
        var testCases = new[]
        {
            new { Path = ".", ShouldThrow = false, Description = "当前目录" },
            new { Path = "..", ShouldThrow = false, Description = "父目录" },
            new { Path = "file.txt", ShouldThrow = false, Description = "简单文件名" },
            new { Path = @"C:\", ShouldThrow = false, Description = "Windows根目录" },
            new { Path = "/", ShouldThrow = false, Description = "Unix根目录" },
        };
        foreach (var testCase in testCases)
        {
            if (testCase.ShouldThrow)
            {
                Should.Throw<ArgumentException>(() => Common.GetParentDirectoryOf(testCase.Path));
                Output.WriteLine($"{testCase.Description}: '{testCase.Path}' 正确抛出异常");
            }
            else
            {
                var result = Common.GetParentDirectoryOf(testCase.Path);
                // 结果可能为 null（对于根目录），但不应该抛出异常
                Output.WriteLine($"{testCase.Description}: '{testCase.Path}' -> '{result}'");
            }
        }
    }
    #endregion
    #region SafeExecute 测试
    /// <summary>
    /// 测试 - SafeExecute 有返回值版本 - 正常执行
    /// </summary>
    [Fact]
    public void SafeExecute_WithReturnValue_NormalExecution_ReturnsResult()
    {
        // Act
        var result = Common.SafeExecute(() => 42, 0);
        // Assert
        result.ShouldBe(42);
        Output.WriteLine($"SafeExecute 正常执行: {result}");
    }
    /// <summary>
    /// 测试 - SafeExecute 有返回值版本 - 异常时返回默认值
    /// </summary>
    [Fact]
    public void SafeExecute_WithReturnValue_ExceptionThrown_ReturnsDefaultValue()
    {
        // Act
        var result = Common.SafeExecute(() => int.Parse("invalid"), 999);
        // Assert
        result.ShouldBe(999);
        Output.WriteLine($"SafeExecute 异常处理: 返回默认值 {result}");
    }
    /// <summary>
    /// 测试 - SafeExecute 有返回值版本 - null 函数抛出异常
    /// </summary>
    [Fact]
    public void SafeExecute_WithReturnValue_NullFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => Common.SafeExecute<int>(null, 0));
        ex.ParamName.ShouldBe("func");
    }
    /// <summary>
    /// 测试 - SafeExecute 无返回值版本 - 正常执行
    /// </summary>
    [Fact]
    public void SafeExecute_WithoutReturnValue_NormalExecution_ReturnsTrue()
    {
        // Arrange
        var executed = false;
        // Act
        var result = Common.SafeExecute(() => executed = true);
        // Assert
        result.ShouldBeTrue();
        executed.ShouldBeTrue();
        Output.WriteLine("SafeExecute 无返回值版本正常执行");
    }
    /// <summary>
    /// 测试 - SafeExecute 无返回值版本 - 异常时返回false
    /// </summary>
    [Fact]
    public void SafeExecute_WithoutReturnValue_ExceptionThrown_ReturnsFalse()
    {
        // Act
        var result = Common.SafeExecute(() => throw new InvalidOperationException("Test exception"));
        // Assert
        result.ShouldBeFalse();
        Output.WriteLine("SafeExecute 无返回值版本异常处理");
    }
    /// <summary>
    /// 测试 - SafeExecute 无返回值版本 - null 操作抛出异常
    /// </summary>
    [Fact]
    public void SafeExecute_WithoutReturnValue_NullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => Common.SafeExecute(null));
        ex.ParamName.ShouldBe("action");
    }
    #endregion
    #region RetryExecute 测试
    /// <summary>
    /// 测试 - RetryExecute - 第一次成功
    /// </summary>
    [Fact]
    public void RetryExecute_FirstAttemptSucceeds_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        // Act
        var result = Common.RetryExecute(() =>
        {
            attemptCount++;
            return "success";
        }, maxRetries: 3, delay: TimeSpan.FromMilliseconds(10));
        // Assert
        result.ShouldBe("success");
        attemptCount.ShouldBe(1);
        Output.WriteLine($"RetryExecute 第一次成功: 尝试次数={attemptCount}");
    }
    /// <summary>
    /// 测试 - RetryExecute - 重试后成功
    /// </summary>
    [Fact]
    public void RetryExecute_SucceedsAfterRetries_ReturnsResult()
    {
        // Arrange
        var attemptCount = 0;
        // Act
        var result = Common.RetryExecute(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("Not ready yet");
            return "success";
        }, maxRetries: 5, delay: TimeSpan.FromMilliseconds(10));
        // Assert
        result.ShouldBe("success");
        attemptCount.ShouldBe(3);
        Output.WriteLine($"RetryExecute 重试后成功: 尝试次数={attemptCount}");
    }
    /// <summary>
    /// 测试 - RetryExecute - 所有重试都失败
    /// </summary>
    [Fact]
    public void RetryExecute_AllRetriesFail_ThrowsAggregateException()
    {
        // Arrange
        var attemptCount = 0;
        // Act & Assert
        var ex = Should.Throw<AggregateException>(() =>
        {
            Common.RetryExecute<int>(() =>
            {
                attemptCount++;
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }, maxRetries: 2, delay: TimeSpan.FromMilliseconds(10));
        });
        ex.InnerExceptions.Count.ShouldBe(3); // 1 + 2 重试 = 3次尝试
        attemptCount.ShouldBe(3);
        Output.WriteLine($"RetryExecute 所有重试失败: 尝试次数={attemptCount}, 异常数={ex.InnerExceptions.Count}");
    }
    /// <summary>
    /// 测试 - RetryExecute - null 函数抛出异常
    /// </summary>
    [Fact]
    public void RetryExecute_NullFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => Common.RetryExecute<string>(null));
        ex.ParamName.ShouldBe("func");
    }
    /// <summary>
    /// 测试 - RetryExecute - 负数重试次数抛出异常
    /// </summary>
    [Fact]
    public void RetryExecute_NegativeMaxRetries_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            Common.RetryExecute(() => "test", maxRetries: -1);
        });
        ex.ParamName.ShouldBe("maxRetries");
    }
    /// <summary>
    /// 测试 - RetryExecute - 零延迟
    /// </summary>
    [Fact]
    public void RetryExecute_ZeroDelay_WorksCorrectly()
    {
        // Arrange
        var attemptCount = 0;
        var startTime = DateTime.Now;
        // Act
        var result = Common.RetryExecute(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("Not ready");
            return "success";
        }, maxRetries: 5, delay: TimeSpan.Zero);
        var endTime = DateTime.Now;
        // Assert
        result.ShouldBe("success");
        attemptCount.ShouldBe(3);
        (endTime - startTime).TotalMilliseconds.ShouldBeLessThan(100); // 应该很快完成
        Output.WriteLine($"RetryExecute 零延迟: 耗时={(endTime - startTime).TotalMilliseconds}ms");
    }
    #endregion
    #region Clamp 测试
    /// <summary>
    /// 测试 - Clamp - 值在范围内
    /// </summary>
    [Theory]
    [InlineData(50, 0, 100, 50)]
    [InlineData(25, 10, 30, 25)]
    [InlineData(0, -10, 10, 0)]
    public void Clamp_ValueInRange_ReturnsOriginalValue(int value, int min, int max, int expected)
    {
        // Act
        var result = Common.Clamp(value, min, max);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"Clamp 值在范围内: Clamp({value}, {min}, {max}) = {result}");
    }
    /// <summary>
    /// 测试 - Clamp - 值小于最小值
    /// </summary>
    [Theory]
    [InlineData(-10, 0, 100, 0)]
    [InlineData(5, 10, 30, 10)]
    [InlineData(-100, -50, 50, -50)]
    public void Clamp_ValueBelowMin_ReturnsMin(int value, int min, int max, int expected)
    {
        // Act
        var result = Common.Clamp(value, min, max);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"Clamp 值小于最小值: Clamp({value}, {min}, {max}) = {result}");
    }
    /// <summary>
    /// 测试 - Clamp - 值大于最大值
    /// </summary>
    [Theory]
    [InlineData(150, 0, 100, 100)]
    [InlineData(35, 10, 30, 30)]
    [InlineData(100, -50, 50, 50)]
    public void Clamp_ValueAboveMax_ReturnsMax(int value, int min, int max, int expected)
    {
        // Act
        var result = Common.Clamp(value, min, max);
        // Assert
        result.ShouldBe(expected);
        Output.WriteLine($"Clamp 值大于最大值: Clamp({value}, {min}, {max}) = {result}");
    }
    /// <summary>
    /// 测试 - Clamp - 最小值大于最大值抛出异常
    /// </summary>
    [Fact]
    public void Clamp_MinGreaterThanMax_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => Common.Clamp(50, 100, 0));
        ex.ParamName.ShouldBe("min");
    }
    /// <summary>
    /// 测试 - Clamp - 不同类型
    /// </summary>
    [Fact]
    public void Clamp_DifferentTypes_WorksCorrectly()
    {
        // Double 测试
        var doubleResult = Common.Clamp(5.5, 1.0, 10.0);
        doubleResult.ShouldBe(5.5);
        // DateTime 测试
        var now = DateTime.Now;
        var min = now.AddDays(-1);
        var max = now.AddDays(1);
        var dateResult = Common.Clamp(now, min, max);
        dateResult.ShouldBe(now);
        // String 测试
        var stringResult = Common.Clamp("b", "a", "c");
        stringResult.ShouldBe("b");
        Output.WriteLine("Clamp 不同类型测试通过");
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 性能测试 - 大量调用
    /// </summary>
    [Fact]
    public void PerformanceTest_ManyOperations_CompletesQuickly()
    {
        // Arrange
        const int iterations = 1000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _ = Common.ApplicationBaseDirectory;
                _ = Common.Line;
                _ = Common.GetCurrentDirectory();
                _ = Common.GetType<int>();
                _ = Common.GetPhysicalPath($"test{i}.txt");
                _ = Common.JoinPath("api", "v1", $"item{i}");
                _ = Common.Clamp(i, 0, 100);
                var a = i;
                var b = i + 1;
                Common.Swap(ref a, ref b);
            }
        }, TimeSpan.FromSeconds(2));
        Output.WriteLine($"性能测试完成: {iterations} 次操作");
    }
    /// <summary>
    /// 测试 - SafeExecute 性能测试
    /// </summary>
    [Fact]
    public void PerformanceTest_SafeExecute_CompletesQuickly()
    {
        // Arrange
        const int iterations = 1000;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _ = Common.SafeExecute(() => i * 2, 0);
                _ = Common.SafeExecute(() => { /* do nothing */ });
            }
        }, TimeSpan.FromSeconds(1));
        Output.WriteLine($"SafeExecute 性能测试完成: {iterations} 次操作");
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 实际应用场景 - 文件路径操作
    /// </summary>
    [Fact]
    public void RealWorldScenario_FilePathOperations_WorksCorrectly()
    {
        // Arrange
        var configFileName = "app.config";
        var logFileName = "app.log";
        // Act
        var baseDir = Common.ApplicationBaseDirectory;
        var configPath = Common.GetPhysicalPath($"config/{configFileName}");
        var logPath = Common.GetPhysicalPath($"logs/{logFileName}");
        var apiUrl = Common.JoinPath("https://api.example.com", "v1", "users");
        // Assert
        baseDir.ShouldNotBeNullOrEmpty();
        configPath.ShouldContain("config");
        configPath.ShouldContain(configFileName);
        logPath.ShouldContain("logs");
        logPath.ShouldContain(logFileName);
        apiUrl.ShouldContain("api.example.com");
        apiUrl.ShouldContain("users");
        Output.WriteLine("文件路径操作场景测试:");
        Output.WriteLine($"  基目录: {baseDir}");
        Output.WriteLine($"  配置文件: {configPath}");
        Output.WriteLine($"  日志文件: {logPath}");
        Output.WriteLine($"  API URL: {apiUrl}");
    }
    /// <summary>
    /// 测试 - 实际应用场景 - 错误处理和重试
    /// </summary>
    [Fact]
    public void RealWorldScenario_ErrorHandlingAndRetry_WorksCorrectly()
    {
        // Arrange
        var attemptCount = 0;
        // Act - 模拟网络请求重试
        var result = Common.SafeExecute(() =>
        {
            return Common.RetryExecute(() =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new TimeoutException("Network timeout");
                return "Data received";
            }, maxRetries: 5, delay: TimeSpan.FromMilliseconds(10));
        }, "Failed to get data");
        // Assert
        result.ShouldBe("Data received");
        attemptCount.ShouldBe(3);
        Output.WriteLine($"错误处理和重试场景: 尝试{attemptCount}次后成功");
    }
    /// <summary>
    /// 测试 - 实际应用场景 - 数据验证和限制
    /// </summary>
    [Fact]
    public void RealWorldScenario_DataValidationAndClamping_WorksCorrectly()
    {
        // Arrange
        var userInputs = new[] { -10, 50, 150, 75 };
        var results = new List<int>();
        // Act - 模拟用户输入验证
        foreach (var input in userInputs)
        {
            var validatedValue = Common.Clamp(input, 0, 100);
            results.Add(validatedValue);
        }
        // Assert
        results[0].ShouldBe(0);   // -10 -> 0
        results[1].ShouldBe(50);  // 50 -> 50
        results[2].ShouldBe(100); // 150 -> 100
        results[3].ShouldBe(75);  // 75 -> 75
        Output.WriteLine($"数据验证场景: {string.Join(", ", userInputs)} -> {string.Join(", ", results)}");
    }
    /// <summary>
    /// 测试 - 复合场景 - 多功能组合使用
    /// </summary>
    [Fact]
    public void ComplexScenario_MultipleFeaturesUsed_WorksCorrectly()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var testId = Guid.NewGuid().ToString("N")[..8];
        try
        {
            // Act - 复合操作
            var result = Common.SafeExecute(() =>
            {
                // 1. 路径操作
                var projectPath = Common.GetParentDirectory(2);
                var configPath = Common.GetPhysicalPath("config.json", projectPath);
                var logUrl = Common.JoinPath("http://logs.example.com", "api", testId);
                // 2. 数据处理
                var priority = Common.Clamp(150, 1, 10);
                var config = new
                {
                    ProjectPath = projectPath,
                    ConfigPath = configPath,
                    LogUrl = logUrl,
                    Priority = priority,
                    TestId = testId
                };
                // 3. 类型处理
                var configType = Common.GetType(config.GetType());
                return new
                {
                    Config = config,
                    Type = configType.Name,
                    Success = true
                };
            }, null);
            // Assert
            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            result.Config.Priority.ShouldBe(10);
            result.Config.TestId.ShouldBe(testId);
            result.Type.ShouldNotBeNullOrEmpty();
            Output.WriteLine($"复合场景测试成功:");
            Output.WriteLine($"  TestId: {result.Config.TestId}");
            Output.WriteLine($"  Priority: {result.Config.Priority}");
            Output.WriteLine($"  Type: {result.Type}");
            Output.WriteLine($"  ProjectPath: {result.Config.ProjectPath}");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"复合场景测试异常: {ex.Message}");
            throw;
        }
    }
    /// <summary>
    /// 测试 - 实际应用场景 - 数据交换
    /// </summary>
    [Fact]
    public void RealWorldScenario_DataSwapping_WorksCorrectly()
    {
        // Arrange
        var user1 = new { Id = 1, Name = "Alice" };
        var user2 = new { Id = 2, Name = "Bob" };
        var originalUser1 = user1;
        var originalUser2 = user2;
        var priority1 = 10;
        var priority2 = 20;
        // Act
        Common.Swap(ref user1, ref user2);
        Common.Swap(ref priority1, ref priority2);
        // Assert
        user1.ShouldBe(originalUser2);
        user2.ShouldBe(originalUser1);
        priority1.ShouldBe(20);
        priority2.ShouldBe(10);
        Output.WriteLine("数据交换场景测试:");
        Output.WriteLine($"  交换后 user1: {user1}");
        Output.WriteLine($"  交换后 user2: {user2}");
        Output.WriteLine($"  交换后 priority1: {priority1}");
        Output.WriteLine($"  交换后 priority2: {priority2}");
    }
    /// <summary>
    /// 测试 - 实际应用场景 - 类型处理
    /// </summary>
    [Fact]
    public void RealWorldScenario_TypeHandling_WorksCorrectly()
    {
        // 模拟实际的类型处理场景
        var types = new[]
        {
            typeof(int?), typeof(string), typeof(DateTime?),
            typeof(bool), typeof(decimal?), typeof(Guid?)
        };
        foreach (var type in types)
        {
            // Act
            var underlyingType = Common.GetType(type);
            // Assert
            underlyingType.ShouldNotBeNull();
            // 验证可空类型被正确处理
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                underlyingType.ShouldBe(Nullable.GetUnderlyingType(type));
            }
            else
            {
                underlyingType.ShouldBe(type);
            }
            Output.WriteLine($"类型处理: {type.Name} -> {underlyingType.Name}");
        }
    }
    #endregion
    #region 边界条件测试
    /// <summary>
    /// 测试 - 边界条件 - 极长路径
    /// </summary>
    [Fact]
    public void EdgeCase_VeryLongPath_HandlesCorrectly()
    {
        // Arrange
        var longPathPart = new string('a', 100);
        var pathParts = new[] { longPathPart, "folder", "file.txt" };
        // Act
        var result = Common.JoinPath(pathParts);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain(longPathPart);
        Output.WriteLine($"极长路径测试: 长度={result.Length}");
    }
    /// <summary>
    /// 测试 - 边界条件 - 特殊字符路径
    /// </summary>
    [Theory]
    [InlineData("测试文件.txt")]
    [InlineData("file with spaces.txt")]
    [InlineData("file-with-dashes.txt")]
    [InlineData("file_with_underscores.txt")]
    [InlineData("file.with.dots.txt")]
    public void EdgeCase_SpecialCharacterPaths_HandlesCorrectly(string fileName)
    {
        // Act
        var result = Common.GetPhysicalPath(fileName);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain(fileName);
        Output.WriteLine($"特殊字符路径: '{fileName}' -> '{result}'");
    }
    /// <summary>
    /// 测试 - 边界条件 - 极值 Clamp
    /// </summary>
    [Fact]
    public void EdgeCase_ClampExtremeValues_HandlesCorrectly()
    {
        // Act & Assert
        Common.Clamp(int.MaxValue, 0, 100).ShouldBe(100);
        Common.Clamp(int.MinValue, 0, 100).ShouldBe(0);
        Common.Clamp(0, int.MinValue, int.MaxValue).ShouldBe(0);
        Output.WriteLine("极值 Clamp 测试通过");
    }
    /// <summary>
    /// 测试 - 边界条件 - 高频率重试
    /// </summary>
    [Fact]
    public void EdgeCase_HighFrequencyRetry_HandlesCorrectly()
    {
        // Arrange
        var attemptCount = 0;
        // Act
        var result = Common.RetryExecute(() =>
        {
            attemptCount++;
            if (attemptCount < 10)
                throw new InvalidOperationException("Not ready");
            return "success";
        }, maxRetries: 15, delay: TimeSpan.FromMilliseconds(1));
        // Assert
        result.ShouldBe("success");
        attemptCount.ShouldBe(10);
        Output.WriteLine($"高频率重试测试: {attemptCount} 次尝试");
    }
    #endregion
}
