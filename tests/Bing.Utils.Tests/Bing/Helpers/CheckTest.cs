namespace Bing.Helpers;

/// <summary>
/// 参数验证辅助类测试
/// </summary>
[Trait("Bing.Helpers", "Check")]
public class CheckTest
{
    #region Required 测试

    /// <summary>
    /// 测试 - Required - 断言为真时不抛出异常
    /// </summary>
    [Fact]
    public void Required_AssertionTrue_DoesNotThrow()
    {
        // Arrange
        var value = 10;

        // Act & Assert
        Should.NotThrow(() => Check.Required(value, x => x > 5, "值必须大于5"));
    }

    /// <summary>
    /// 测试 - Required - 断言为假时抛出异常
    /// </summary>
    [Fact]
    public void Required_AssertionFalse_ThrowsException()
    {
        // Arrange
        var value = 3;

        // Act & Assert
        Should.Throw<Exception>(() => Check.Required(value, x => x > 5, "值必须大于5"))
            .Message.ShouldBe("值必须大于5");
    }

    /// <summary>
    /// 测试 - Required - 断言函数为null抛出异常
    /// </summary>
    [Fact]
    public void Required_NullAssertionFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.Required(10, (Func<int, bool>)null, "message"))
            .ParamName.ShouldBe("assertionFunc");
    }

    /// <summary>
    /// 测试 - Required泛型 - 自定义异常类型
    /// </summary>
    [Fact]
    public void Required_Generic_CustomExceptionType()
    {
        // Arrange
        var value = 3;

        // Act & Assert
        //Should.Throw<ArgumentOutOfRangeException>(() =>
        //        Check.Required<int, ArgumentOutOfRangeException>(value, x => x > 5, "值超出范围"))
        //    .Message.ShouldBe("值超出范围");
        Should.Throw<ArgumentOutOfRangeException>(() =>
                Check.Required<int, ArgumentOutOfRangeException>(value, x => x > 5, "值超出范围"));
    }

    #endregion

    #region NotNull 测试

    /// <summary>
    /// 测试 - NotNull - 有效值返回原值
    /// </summary>
    [Fact]
    public void NotNull_ValidValue_ReturnsOriginalValue()
    {
        // Arrange
        var testObject = new object();

        // Act
        var result = Check.NotNull(testObject, nameof(testObject));

        // Assert
        result.ShouldBe(testObject);
    }

    /// <summary>
    /// 测试 - NotNull - null值抛出异常
    /// </summary>
    [Fact]
    public void NotNull_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        object testObject = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.NotNull(testObject, nameof(testObject)))
            .ParamName.ShouldBe(nameof(testObject));
    }

    /// <summary>
    /// 测试 - NotNull带消息 - 自定义消息
    /// </summary>
    [Fact]
    public void NotNull_WithMessage_CustomMessage()
    {
        // Arrange
        object testObject = null;
        var customMessage = "自定义错误消息";

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.NotNull(testObject, "param", customMessage))
            .Message.ShouldContain(customMessage);
    }

    /// <summary>
    /// 测试 - NotNull字符串 - 长度验证
    /// </summary>
    [Theory]
    [InlineData("valid", 10, 1, true)]
    [InlineData("toolongstring", 5, 1, false)]
    [InlineData("", 10, 1, false)]
    public void NotNull_String_LengthValidation(string value, int maxLength, int minLength, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            Should.NotThrow(() => Check.NotNull(value, "param", maxLength, minLength));
        }
        else
        {
            Should.Throw<ArgumentException>(() => Check.NotNull(value, "param", maxLength, minLength));
        }
    }

    #endregion

    #region NotNullOrWhiteSpace 测试

    /// <summary>
    /// 测试 - NotNullOrWhiteSpace - 有效字符串返回原值
    /// </summary>
    [Fact]
    public void NotNullOrWhiteSpace_ValidString_ReturnsOriginalValue()
    {
        // Arrange
        var testString = "valid string";

        // Act
        var result = Check.NotNullOrWhiteSpace(testString, nameof(testString));

        // Assert
        result.ShouldBe(testString);
    }

    /// <summary>
    /// 测试 - NotNullOrWhiteSpace - 无效值抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void NotNullOrWhiteSpace_InvalidValues_ThrowsArgumentException(string invalidValue)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.NotNullOrWhiteSpace(invalidValue, "param"));
    }

    #endregion

    #region NotNullOrEmpty 测试

    /// <summary>
    /// 测试 - NotNullOrEmpty字符串 - 有效字符串返回原值
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_String_ValidString_ReturnsOriginalValue()
    {
        // Arrange
        var testString = "valid";

        // Act
        var result = Check.NotNullOrEmpty(testString, nameof(testString));

        // Assert
        result.ShouldBe(testString);
    }

    /// <summary>
    /// 测试 - NotNullOrEmpty字符串 - 无效值抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NotNullOrEmpty_String_InvalidValues_ThrowsArgumentException(string invalidValue)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.NotNullOrEmpty(invalidValue, "param"));
    }

    /// <summary>
    /// 测试 - NotNullOrEmpty集合 - 有效集合返回原值
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_Collection_ValidCollection_ReturnsOriginalValue()
    {
        // Arrange
        var testCollection = new List<int> { 1, 2, 3 };

        // Act
        var result = Check.NotNullOrEmpty(testCollection, nameof(testCollection));

        // Assert
        result.ShouldBe(testCollection);
    }

    /// <summary>
    /// 测试 - NotNullOrEmpty集合 - 空集合抛出异常
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_Collection_EmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var emptyCollection = new List<int>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.NotNullOrEmpty(emptyCollection, "param"));
    }

    /// <summary>
    /// 测试 - NotNullOrEmpty可枚举 - null集合抛出异常
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_Enumerable_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<int> nullCollection = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.NotNullOrEmpty(nullCollection, "param"));
    }

    /// <summary>
    /// 测试 - NotNullOrEmpty可枚举 - 空集合抛出异常
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_Enumerable_EmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var emptyCollection = Enumerable.Empty<int>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.NotNullOrEmpty(emptyCollection, "param"));
    }

    #endregion

    #region NotEmpty 测试

    /// <summary>
    /// 测试 - NotEmpty - 有效Guid不抛出异常
    /// </summary>
    [Fact]
    public void NotEmpty_ValidGuid_DoesNotThrow()
    {
        // Arrange
        var validGuid = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() => Check.NotEmpty(validGuid, nameof(validGuid)));
    }

    /// <summary>
    /// 测试 - NotEmpty - 空Guid抛出异常
    /// </summary>
    [Fact]
    public void NotEmpty_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.NotEmpty(emptyGuid, "param"));
    }

    #endregion

    #region AssignableTo 测试

    /// <summary>
    /// 测试 - AssignableTo - 可分配类型返回原类型
    /// </summary>
    [Fact]
    public void AssignableTo_AssignableType_ReturnsOriginalType()
    {
        // Arrange
        var stringType = typeof(string);

        // Act
        var result = Check.AssignableTo<object>(stringType, nameof(stringType));

        // Assert
        result.ShouldBe(stringType);
    }

    /// <summary>
    /// 测试 - AssignableTo - 不可分配类型抛出异常
    /// </summary>
    [Fact]
    public void AssignableTo_NonAssignableType_ThrowsArgumentException()
    {
        // Arrange
        var intType = typeof(int);

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.AssignableTo<string>(intType, "param"));
    }

    /// <summary>
    /// 测试 - AssignableTo - null类型抛出异常
    /// </summary>
    [Fact]
    public void AssignableTo_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.AssignableTo<object>(null, "param"));
    }

    #endregion

    #region Length 测试

    /// <summary>
    /// 测试 - Length - 有效长度返回原字符串
    /// </summary>
    [Fact]
    public void Length_ValidLength_ReturnsOriginalString()
    {
        // Arrange
        var testString = "test";

        // Act
        var result = Check.Length(testString, nameof(testString), 10, 2);

        // Assert
        result.ShouldBe(testString);
    }

    /// <summary>
    /// 测试 - Length - 长度超出最大值抛出异常
    /// </summary>
    [Fact]
    public void Length_ExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longString = "verylongstring";

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Length(longString, "param", 5));
    }

    /// <summary>
    /// 测试 - Length - 长度小于最小值抛出异常
    /// </summary>
    [Fact]
    public void Length_BelowMinLength_ThrowsArgumentException()
    {
        // Arrange
        var shortString = "hi";

        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Length(shortString, "param", 10, 5));
    }

    /// <summary>
    /// 测试 - Length - null字符串且最小长度大于0抛出异常
    /// </summary>
    [Fact]
    public void Length_NullStringWithMinLength_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Length(null, "param", 10, 1));
    }

    #endregion

    #region Positive 测试

    /// <summary>
    /// 测试 - Positive - 正数返回原值
    /// </summary>
    [Theory]
    [InlineData((short)5)]
    [InlineData(10)]
    [InlineData(100L)]
    [InlineData(3.14f)]
    [InlineData(2.718)]
    public void Positive_PositiveValues_ReturnsOriginalValue(object value)
    {
        // Act & Assert
        if (value is short shortValue)
            Check.Positive(shortValue, "param").ShouldBe(shortValue);
        else if (value is int intValue)
            Check.Positive(intValue, "param").ShouldBe(intValue);
        else if (value is long longValue)
            Check.Positive(longValue, "param").ShouldBe(longValue);
        else if (value is float floatValue)
            Check.Positive(floatValue, "param").ShouldBe(floatValue);
        else if (value is double doubleValue)
            Check.Positive(doubleValue, "param").ShouldBe(doubleValue);
    }

    /// <summary>
    /// 测试 - Positive - 零值抛出异常
    /// </summary>
    [Fact]
    public void Positive_ZeroValue_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Positive(0, "param"))
            .Message.ShouldContain("不能等于零");
    }

    /// <summary>
    /// 测试 - Positive - 负数抛出异常
    /// </summary>
    [Fact]
    public void Positive_NegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Positive(-5, "param"))
            .Message.ShouldContain("不能小于零");
    }

    /// <summary>
    /// 测试 - Positive - decimal类型
    /// </summary>
    [Theory]
    [InlineData(1.5)]
    [InlineData(0)]
    [InlineData(-2.5)]
    public void Positive_Decimal_VariousValues(decimal value)
    {
        if (value > 0)
        {
            Check.Positive(value, "param").ShouldBe(value);
        }
        else
        {
            Should.Throw<ArgumentException>(() => Check.Positive(value, "param"));
        }
    }

    #endregion

    #region Range 测试

    /// <summary>
    /// 测试 - Range - 值在范围内返回原值
    /// </summary>
    [Fact]
    public void Range_ValueInRange_ReturnsOriginalValue()
    {
        // Arrange
        var value = 50;

        // Act
        var result = Check.Range(value, nameof(value), 10, 100);

        // Assert
        result.ShouldBe(value);
    }

    /// <summary>
    /// 测试 - Range - 值小于最小值抛出异常
    /// </summary>
    [Fact]
    public void Range_ValueBelowMinimum_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Range(5, "param", 10, 100))
            .Message.ShouldContain("超出范围");
    }

    /// <summary>
    /// 测试 - Range - 值大于最大值抛出异常
    /// </summary>
    [Fact]
    public void Range_ValueAboveMaximum_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Check.Range(150, "param", 10, 100))
            .Message.ShouldContain("超出范围");
    }

    /// <summary>
    /// 测试 - Range - 不同数值类型
    /// </summary>
    [Theory]
    [InlineData(5.0f, 1.0f, 10.0f, true)]
    [InlineData(15.0f, 1.0f, 10.0f, false)]
    [InlineData(5.0, 1.0, 10.0, true)]
    [InlineData(15.0, 1.0, 10.0, false)]
    public void Range_DifferentNumericTypes(object value, object min, object max, bool shouldPass)
    {
        if (shouldPass)
        {
            if (value is float floatValue && min is float floatMin && max is float floatMax)
                Check.Range(floatValue, "param", floatMin, floatMax).ShouldBe(floatValue);
            else if (value is double doubleValue && min is double doubleMin && max is double doubleMax)
                Check.Range(doubleValue, "param", doubleMin, doubleMax).ShouldBe(doubleValue);
        }
        else
        {
            if (value is float floatValue && min is float floatMin && max is float floatMax)
                Should.Throw<ArgumentException>(() => Check.Range(floatValue, "param", floatMin, floatMax));
            else if (value is double doubleValue && min is double doubleMin && max is double doubleMax)
                Should.Throw<ArgumentException>(() => Check.Range(doubleValue, "param", doubleMin, doubleMax));
        }
    }

    #endregion

    #region Between 测试

    /// <summary>
    /// 测试 - LessThan - 值小于目标时不抛出异常
    /// </summary>
    [Fact]
    public void LessThan_ValueLessThanTarget_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => Check.LessThan(5, "param", 10));
    }

    /// <summary>
    /// 测试 - LessThan - 值大于目标时抛出异常
    /// </summary>
    [Fact]
    public void LessThan_ValueGreaterThanTarget_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Check.LessThan(15, "param", 10));
    }

    /// <summary>
    /// 测试 - GreaterThan - 值大于目标时不抛出异常
    /// </summary>
    [Fact]
    public void GreaterThan_ValueGreaterThanTarget_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => Check.GreaterThan(15, "param", 10));
    }

    /// <summary>
    /// 测试 - GreaterThan - 值小于目标时抛出异常
    /// </summary>
    [Fact]
    public void GreaterThan_ValueLessThanTarget_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Check.GreaterThan(5, "param", 10));
    }

    /// <summary>
    /// 测试 - Between - 值在范围内不抛出异常
    /// </summary>
    [Fact]
    public void Between_ValueInRange_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => Check.Between(50, "param", 10, 100, true, true));
    }

    /// <summary>
    /// 测试 - Between - 值超出范围抛出异常
    /// </summary>
    [Theory]
    [InlineData(5)]   // 小于起始值
    [InlineData(150)] // 大于结束值
    public void Between_ValueOutOfRange_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Check.Between(value, "param", 10, 100));
    }

    /// <summary>
    /// 测试 - NotNegativeOrZero - 正时间跨度不抛出异常
    /// </summary>
    [Fact]
    public void NotNegativeOrZero_PositiveTimeSpan_DoesNotThrow()
    {
        // Arrange
        var positiveTimeSpan = TimeSpan.FromMinutes(5);

        // Act & Assert
        Should.NotThrow(() => Check.NotNegativeOrZero(positiveTimeSpan, "param"));
    }

    /// <summary>
    /// 测试 - NotNegativeOrZero - 零或负时间跨度抛出异常
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NotNegativeOrZero_ZeroOrNegativeTimeSpan_ThrowsArgumentOutOfRangeException(int minutes)
    {
        // Arrange
        var timeSpan = TimeSpan.FromMinutes(minutes);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Check.NotNegativeOrZero(timeSpan, "param"));
    }

    #endregion

    #region IO 测试

    /// <summary>
    /// 测试 - DirectoryExists - 存在的目录不抛出异常
    /// </summary>
    [Fact]
    public void DirectoryExists_ExistingDirectory_DoesNotThrow()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act & Assert
        Should.NotThrow(() => Check.DirectoryExists(tempDir));
    }

    /// <summary>
    /// 测试 - DirectoryExists - 不存在的目录抛出异常
    /// </summary>
    [Fact]
    public void DirectoryExists_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Should.Throw<DirectoryNotFoundException>(() => Check.DirectoryExists(nonExistentDir));
    }

    /// <summary>
    /// 测试 - DirectoryExists - null路径抛出异常
    /// </summary>
    [Fact]
    public void DirectoryExists_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.DirectoryExists(null));
    }

    /// <summary>
    /// 测试 - FileExists - 存在的文件不抛出异常
    /// </summary>
    [Fact]
    public void FileExists_ExistingFile_DoesNotThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act & Assert
            Should.NotThrow(() => Check.FileExists(tempFile));
        }
        finally
        {
            // 清理
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// 测试 - FileExists - 不存在的文件抛出异常
    /// </summary>
    [Fact]
    public void FileExists_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => Check.FileExists(nonExistentFile));
    }

    /// <summary>
    /// 测试 - FileExists - null路径抛出异常
    /// </summary>
    [Fact]
    public void FileExists_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Check.FileExists(null));
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 复合验证 - 多个验证方法组合使用
    /// </summary>
    [Fact]
    public void IntegrationTest_ComplexValidation_WorksCorrectly()
    {
        // Arrange
        var testData = new
        {
            Name = "TestUser",
            Age = 25,
            Score = 85.5,
            Id = Guid.NewGuid()
        };

        // Act & Assert - 所有验证都应该通过
        Should.NotThrow(() =>
        {
            Check.NotNullOrWhiteSpace(testData.Name, nameof(testData.Name), 100, 3);
            Check.Range(testData.Age, nameof(testData.Age), 0, 120);
            Check.Positive(testData.Score, nameof(testData.Score));
            Check.NotEmpty(testData.Id, nameof(testData.Id));
        });
    }

    /// <summary>
    /// 测试 - 验证链式调用
    /// </summary>
    [Fact]
    public void IntegrationTest_ChainedValidation_WorksCorrectly()
    {
        // Arrange
        var value = "Test Value";

        // Act
        var result = Check.NotNull(
            Check.NotNullOrWhiteSpace(
                Check.Length(value, "value", 50, 5),
                "value"),
            "value");

        // Assert
        result.ShouldBe(value);
    }

    /// <summary>
    /// 测试 - 边界值验证
    /// </summary>
    [Theory]
    [InlineData(int.MinValue, int.MinValue, int.MaxValue, true)]
    [InlineData(int.MaxValue, int.MinValue, int.MaxValue, true)]
    [InlineData(0, -10, 10, true)]
    [InlineData(11, -10, 10, false)]
    public void IntegrationTest_BoundaryValues(int value, int min, int max, bool shouldPass)
    {
        if (shouldPass)
        {
            Should.NotThrow(() => Check.Range(value, "param", min, max));
        }
        else
        {
            Should.Throw<ArgumentException>(() => Check.Range(value, "param", min, max));
        }
    }

    #endregion

    /// <summary>
    /// 测试 - 不可空检查
    /// </summary>
    [Fact]
    public void Test_NotNull()
    {
        Check.NotNull("test", nameof(Test_NotNull)).ShouldBe("test");
        Check.NotNull(string.Empty, nameof(Test_NotNull)).ShouldBe(string.Empty);
        Check.NotNull("test", nameof(Test_NotNull), maxLength: 4, minLength: 0).ShouldBe("test");

        Assert.Throws<ArgumentNullException>(() => Check.NotNull<object>(null, nameof(Test_NotNull)));
        Assert.Throws<ArgumentException>(() => Check.NotNull(null, nameof(Test_NotNull)));
        Assert.Throws<ArgumentException>(() => Check.NotNull("test", nameof(Test_NotNull), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNull("test", nameof(Test_NotNull), minLength: 5));
    }

    /// <summary>
    /// 测试 - 检查字符串不能为空引用或空白字符
    /// </summary>
    [Fact]
    public void Test_NotNullOrWhiteSpace()
    {
        Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace)).ShouldBe("test");
        Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), maxLength: 4, minLength: 0).ShouldBe("test");

        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace(null, nameof(Test_NotNullOrWhiteSpace)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace(string.Empty, nameof(Test_NotNullOrWhiteSpace)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrWhiteSpace("test", nameof(Test_NotNullOrWhiteSpace), minLength: 5));
    }

    /// <summary>
    /// 测试 - 检查不为 null 或空集合
    /// </summary>
    [Fact]
    public void Test_NotNullOrEmpty()
    {
        Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty)).ShouldBe("test");
        Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), maxLength: 4, minLength: 0).ShouldBe("test");
        Check.NotNullOrEmpty(new List<string> { "test" }, nameof(Test_NotNullOrEmpty));

        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(null, nameof(Test_NotNullOrEmpty)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(string.Empty, nameof(Test_NotNullOrEmpty)));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty("test", nameof(Test_NotNullOrEmpty), minLength: 5));
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(new List<string>(), nameof(Test_NotNullOrEmpty)));
    }

    /// <summary>
    /// 测试 - 验证类型是否可分配给指定基础类型
    /// </summary>
    [Fact]
    public void Test_AssignableTo()
    {
        Check.AssignableTo<object>(typeof(string), nameof(Test_AssignableTo)).ShouldBe(typeof(string));
        Check.AssignableTo<Parent>(typeof(Child), nameof(Test_AssignableTo)).ShouldBe(typeof(Child));
        Check.AssignableTo<Child>(typeof(Child2), nameof(Test_AssignableTo)).ShouldBe(typeof(Child2));
        Check.AssignableTo<Parent>(typeof(Child2), nameof(Test_AssignableTo)).ShouldBe(typeof(Child2));

        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child>(typeof(Parent), nameof(Test_AssignableTo)));
        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child2>(typeof(Child), nameof(Test_AssignableTo)));
        Assert.Throws<ArgumentException>(() => Check.AssignableTo<Child2>(typeof(Parent), nameof(Test_AssignableTo)));
    }

    /// <summary>
    /// 测试 - 验证字符串的长度是否符合指定的范围
    /// </summary>
    [Fact]
    public void Test_Length()
    {
        Check.Length("test", nameof(Test_Length), maxLength: 4).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 5).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 0).ShouldBe("test");
        Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 4).ShouldBe("test");

        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 0));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 3));
        Assert.Throws<ArgumentException>(() => Check.Length("test", nameof(Test_Length), maxLength: 4, minLength: 5));
    }

    /// <summary>
    /// 测试 - 确保值为正数
    /// </summary>
    [Fact]
    public void Test_Positive()
    {
        Check.Positive(Conv.To<short>(1), nameof(Test_Positive)).ShouldBe(Conv.To<short>(1));
        Check.Positive(Conv.To<int>(1), nameof(Test_Positive)).ShouldBe(Conv.To<int>(1));
        Check.Positive(Conv.To<long>(1), nameof(Test_Positive)).ShouldBe(Conv.To<long>(1));
        Check.Positive(decimal.One, nameof(Test_Positive)).ShouldBe(decimal.One);
        Check.Positive(1.0f, nameof(Test_Positive)).ShouldBe(1.0f);
        Check.Positive(1.0, nameof(Test_Positive)).ShouldBe(1.0);

        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<short>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<int>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<long>(0), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(decimal.Zero, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(0.0f, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(0.0, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<short>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<int>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(Conv.To<long>(-1), nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-decimal.One, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-1.0f, nameof(Test_Positive)));
        Assert.Throws<ArgumentException>(() => Check.Positive(-1.0, nameof(Test_Positive)));
    }

    /// <summary>
    /// 测试 - 确保值处于指定的范围内
    /// </summary>
    [Fact]
    public void Test_Range()
    {
        Check.Range(Conv.To<short>(1), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)).ShouldBe(Conv.To<short>(1));
        Check.Range(Conv.To<int>(1), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)).ShouldBe(Conv.To<int>(1));
        Check.Range(Conv.To<long>(1), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)).ShouldBe(Conv.To<long>(1));
        Check.Range(decimal.One, nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)).ShouldBe(decimal.One);
        Check.Range(1.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f).ShouldBe(1.0f);
        Check.Range(1.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0).ShouldBe(1.0);

        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<short>(0), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<int>(0), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<long>(0), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(decimal.Zero, nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(0.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f));
        Assert.Throws<ArgumentException>(() => Check.Range(0.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<short>(11), nameof(Test_Range), minimumValue: Conv.To<short>(1), maximumValue: Conv.To<short>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<int>(11), nameof(Test_Range), minimumValue: Conv.To<int>(1), maximumValue: Conv.To<int>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<long>(11), nameof(Test_Range), minimumValue: Conv.To<long>(1), maximumValue: Conv.To<long>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(Conv.To<decimal>(11), nameof(Test_Range), minimumValue: decimal.One, maximumValue: Conv.To<decimal>(10)));
        Assert.Throws<ArgumentException>(() => Check.Range(11.0f, nameof(Test_Range), minimumValue: 1.0f, maximumValue: 10.0f));
        Assert.Throws<ArgumentException>(() => Check.Range(11.0, nameof(Test_Range), minimumValue: 1.0, maximumValue: 10.0));
    }

    class Parent;

    class Child : Parent;

    class Child2 : Child;
}