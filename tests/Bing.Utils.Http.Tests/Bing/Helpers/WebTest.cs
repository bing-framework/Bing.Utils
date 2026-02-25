using Bing.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;
namespace Bing.Helpers;
/// <summary>
/// Web操作 测试
/// </summary>
[Trait("Bing.Helpers", "Web")]
[Collection(WebHttpContextCollection.Name)]
public class WebTest : TestBase, IDisposable
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly Mock<HttpResponse> _mockResponse;
    private readonly Mock<IRequestCookieCollection> _mockRequestCookies;
    private readonly Mock<IResponseCookies> _mockResponseCookies;
    private readonly Mock<IQueryCollection> _mockQuery;
    private readonly Mock<IFormCollection> _mockForm;
    private readonly Mock<IHeaderDictionary> _mockHeaders;
    private readonly Mock<ConnectionInfo> _mockConnection;
    /// <inheritdoc />
    public WebTest(ITestOutputHelper output) : base(output)
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockResponse = new Mock<HttpResponse>();
        _mockRequestCookies = new Mock<IRequestCookieCollection>();
        _mockResponseCookies = new Mock<IResponseCookies>();
        _mockQuery = new Mock<IQueryCollection>();
        _mockForm = new Mock<IFormCollection>();
        _mockHeaders = new Mock<IHeaderDictionary>();
        _mockConnection = new Mock<ConnectionInfo>();
        // 设置模拟对象关系
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Response).Returns(_mockResponse.Object);
        _mockHttpContext.Setup(x => x.Connection).Returns(_mockConnection.Object);
        _mockRequest.Setup(x => x.Cookies).Returns(_mockRequestCookies.Object);
        _mockRequest.Setup(x => x.Query).Returns(_mockQuery.Object);
        _mockRequest.Setup(x => x.Form).Returns(_mockForm.Object);
        _mockRequest.Setup(x => x.Headers).Returns(_mockHeaders.Object);
        _mockResponse.Setup(x => x.Cookies).Returns(_mockResponseCookies.Object);
        // 设置 Web 类的静态属性
        Web.HttpContextAccessor = _mockHttpContextAccessor.Object;
    }
    public void Dispose()
    {
        Web.HttpContextAccessor = null;
    }
    #region 属性测试
    /// 测试 - HttpContext - 正常获取
    [Fact]
    public void HttpContext_ShouldReturnContext_WhenAccessorIsSet()
    {
        // Act
        var result = Web.HttpContext;
        // Assert
        result.ShouldBe(_mockHttpContext.Object);
    }
    /// 测试 - HttpContext - HttpContextAccessor为空时返回null
    [Fact]
    public void HttpContext_ShouldReturnNull_WhenAccessorIsNull()
    {
        // Arrange
        Web.HttpContextAccessor = null;
        // Act
        var result = Web.HttpContext;
        // Assert
        result.ShouldBeNull();
    }
    /// 测试 - Request - 正常获取
    [Fact]
    public void Request_ShouldReturnRequest_WhenContextExists()
    {
        // Act
        var result = Web.Request;
        // Assert
        result.ShouldBe(_mockRequest.Object);
    }
    /// 测试 - Response - 正常获取
    [Fact]
    public void Response_ShouldReturnResponse_WhenContextExists()
    {
        // Act
        var result = Web.Response;
        // Assert
        result.ShouldBe(_mockResponse.Object);
    }
    /// 测试 - LocalIpAddress - 正常获取本地IP
    [Fact]
    public void LocalIpAddress_ShouldReturnLocalIp_WhenConnectionExists()
    {
        // Arrange
        var localIp = IPAddress.Parse("192.168.1.100");
        _mockConnection.Setup(x => x.LocalIpAddress).Returns(localIp);
        // Act
        var result = Web.LocalIpAddress;
        // Assert
        result.ShouldBe("192.168.1.100");
    }
    /// 测试 - LocalIpAddress - 返回环回地址当IP为空时
    [Fact]
    public void LocalIpAddress_ShouldReturnLoopback_WhenLocalIpIsNull()
    {
        // Arrange
        _mockConnection.Setup(x => x.LocalIpAddress).Returns((IPAddress)null);
        // Act
        var result = Web.LocalIpAddress;
        // Assert
        result.ShouldBe(IPAddress.Loopback.ToString());
    }
    /// 测试 - LocalIpAddress - 异常时返回环回地址
    [Fact]
    public void LocalIpAddress_ShouldReturnLoopback_WhenExceptionOccurs()
    {
        // Arrange
        _mockConnection.Setup(x => x.LocalIpAddress).Throws<Exception>();
        // Act
        var result = Web.LocalIpAddress;
        // Assert
        result.ShouldBe(IPAddress.Loopback.ToString());
    }
    /// 测试 - RequestType - 正常获取请求方法
    [Fact]
    public void RequestType_ShouldReturnMethod_WhenRequestExists()
    {
        // Arrange
        _mockRequest.Setup(x => x.Method).Returns("POST");
        // Act
        var result = Web.RequestType;
        // Assert
        result.ShouldBe("POST");
    }
    /// 测试 - Form - 正常获取表单数据
    [Fact]
    public void Form_ShouldReturnFormCollection_WhenRequestExists()
    {
        // Act
        var result = Web.Form;
        // Assert
        result.ShouldBe(_mockForm.Object);
    }
    /// 测试 - AccessToken - 正常获取Bearer Token
    [Fact]
    public void AccessToken_ShouldReturnToken_WhenBearerAuthorizationExists()
    {
        // Arrange
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        const string authorization = $"Bearer {token}";
        var mockStringValues = new StringValues(authorization);
        _mockHeaders.Setup(x => x["Authorization"]).Returns(mockStringValues);
        // Act
        var result = Web.AccessToken;
        // Assert
        result.ShouldBe(token);
    }
    /// 测试 - AccessToken - 获取传统格式Token
    [Fact]
    public void AccessToken_ShouldReturnToken_WhenTraditionalFormatExists()
    {
        // Arrange
        const string token = "abc123";
        const string authorization = $"Token {token}";
        var mockStringValues = new StringValues(authorization);
        _mockHeaders.Setup(x => x["Authorization"]).Returns(mockStringValues);
        // Act
        var result = Web.AccessToken;
        // Assert
        result.ShouldBe(token);
    }
    /// 测试 - AccessToken - 授权头为空时返回null
    [Fact]
    public void AccessToken_ShouldReturnNull_WhenAuthorizationIsEmpty()
    {
        // Arrange
        var emptyStringValues = new StringValues(string.Empty);
        _mockHeaders.Setup(x => x["Authorization"]).Returns(emptyStringValues);
        // Act
        var result = Web.AccessToken;
        // Assert
        result.ShouldBeNull();
    }
    /// 测试 - Browser - 正常获取用户代理
    [Fact]
    public void Browser_ShouldReturnUserAgent_WhenHeaderExists()
    {
        // Arrange
        const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        _mockHeaders.Setup(x => x["User-Agent"]).Returns(userAgent);
        // Act
        var result = Web.Browser;
        // Assert
        result.ShouldBe(userAgent);
    }
    /// 测试 - ContentType - 正常获取内容类型
    [Fact]
    public void ContentType_ShouldReturnContentType_WhenRequestExists()
    {
        // Arrange
        const string contentType = "application/json";
        _mockRequest.Setup(x => x.ContentType).Returns(contentType);
        // Act
        var result = Web.ContentType;
        // Assert
        result.ShouldBe(contentType);
    }
    /// 测试 - QueryString - 正常获取查询字符串
    [Fact]
    public void QueryString_ShouldReturnQueryString_WhenRequestExists()
    {
        // Arrange
        var queryString = new QueryString("?name=test&value=123");
        _mockRequest.Setup(x => x.QueryString).Returns(queryString);
        // Act
        var result = Web.QueryString;
        // Assert
        result.ShouldBe("?name=test&value=123");
    }
    /// 测试 - IsLocal - 远程和本地IP相同时返回true
    [Fact]
    public void IsLocal_ShouldReturnTrue_WhenRemoteAndLocalIpAreEqual()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.100");
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(ip);
        _mockConnection.Setup(x => x.LocalIpAddress).Returns(ip);
        // Act
        var result = Web.IsLocal;
        // Assert
        result.ShouldBeTrue();
    }
    /// 测试 - IsLocal - 远程IP是环回地址时返回true
    [Fact]
    public void IsLocal_ShouldReturnTrue_WhenRemoteIpIsLoopback()
    {
        // Arrange
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Loopback);
        _mockConnection.Setup(x => x.LocalIpAddress).Returns((IPAddress)null); // 本地IP为空
        // Act
        var result = Web.IsLocal;
        // Assert
        result.ShouldBeTrue();
    }
    /// 测试 - IsLocal - 远程IP是IPv6环回地址时返回true
    [Fact]
    public void IsLocal_ShouldReturnTrue_WhenRemoteIpIsIPv6Loopback()
    {
        // Arrange
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.IPv6Loopback);
        _mockConnection.Setup(x => x.LocalIpAddress).Returns((IPAddress)null);
        // Act
        var result = Web.IsLocal;
        // Assert
        result.ShouldBeTrue();
    }
    /// 测试 - IsLocal - 远程IP不是环回地址且与本地IP不同时返回false
    [Fact]
    public void IsLocal_ShouldReturnFalse_WhenRemoteIpIsNotLocalAndNotLoopback()
    {
        // Arrange
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("8.8.8.8"));
        _mockConnection.Setup(x => x.LocalIpAddress).Returns(IPAddress.Parse("192.168.1.100"));
        // Act
        var result = Web.IsLocal;
        // Assert
        result.ShouldBeFalse();
    }
    /// 测试 - IsLocal - 远程IP为空时返回true
    [Fact]
    public void IsLocal_ShouldReturnTrue_WhenRemoteIpIsNull()
    {
        // Arrange
        _mockConnection.Setup(x => x.RemoteIpAddress).Returns((IPAddress)null);
        _mockConnection.Setup(x => x.LocalIpAddress).Returns(IPAddress.Parse("192.168.1.100"));
        // Act
        var result = Web.IsLocal;
        // Assert
        result.ShouldBeTrue();
    }
    /// 测试 - IsLocal - Connection为空时抛出异常
    [Fact]
    public void IsLocal_ShouldThrowArgumentNullException_WhenConnectionIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Connection).Returns((ConnectionInfo)null);
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() => Web.IsLocal);
        exception.ParamName.ShouldBe("connection");
    }
    #endregion
    #region GetParam 测试
    /// 测试 - GetParam - 正常获取查询参数
    [Fact]
    public void GetParam_ShouldReturnQueryValue_WhenQueryParamExists()
    {
        // Arrange
        const string paramName = "testParam";
        const string paramValue = "testValue";
        _mockQuery.Setup(x => x[paramName]).Returns(paramValue);
        // Act
        var result = Web.GetParam(paramName);
        // Assert
        result.ShouldBe(paramValue);
    }
    /// 测试 - GetParam - 获取表单参数
    [Fact]
    public void GetParam_ShouldReturnFormValue_WhenQueryParamNotExistsButFormParamExists()
    {
        // Arrange
        const string paramName = "testParam";
        const string formValue = "formValue";
        _mockQuery.Setup(x => x[paramName]).Returns(string.Empty);
        _mockForm.Setup(x => x[paramName]).Returns(formValue);
        // Act
        var result = Web.GetParam(paramName);
        // Assert
        result.ShouldBe(formValue);
    }
    /// 测试 - GetParam - 获取请求头参数
    [Fact]
    public void GetParam_ShouldReturnHeaderValue_WhenOnlyHeaderParamExists()
    {
        // Arrange
        const string paramName = "testParam";
        const string headerValue = "headerValue";
        _mockQuery.Setup(x => x[paramName]).Returns(string.Empty);
        _mockForm.Setup(x => x[paramName]).Returns(string.Empty);
        _mockHeaders.Setup(x => x[paramName]).Returns(headerValue);
        // Act
        var result = Web.GetParam(paramName);
        // Assert
        result.ShouldBe(headerValue);
    }
    /// 测试 - GetParam - 表单读取异常时继续获取请求头
    [Fact]
    public void GetParam_ShouldReturnHeaderValue_WhenFormThrowsException()
    {
        // Arrange
        const string paramName = "testParam";
        const string headerValue = "headerValue";
        _mockQuery.Setup(x => x[paramName]).Returns(string.Empty);
        _mockForm.Setup(x => x[paramName]).Throws<InvalidOperationException>();
        _mockHeaders.Setup(x => x[paramName]).Returns(headerValue);
        // Act
        var result = Web.GetParam(paramName);
        // Assert
        result.ShouldBe(headerValue);
    }
    /// 测试 - GetParam - Request为空时返回空字符串
    [Fact]
    public void GetParam_ShouldReturnEmptyString_WhenRequestIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Request).Returns((HttpRequest)null);
        // Act
        var result = Web.GetParam("anyParam");
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - GetParam - 参数名为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetParam_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace(string paramName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => Web.GetParam(paramName));
        exception.ParamName.ShouldBe("name");
        exception.Message.ShouldContain("参数名称不能为空或空白字符串");
    }
    #endregion
    #region UrlEncode 测试
    /// 测试 - UrlEncode - 正常编码
    [Fact]
    public void UrlEncode_ShouldReturnEncodedString_WhenInputIsValid()
    {
        // Arrange
        const string input = "http://example.com";
        // Act
        var result = Web.UrlEncode(input);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("%3A%2F%2F");
    }
    /// 测试 - UrlEncode - 大写编码
    [Fact]
    public void UrlEncode_ShouldReturnUpperCaseEncoding_WhenIsUpperIsTrue()
    {
        // Arrange
        const string input = "http://example.com";
        // Act
        var result = Web.UrlEncode(input, isUpper: true);
        // Assert
        result.ShouldContain("%3A%2F%2F"); // 应该是大写
    }
    /// 测试 - UrlEncode - 指定编码名称
    [Fact]
    public void UrlEncode_ShouldUseSpecifiedEncoding_WhenEncodingNameProvided()
    {
        // Arrange
        const string input = "测试中文";
        const string encoding = "UTF-8";
        // Act
        var result = Web.UrlEncode(input, encoding);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldNotBe(input);
    }
    /// 测试 - UrlEncode - 指定编码对象
    [Fact]
    public void UrlEncode_ShouldUseSpecifiedEncodingObject_WhenEncodingObjectProvided()
    {
        // Arrange
        const string input = "测试中文";
        var encoding = Encoding.UTF8;
        // Act
        var result = Web.UrlEncode(input, encoding);
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldNotBe(input);
    }
    /// 测试 - UrlEncode - 空字符串返回空字符串
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UrlEncode_ShouldReturnEmptyString_WhenInputIsNullOrEmpty(string input)
    {
        // Act
        var result = Web.UrlEncode(input);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - UrlEncode - 无效编码名称抛出异常
    [Fact]
    public void UrlEncode_ShouldThrowArgumentException_WhenEncodingNameIsInvalid()
    {
        // Arrange
        const string input = "test";
        const string invalidEncoding = "invalid-encoding";
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            Web.UrlEncode(input, invalidEncoding));
        exception.ParamName.ShouldBe("encoding");
        exception.Message.ShouldContain("无效的字符编码");
    }
    /// 测试 - UrlEncode - 编码对象为空抛出异常
    [Fact]
    public void UrlEncode_ShouldThrowArgumentNullException_WhenEncodingObjectIsNull()
    {
        // Arrange
        const string input = "test";
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Web.UrlEncode(input, (Encoding)null));
        exception.ParamName.ShouldBe("encoding");
    }
    #endregion
    #region UrlDecode 测试
    /// 测试 - UrlDecode - 正常解码
    [Fact]
    public void UrlDecode_ShouldReturnDecodedString_WhenInputIsValid()
    {
        // Arrange
        const string encoded = "http%3A%2F%2Fexample.com";
        const string expected = "http://example.com";
        // Act
        var result = Web.UrlDecode(encoded);
        // Assert
        result.ShouldBe(expected);
    }
    /// 测试 - UrlDecode - 指定编码解码
    [Fact]
    public void UrlDecode_ShouldDecodeWithSpecifiedEncoding_WhenEncodingProvided()
    {
        // Arrange
        const string encoded = "%E6%B5%8B%E8%AF%95"; // "测试" 的 UTF-8 编码
        var encoding = Encoding.UTF8;
        // Act
        var result = Web.UrlDecode(encoded, encoding);
        // Assert
        result.ShouldBe("测试");
    }
    /// 测试 - UrlDecode - 空字符串返回空字符串
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UrlDecode_ShouldReturnEmptyString_WhenInputIsNullOrEmpty(string input)
    {
        // Act
        var result = Web.UrlDecode(input);
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - UrlDecode - 编码对象为空抛出异常
    [Fact]
    public void UrlDecode_ShouldThrowArgumentNullException_WhenEncodingIsNull()
    {
        // Arrange
        const string encoded = "test";
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Web.UrlDecode(encoded, (Encoding)null));
        exception.ParamName.ShouldBe("encoding");
    }
    #endregion
    #region Cookie 操作测试
    /// 测试 - GetCookie - 正常获取Cookie
    [Fact]
    public void GetCookie_ShouldReturnCookieValue_WhenCookieExists()
    {
        // Arrange
        const string cookieName = "testCookie";
        const string cookieValue = "testValue";
        _mockRequestCookies.Setup(x => x[cookieName]).Returns(cookieValue);
        // Act
        var result = Web.GetCookie(cookieName);
        // Assert
        result.ShouldBe(cookieValue);
    }
    /// 测试 - GetCookie - Cookie不存在时返回null
    [Fact]
    public void GetCookie_ShouldReturnNull_WhenCookieNotExists()
    {
        // Arrange
        const string cookieName = "nonExistentCookie";
        _mockRequestCookies.Setup(x => x[cookieName]).Returns((string)null);
        // Act
        var result = Web.GetCookie(cookieName);
        // Assert
        result.ShouldBeNull();
    }
    /// 测试 - GetCookie - Cookie键名为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCookie_ShouldThrowArgumentException_WhenKeyIsNullOrWhiteSpace(string cookieName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => Web.GetCookie(cookieName));
        exception.ParamName.ShouldBe("key");
        exception.Message.ShouldContain("Cookie 键名不能为空或空白字符串");
    }
    /// 测试 - SetCookie - 正常设置Cookie
    [Fact]
    public void SetCookie_ShouldSetCookie_WhenParametersAreValid()
    {
        // Arrange
        const string cookieName = "testCookie";
        const string cookieValue = "testValue";
        // Act
        Web.SetCookie(cookieName, cookieValue);
        // Assert
        _mockResponseCookies.Verify(x => x.Append(cookieName, cookieValue), Times.Once);
    }
    /// 测试 - SetCookie - Cookie值为null时设置为空字符串
    [Fact]
    public void SetCookie_ShouldSetEmptyString_WhenValueIsNull()
    {
        // Arrange
        const string cookieName = "testCookie";
        // Act
        Web.SetCookie(cookieName, null);
        // Assert
        _mockResponseCookies.Verify(x => x.Append(cookieName, string.Empty), Times.Once);
    }
    /// 测试 - SetCookie - 使用Cookie选项设置
    [Fact]
    public void SetCookie_ShouldSetCookieWithOptions_WhenOptionsProvided()
    {
        // Arrange
        const string cookieName = "testCookie";
        const string cookieValue = "testValue";
        var options = new CookieOptions { HttpOnly = true, Secure = true };
        // Act
        Web.SetCookie(cookieName, cookieValue, options);
        // Assert
        _mockResponseCookies.Verify(x => x.Append(cookieName, cookieValue, options), Times.Once);
    }
    /// 测试 - SetCookie - Cookie键名为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCookie_ShouldThrowArgumentException_WhenKeyIsNullOrWhiteSpace(string cookieName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            Web.SetCookie(cookieName, "value"));
        exception.ParamName.ShouldBe("key");
        exception.Message.ShouldContain("Cookie 键名不能为空或空白字符串");
    }
    /// 测试 - SetCookie - Cookie选项为空抛出异常
    [Fact]
    public void SetCookie_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            Web.SetCookie("testCookie", "testValue", null));
        exception.ParamName.ShouldBe("options");
    }
    /// 测试 - SetCookie - Response为空抛出异常
    [Fact]
    public void SetCookie_ShouldThrowInvalidOperationException_WhenResponseIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Response).Returns((HttpResponse)null);
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            Web.SetCookie("testCookie", "testValue"));
        exception.Message.ShouldContain("HTTP 响应对象不可用");
    }
    /// 测试 - RemoveCookie - 正常移除Cookie
    [Fact]
    public void RemoveCookie_ShouldRemoveCookie_WhenKeyIsValid()
    {
        // Arrange
        const string cookieName = "testCookie";
        // Act
        Web.RemoveCookie(cookieName);
        // Assert
        _mockResponseCookies.Verify(x => x.Delete(cookieName), Times.Once);
    }
    /// 测试 - RemoveCookie - 使用选项移除Cookie
    [Fact]
    public void RemoveCookie_ShouldRemoveCookieWithOptions_WhenOptionsProvided()
    {
        // Arrange
        const string cookieName = "testCookie";
        var options = new CookieOptions { Path = "/test" };
        // Act
        Web.RemoveCookie(cookieName, options);
        // Assert
        _mockResponseCookies.Verify(x => x.Delete(cookieName, options), Times.Once);
    }
    /// 测试 - RemoveCookie - Cookie键名为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveCookie_ShouldThrowArgumentException_WhenKeyIsNullOrWhiteSpace(string cookieName)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            Web.RemoveCookie(cookieName));
        exception.ParamName.ShouldBe("key");
        exception.Message.ShouldContain("Cookie 键名不能为空或空白字符串");
    }
    #endregion
    #region GetFiles 测试
    /// 测试 - GetFiles - 获取有效文件列表
    [Fact]
    public void GetFiles_ShouldReturnValidFiles_WhenFilesExist()
    {
        // Arrange
        var mockFile1 = new Mock<IFormFile>();
        var mockFile2 = new Mock<IFormFile>();
        var mockFile3 = new Mock<IFormFile>();
        mockFile1.Setup(x => x.Length).Returns(100);
        mockFile2.Setup(x => x.Length).Returns(0); // 无效文件
        mockFile3.Setup(x => x.Length).Returns(200);
        var mockFiles = new Mock<IFormFileCollection>();
        mockFiles.Setup(x => x.Count).Returns(3);
        mockFiles.Setup(x => x.GetEnumerator()).Returns(new List<IFormFile>
        {
            mockFile1.Object,
            mockFile2.Object,
            mockFile3.Object
        }.GetEnumerator());
        _mockForm.Setup(x => x.Files).Returns(mockFiles.Object);
        // Act
        var result = Web.GetFiles();
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2); // 只有两个有效文件
    }
    /// 测试 - GetFiles - 无文件时返回空列表
    [Fact]
    public void GetFiles_ShouldReturnEmptyList_WhenNoFilesExist()
    {
        // Arrange
        _mockForm.Setup(x => x.Files).Returns((IFormFileCollection)null);
        // Act
        var result = Web.GetFiles();
        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
    /// 测试 - GetFiles - 文件集合为空时返回空列表
    [Fact]
    public void GetFiles_ShouldReturnEmptyList_WhenFilesCountIsZero()
    {
        // Arrange
        var mockFiles = new Mock<IFormFileCollection>();
        mockFiles.Setup(x => x.Count).Returns(0);
        _mockForm.Setup(x => x.Files).Returns(mockFiles.Object);
        // Act
        var result = Web.GetFiles();
        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
    /// 测试 - GetFile - 获取第一个有效文件
    [Fact]
    public void GetFile_ShouldReturnFirstValidFile_WhenFilesExist()
    {
        // Arrange
        var mockFile1 = new Mock<IFormFile>();
        var mockFile2 = new Mock<IFormFile>();
        mockFile1.Setup(x => x.Length).Returns(100);
        mockFile2.Setup(x => x.Length).Returns(200);
        var mockFiles = new Mock<IFormFileCollection>();
        mockFiles.Setup(x => x.Count).Returns(2);
        mockFiles.Setup(x => x.GetEnumerator()).Returns(new List<IFormFile>
        {
            mockFile1.Object,
            mockFile2.Object
        }.GetEnumerator());
        _mockForm.Setup(x => x.Files).Returns(mockFiles.Object);
        // Act
        var result = Web.GetFile();
        // Assert
        result.ShouldBe(mockFile1.Object);
    }
    /// 测试 - GetFile - 无文件时返回null
    [Fact]
    public void GetFile_ShouldReturnNull_WhenNoFilesExist()
    {
        // Arrange
        _mockForm.Setup(x => x.Files).Returns((IFormFileCollection)null);
        // Act
        var result = Web.GetFile();
        // Assert
        result.ShouldBeNull();
    }
    #endregion
    #region Redirect 测试
    /// 测试 - Redirect - 正常重定向
    [Fact]
    public void Redirect_ShouldRedirect_WhenUrlIsValid()
    {
        // Arrange
        const string url = "https://example.com";
        // Act
        Web.Redirect(url);
        // Assert
        _mockResponse.Verify(x => x.Redirect(url), Times.Once);
    }
    /// 测试 - Redirect - URL为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Redirect_ShouldThrowArgumentException_WhenUrlIsNullOrWhiteSpace(string url)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => Web.Redirect(url));
        exception.ParamName.ShouldBe("url");
        exception.Message.ShouldContain("重定向 URL 不能为空或空白字符串");
    }
    /// 测试 - Redirect - Response为空抛出异常
    [Fact]
    public void Redirect_ShouldThrowInvalidOperationException_WhenResponseIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Response).Returns((HttpResponse)null);
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            Web.Redirect("https://example.com"));
        exception.Message.ShouldContain("HTTP 响应对象不可用");
    }
    #endregion
    #region Write 测试
    ///// 测试 - Write - 正常输出文本
    //[Fact]
    //public void Write_ShouldWriteText_WhenTextIsValid()
    //{
    //    // Arrange
    //    const string text = "Hello World";
    //    var mockStream = new MemoryStream();
    //    var mockHeaders = new Mock<IHeaderDictionary>();
    //    _mockResponse.SetupProperty(x => x.ContentType);
    //    _mockResponse.Setup(x => x.Body).Returns(mockStream);
    //    _mockResponse.Setup(x => x.Headers).Returns(mockHeaders.Object);
    //    // Act
    //    Web.Write(text);
    //    // Assert
    //    _mockResponse.VerifySet(x => x.ContentType = "text/plain;charset=utf-8", Times.Once);
    //    _mockResponse.Object.ContentType.ShouldBe("text/plain;charset=utf-8");
    //    // 验证内容已写入流
    //    mockStream.Length.ShouldBeGreaterThan(0);
    //}
    ///// 测试 - Write - 文本为null时输出空字符串
    //[Fact]
    //public void Write_ShouldWriteEmptyString_WhenTextIsNull()
    //{
    //    // Arrange
    //    _mockResponse.SetupProperty(x => x.ContentType);
    //    _mockResponse.Setup(x => x.WriteAsync(It.IsAny<string>(), default))
    //        .Returns(Task.CompletedTask);
    //    // Act
    //    Web.Write((string)null);
    //    // Assert
    //    _mockResponse.Verify(x => x.WriteAsync(string.Empty, default), Times.Once);
    //}
    /// 测试 - Write - Response为空抛出异常
    [Fact]
    public void Write_ShouldThrowInvalidOperationException_WhenResponseIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Response).Returns((HttpResponse)null);
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            Web.Write("test"));
        exception.Message.ShouldContain("HTTP 响应对象不可用");
    }
    /// 测试 - Write - 正常写入响应正文
    [Fact]
    public void Write_ShouldWriteBodyAndSetContentType_WhenResponseIsAvailable()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        // Act
        Web.Write("hello");
        // Assert
        context.Response.ContentType.ShouldBe("text/plain;charset=utf-8");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var content = reader.ReadToEnd();
        content.ShouldBe("hello");
    }
    /// 测试 - WriteAsync - 响应已开始时抛出异常
    [Fact]
    public async Task WriteAsync_ShouldThrowInvalidOperationException_WhenResponseHasStarted()
    {
        // Arrange
        _mockResponse.Setup(x => x.HasStarted).Returns(true);
        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => Web.WriteAsync("test"));
        // Assert
        exception.Message.ShouldContain("响应已开始发送");
    }
    #endregion
    #region DownloadAsync 测试
    /// 测试 - DownloadAsync - 字节数组为空抛出异常
    [Theory]
    [InlineData(null)]
    public void DownloadAsync_ShouldThrowArgumentException_WhenBytesIsNull(byte[] bytes)
    {
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentException>(async () =>
            await Web.DownloadAsync(bytes, "test.txt"));
        exception.Result.ParamName.ShouldBe("bytes");
        exception.Result.Message.ShouldContain("文件字节数组不能为空");
    }
    /// 测试 - DownloadAsync - 空字节数组抛出异常
    [Fact]
    public void DownloadAsync_ShouldThrowArgumentException_WhenBytesIsEmpty()
    {
        // Arrange
        var emptyBytes = new byte[0];
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentException>(async () =>
            await Web.DownloadAsync(emptyBytes, "test.txt"));
        exception.Result.ParamName.ShouldBe("bytes");
        exception.Result.Message.ShouldContain("文件字节数组不能为空");
    }
    /// 测试 - DownloadAsync - 文件名为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DownloadAsync_ShouldThrowArgumentException_WhenFileNameIsNullOrWhiteSpace(string fileName)
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentException>(async () =>
            await Web.DownloadAsync(bytes, fileName));
        exception.Result.ParamName.ShouldBe("fileName");
        exception.Result.Message.ShouldContain("文件名不能为空");
    }
    /// 测试 - DownloadAsync - 编码为空抛出异常
    [Fact]
    public void DownloadAsync_ShouldThrowArgumentNullException_WhenEncodingIsNull()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        var mockResponse = new Mock<HttpResponse>();
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentNullException>(async () =>
            await Web.DownloadAsync(bytes, "test.txt", null, mockResponse.Object));
        exception.Result.ParamName.ShouldBe("encoding");
    }
    /// 测试 - DownloadAsync - Response为空抛出异常
    [Fact]
    public void DownloadAsync_ShouldThrowArgumentNullException_WhenResponseIsNull()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentNullException>(async () =>
            await Web.DownloadAsync(bytes, "test.txt", Encoding.UTF8, null));
        exception.Result.ParamName.ShouldBe("response");
    }
    /// 测试 - DownloadFileAsync - 文件路径为空抛出异常
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DownloadFileAsync_ShouldThrowArgumentException_WhenFilePathIsNullOrWhiteSpace(string filePath)
    {
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentException>(async () =>
            await Web.DownloadFileAsync(filePath, "test.txt"));
        exception.Result.ParamName.ShouldBe("filePath");
        exception.Result.Message.ShouldContain("文件路径不能为空");
    }
    /// 测试 - DownloadFileAsync - 文件不存在抛出异常
    [Fact]
    public void DownloadFileAsync_ShouldThrowFileNotFoundException_WhenFileNotExists()
    {
        // Arrange
        const string nonExistentFile = "C:\\NonExistent\\file.txt";
        // Act & Assert
        var exception = Should.ThrowAsync<FileNotFoundException>(async () =>
            await Web.DownloadFileAsync(nonExistentFile, "test.txt"));
        exception.Result.Message.ShouldContain("文件不存在");
    }
    /// 测试 - DownloadAsync - Stream为空抛出异常
    [Fact]
    public void DownloadAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        // Act & Assert
        var exception = Should.ThrowAsync<ArgumentNullException>(async () =>
            await Web.DownloadAsync((Stream)null, "test.txt"));
        exception.Result.ParamName.ShouldBe("stream");
    }
    /// 测试 - DownloadAsync - 字节下载成功并写入响应
    [Fact]
    public async Task DownloadAsync_WithBytes_ShouldWriteHeadersAndBody()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3, 4 };
        var response = new DefaultHttpContext().Response;
        response.Body = new MemoryStream();
        // Act
        await Web.DownloadAsync(bytes, "test file.txt", Encoding.UTF8, response);
        // Assert
        response.ContentType.ShouldBe("application/octet-stream");
        response.Headers["Content-Disposition"].ToString().ShouldContain("attachment; filename=");
        response.Headers["Content-Disposition"].ToString().ShouldContain("testfile.txt");
        response.Headers["Content-Length"].ToString().ShouldBe("4");
        response.Body.Position = 0;
        using var memory = new MemoryStream();
        await response.Body.CopyToAsync(memory);
        memory.ToArray().ShouldBe(bytes);
    }
    /// 测试 - DownloadAsync - 使用当前上下文响应对象下载成功
    [Fact]
    public async Task DownloadAsync_Overload_ShouldUseCurrentResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var bytes = new byte[] { 9, 8, 7 };
        // Act
        await Web.DownloadAsync(bytes, "demo.txt");
        // Assert
        context.Response.Headers["Content-Length"].ToString().ShouldBe("3");
        context.Response.Body.Position = 0;
        using var memory = new MemoryStream();
        await context.Response.Body.CopyToAsync(memory);
        memory.ToArray().ShouldBe(bytes);
    }
    #endregion
    #region Body 和 GetBodyAsync 测试
    /// 测试 - Body - Request为空时返回空字符串
    [Fact]
    public void Body_ShouldReturnEmptyString_WhenRequestIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Request).Returns((HttpRequest)null);
        // Act
        var result = Web.Body;
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - Body - 正常读取请求正文
    [Fact]
    public void Body_ShouldReturnRequestBody_WhenRequestHasBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":\"bing\"}"));
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        // Act
        var result = Web.Body;
        // Assert
        result.ShouldBe("{\"name\":\"bing\"}");
    }
    /// 测试 - GetBodyAsync - Request为空时返回空字符串
    [Fact]
    public async Task GetBodyAsync_ShouldReturnEmptyString_WhenRequestIsNull()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.Request).Returns((HttpRequest)null);
        // Act
        var result = await Web.GetBodyAsync();
        // Assert
        result.ShouldBe(string.Empty);
    }
    /// 测试 - GetBodyAsync - 正常读取请求正文
    [Fact]
    public async Task GetBodyAsync_ShouldReturnRequestBody_WhenRequestHasBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("async-body"));
        Web.HttpContextAccessor = new HttpContextAccessor { HttpContext = context };
        // Act
        var result = await Web.GetBodyAsync();
        // Assert
        result.ShouldBe("async-body");
    }
    #endregion
    #region Host 测试
    /// 测试 - Host - HttpContext为空时返回本地主机名
    [Fact]
    public void Host_ShouldReturnLocalHostName_WhenHttpContextIsNull()
    {
        // Arrange
        Web.HttpContextAccessor = null;
        // Act
        var result = Web.Host;
        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe(System.Net.Dns.GetHostName());
    }
    #endregion
}
