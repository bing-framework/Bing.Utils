using Microsoft.AspNetCore.Http;
namespace Bing.Helpers;
/// <summary>
/// CookieHelper 测试
/// </summary>
[Trait("Bing.Helpers", "Cookie")]
public class CookieHelperTest: TestBase
{
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly Mock<IRequestCookieCollection> _mockRequestCookies;
    private readonly Mock<IResponseCookies> _mockResponseCookies;
    /// <inheritdoc />
    public CookieHelperTest(ITestOutputHelper output) : base(output)
    {
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockResponse = new Mock<HttpResponse>();
        _mockRequestCookies = new Mock<IRequestCookieCollection>();
        _mockResponseCookies = new Mock<IResponseCookies>();
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Response).Returns(_mockResponse.Object);
        _mockRequest.Setup(x => x.Cookies).Returns(_mockRequestCookies.Object);
        _mockResponse.Setup(x => x.Cookies).Returns(_mockResponseCookies.Object);
    }
    #region GetCookie 测试
    /// 测试 - GetCookie - 正常获取存在的Cookie值
    [Fact]
    public void GetCookie_ShouldReturnCookieValue_WhenCookieExists()
    {
        // Arrange
        const string cookieName = "testCookie";
        const string cookieValue = "testValue";
        _mockRequestCookies.Setup(x => x[cookieName]).Returns(cookieValue);
        // Act
        var result = CookieHelper.GetCookie(_mockHttpContext.Object, cookieName);
        // Assert
        result.ShouldBe(cookieValue);
    }
    /// 测试 - GetCookie - 获取不存在的Cookie返回空字符串
    [Fact]
    public void GetCookie_ShouldReturnEmptyString_WhenCookieNotExists()
    {
        // Arrange
        const string cookieName = "nonExistentCookie";
        _mockRequestCookies.Setup(x => x[cookieName]).Returns((string)null);
        // Act
        var result = CookieHelper.GetCookie(_mockHttpContext.Object, cookieName);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - GetCookie - 空Cookie名称抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCookie_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace(string cookieName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            CookieHelper.GetCookie(_mockHttpContext.Object, cookieName));
        exception.ParamName.ShouldBe("name");
        exception.Message.ShouldContain("Cookie 名称不能为空或空白字符串");
    }
    /// 测试 - GetCookie - HttpContext为空返回空字符串
    [Fact]
    public void GetCookie_ShouldReturnEmptyString_WhenHttpContextIsNull()
    {
        // Act
        var result = CookieHelper.GetCookie(null, "testCookie");
        // Assert
        result.ShouldBe(string.Empty);
    }
    #endregion
    #region WriteCookie 测试
    /// 测试 - WriteCookie - 正常写入Cookie
    [Fact]
    public void WriteCookie_ShouldWriteCookie_WhenParametersAreValid()
    {
        // Arrange
        const string cookieName = "testCookie";
        const string cookieValue = "testValue";
        // Act
        CookieHelper.WriteCookie(_mockHttpContext.Object, cookieName, cookieValue);
        // Assert
        _mockResponseCookies.Verify(x => x.Append(
                cookieName,
                cookieValue,
                It.Is<CookieOptions>(opt => opt.HttpOnly == true)),
            Times.Once);
    }
    /// 测试 - WriteCookie - Cookie值为空时写入空字符串
    [Fact]
    public void WriteCookie_ShouldWriteEmptyString_WhenValueIsNull()
    {
        // Arrange
        const string cookieName = "testCookie";
        // Act
        CookieHelper.WriteCookie(_mockHttpContext.Object, cookieName, null);
        // Assert
        _mockResponseCookies.Verify(x => x.Append(
                cookieName,
                string.Empty,
                It.IsAny<CookieOptions>()),
            Times.Once);
    }
    /// 测试 - WriteCookie - HttpContext为空抛出异常
    [Fact]
    public void WriteCookie_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            CookieHelper.WriteCookie(null, "testCookie", "testValue"));
        exception.ParamName.ShouldBe("context");
        exception.Message.ShouldContain("HTTP 上下文不能为空");
    }
    /// 测试 - WriteCookie - Cookie名称为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WriteCookie_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace(string cookieName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            CookieHelper.WriteCookie(_mockHttpContext.Object, cookieName, "testValue"));
        exception.ParamName.ShouldBe("name");
        exception.Message.ShouldContain("Cookie 名称不能为空或空白字符串");
    }
    #endregion
    #region ClearCookie 测试
    /// 测试 - ClearCookie - 正常清空所有Cookie
    [Fact]
    public void ClearCookie_ShouldDeleteAllCookies_WhenCookiesExist()
    {
        // Arrange
        var cookieKeys = new List<string> { "cookie1", "cookie2", "cookie3" };
        _mockRequestCookies.Setup(x => x.Keys).Returns(cookieKeys);
        // Act
        CookieHelper.ClearCookie(_mockHttpContext.Object);
        // Assert
        foreach (var cookieName in cookieKeys)
        {
            _mockResponseCookies.Verify(x => x.Delete(cookieName), Times.Once);
        }
    }
    /// 测试 - ClearCookie - HttpContext为空抛出异常
    [Fact]
    public void ClearCookie_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            CookieHelper.ClearCookie(null));
        exception.ParamName.ShouldBe("context");
        exception.Message.ShouldContain("HTTP 上下文不能为空");
    }
    /// 测试 - ClearCookie - 无Cookie时正常执行
    [Fact]
    public void ClearCookie_ShouldNotThrow_WhenNoCookiesExist()
    {
        // Arrange
        _mockRequestCookies.Setup(x => x.Keys).Returns(new List<string>());
        // Act & Assert
        Should.NotThrow(() => CookieHelper.ClearCookie(_mockHttpContext.Object));
    }
    #endregion
}
