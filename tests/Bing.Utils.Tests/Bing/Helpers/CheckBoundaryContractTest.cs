namespace Bing.Helpers;

/// <summary>
/// 测试类：`Check` 边界契约测试
/// </summary>
[Trait("Bing.Helpers", "Check.Boundary")]
public class CheckBoundaryContractTest
{
    /// <summary>
    /// 测试用例：`Required` 在断言失败且 `message=null` 时，应抛出 `ArgumentNullException(message)`
    /// </summary>
    [Fact]
    public void Required_AssertionFalseAndMessageNull_ThrowsArgumentNullExceptionWithMessageParam()
    {
        var exception = Should.Throw<ArgumentNullException>(() => Check.Required(1, x => x > 10, null));
        exception.ParamName.ShouldBe("message");
    }

    /// <summary>
    /// 测试用例：路径校验方法在 `path=null` 且传入自定义参数名时，应保留参数名
    /// </summary>
    [Theory]
    [InlineData("DirectoryExists", "directoryPath")]
    [InlineData("FileExists", "filePath")]
    public void PathValidation_NullPathWithCustomParameterName_ThrowsArgumentNullExceptionWithCustomParam(
        string methodName,
        string paramName)
    {
        var exception = Should.Throw<ArgumentNullException>(() => InvokePathValidation(methodName, paramName));

        exception.ParamName.ShouldBe(paramName);
    }

    /// <summary>
    /// 测试用例：`NotNullOrEmpty(ICollection)` 不应修改输入集合内容或顺序
    /// </summary>
    [Fact]
    public void NotNullOrEmpty_ICollection_DoesNotMutateInputCollection()
    {
        var values = new List<int> { 1, 2, 3 };
        var snapshot = values.ToArray();

        var result = Check.NotNullOrEmpty(values, nameof(values));

        result.ShouldBeSameAs(values);
        values.ShouldBe(snapshot);
    }

    /// <summary>
    /// 测试用例：`Length` 在长度小于最小值时，应抛出 `ArgumentException` 且参数名正确
    /// </summary>
    [Fact]
    public void Length_ValueBelowMinLength_ThrowsArgumentExceptionWithParameterName()
    {
        var exception = Should.Throw<ArgumentException>(() => Check.Length("abc", "text", maxLength: 10, minLength: 4));

        exception.ParamName.ShouldBe("text");
    }

    /// <summary>
    /// 测试用例：范围校验失败时，当前实现会把完整文案写入 `ParamName`（回归/契约记录）
    /// </summary>
    [Theory]
    [MemberData(nameof(GetRangeValidationFailureCases))]
    public void RangeValidation_WhenValidationFails_CurrentlyWritesMessageIntoParamName(
        string caseName,
        Action act)
    {
        caseName.ShouldNotBeNullOrWhiteSpace();
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);

        exception.ParamName.ShouldNotBeNullOrEmpty();
        exception.ParamName.ShouldContain("value");
    }

    /// <summary>
    /// 测试用例：`NotEmpty(Guid)` 失败时，当前异常不携带 `ParamName`，但消息中包含参数名
    /// </summary>
    [Fact]
    public void NotEmpty_EmptyGuid_CurrentlyThrowsWithoutParamName()
    {
        var exception = Should.Throw<ArgumentException>(() => Check.NotEmpty(Guid.Empty, "id"));

        exception.ParamName.ShouldBeNull();
        exception.Message.ShouldContain("id");
    }

    /// <summary>
    /// 测试用例：`NotNullOrWhiteSpace` 对 `null/empty/whitespace` 输入应抛出参数异常
    /// </summary>
    [Theory]
    [MemberData(nameof(GetInvalidStringInputs))]
    public void NotNullOrWhiteSpace_InvalidBoundaryInputs_ThrowsArgumentException(string value)
    {
        var exception = Should.Throw<ArgumentException>(() => Check.NotNullOrWhiteSpace(value, "text"));

        exception.ParamName.ShouldBe("text");
    }

    /// <summary>
    /// 测试用例：`NotNullOrWhiteSpace` 对特殊字符和超长字符串（未限长）应返回原值
    /// </summary>
    [Theory]
    [MemberData(nameof(GetValidStringInputs))]
    public void NotNullOrWhiteSpace_ValidBoundaryInputs_ReturnsOriginalValue(string value)
    {
        var result = Check.NotNullOrWhiteSpace(value, "text");

        result.ShouldBe(value);
    }

    /// <summary>
    /// 测试用例：`NotNullOrWhiteSpace` 超过 `maxLength` 时应抛出异常，且消息包含长度关键字
    /// </summary>
    [Theory]
    [MemberData(nameof(GetOverMaxLengthCases))]
    public void NotNullOrWhiteSpace_OverMaxLength_ThrowsArgumentExceptionWithKeyword(int actualLength, int maxLength)
    {
        var value = new string('a', actualLength);

        var exception = Should.Throw<ArgumentException>(() => Check.NotNullOrWhiteSpace(value, "text", maxLength: maxLength));

        exception.ParamName.ShouldBe("text");
        exception.Message.ShouldContain("长");
    }

    /// <summary>
    /// 测试数据：范围校验失败场景（用于记录当前 `ParamName` 契约）
    /// </summary>
    public static IEnumerable<object[]> GetRangeValidationFailureCases()
    {
        yield return new object[]
        {
            "LessThan",
            (Action)(() => Check.LessThan(10, "value", 5))
        };

        yield return new object[]
        {
            "GreaterThan",
            (Action)(() => Check.GreaterThan(1, "value", 5))
        };

        yield return new object[]
        {
            "Between",
            (Action)(() => Check.Between(10, "value", start: 1, end: 5, startEqual: true, endEqual: true))
        };
    }

    /// <summary>
    /// 测试数据：非法字符串边界输入
    /// </summary>
    public static IEnumerable<object[]> GetInvalidStringInputs()
    {
        yield return new object[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { " " };
        yield return new object[] { "\t" };
        yield return new object[] { "\r\n" };
    }

    /// <summary>
    /// 测试数据：合法字符串边界输入
    /// </summary>
    public static IEnumerable<object[]> GetValidStringInputs()
    {
        yield return new object[] { "!@#$%^&*()_+-=[]{}|;':,./<>?" };
        yield return new object[] { "  a  " };
        yield return new object[] { new string('x', 4096) };
        yield return new object[] { $"{new string('a', 1024)}-END" };
    }

    /// <summary>
    /// 测试数据：超过最大长度的边界样例
    /// </summary>
    public static IEnumerable<object[]> GetOverMaxLengthCases()
    {
        yield return new object[] { 1025, 1024 };
        yield return new object[] { 2048, 1024 };
    }

    private static void InvokePathValidation(string methodName, string paramName)
    {
        switch (methodName)
        {
            case "DirectoryExists":
                Check.DirectoryExists(null, paramName);
                return;
            case "FileExists":
                Check.FileExists(null, paramName);
                return;
            default:
                throw new NotSupportedException($"Unknown method: {methodName}");
        }
    }
}
