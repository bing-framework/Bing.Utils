using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Bing.Extensions;
using Bing.Helpers;
using Bing.Http.Clients.Internal;
using Bing.IO;
using Bing.Serialization.SystemTextJson;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Bing.Http.Clients;

/// <summary>
/// Http请求
/// </summary>
/// <typeparam name="TResult">结果类型</typeparam>
public partial class HttpRequest<TResult> : IHttpRequest<TResult>, IHttpRequestResponse<TResult>,
    IHttpRequestInterceptorRegistration<TResult>, IHttpRequestObservationRegistration<TResult> where TResult : class
{
    #region 字段

    /// <summary>
    /// Http客户端工厂
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Http客户端
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Http客户端名称
    /// </summary>
    private string _httpClientName;

    /// <summary>
    /// Http方法
    /// </summary>
    private readonly HttpMethod _httpMethod;

    /// <summary>
    /// 服务地址
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// Json序列化配置
    /// </summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// 响应正文读取和交付给调用方时允许的最大字节数。
    /// </summary>
    private long? _maxResponseBytes;
    /// <summary>
    /// 未显式配置时使用的默认 JSON 序列化选项。
    /// </summary>
    private JsonSerializerOptions _defaultJsonSerializerOptions;
    /// <summary>
    /// 当前异步请求操作的取消令牌上下文。
    /// </summary>
    private readonly System.Threading.AsyncLocal<CancellationToken> _operationToken = new();
    /// <summary>
    /// 按注册顺序执行的请求拦截器。
    /// </summary>
    private readonly List<HttpRequestInterceptor> _interceptors = new();
    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="HttpRequest{TResult}"/>类型的实例
    /// </summary>
    /// <param name="httpClientFactory">Http客户端工厂</param>
    /// <param name="httpClient">Http客户端</param>
    /// <param name="httpMethod">Http方法</param>
    /// <param name="url">服务地址</param>
    public HttpRequest(IHttpClientFactory httpClientFactory, HttpClient httpClient, HttpMethod httpMethod, string url)
    {
        if (httpClientFactory == null && httpClient == null)
            throw new ArgumentNullException(nameof(httpClientFactory));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClient;
        _httpMethod = httpMethod;
        _url = url;
        HeaderParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        QueryParams = new Dictionary<string, string>();
        Params = new Dictionary<string, object>();
        Files = new List<FileData>();
        Cookies = new Dictionary<string, string>();
        HttpContentType = Http.HttpContentType.Json.Description();
        CharacterEncoding = System.Text.Encoding.UTF8;
        IsUseCookies = true;
        IsFileParameterQuotes = true;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 基地址
    /// </summary>
    protected string BaseAddressUri { get; private set; }

    /// <summary>
    /// 证书路径
    /// </summary>
    protected string CertificatePath { get; private set; }

    /// <summary>
    /// 证书密码
    /// </summary>
    protected string CertificatePassword { get; private set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    protected string HttpContentType { get; private set; }

    /// <summary>
    /// 字符编码
    /// </summary>
    protected Encoding CharacterEncoding { get; private set; }

    /// <summary>
    /// 超时时间间隔
    /// </summary>
    protected TimeSpan? HttpTimeout { get; private set; }

    /// <summary>
    /// 请求头参数集合
    /// </summary>
    protected IDictionary<string, string> HeaderParams { get; }

    /// <summary>
    /// Cookie参数集合
    /// </summary>
    protected IDictionary<string, string> Cookies { get; }

    /// <summary>
    /// 查询字符串参数集合
    /// </summary>
    protected IDictionary<string, string> QueryParams { get; }

    /// <summary>
    /// 参数集合
    /// </summary>
    protected IDictionary<string, object> Params { get; }

    /// <summary>
    /// 参数
    /// </summary>
    protected object Param { get; private set; }

    /// <summary>
    /// 文件数据列表
    /// </summary>
    protected List<FileData> Files { get; private set; }

    /// <summary>
    /// 是否自动携带cookie
    /// </summary>
    protected bool IsUseCookies { get; private set; }

    /// <summary>
    /// 文件上传参数是否添加双引号
    /// </summary>
    protected bool IsFileParameterQuotes { get; private set; }

    /// <summary>
    /// 发送前操作
    /// </summary>
    protected Func<HttpRequestMessage, bool> SendBeforeAction { get; private set; }

    /// <summary>
    /// 发送后操作
    /// </summary>
    protected Func<HttpResponseMessage, Task<TResult>> SendAfterAction { get; private set; }

    /// <summary>
    /// 支持协作取消的发送后操作。
    /// </summary>
    protected Func<HttpResponseMessage, CancellationToken, Task<TResult>> SendAfterWithCancellationAction { get; private set; }

    /// <summary>
    /// 结果转换操作
    /// </summary>
    protected Func<string, TResult> ConvertAction { get; private set; }

    /// <summary>
    /// 执行成功操作
    /// </summary>
    protected Action<TResult> SuccessAction { get; private set; }

    /// <summary>
    /// 执行成功操作
    /// </summary>
    protected Func<TResult, Task> SuccessFunc { get; private set; }

    /// <summary>
    /// 执行失败操作
    /// </summary>
    protected Action<HttpResponseMessage, object> FailAction { get; private set; }

    /// <summary>
    /// 执行完成操作
    /// </summary>
    protected Action<HttpResponseMessage, object> CompleteAction { get; private set; }

    #endregion

    #region HttpClientName(设置Http客户端名称)

    /// <summary>
    /// 设置外部配置的HttpClient实例名称
    /// </summary>
    /// <param name="name">HttpClient名称</param>
    public IHttpRequest<TResult> HttpClientName(string name)
    {
        _httpClientName = name;
        return this;
    }

    #endregion

    #region BaseAddress(设置基地址)

    /// <summary>
    /// 设置基地址
    /// </summary>
    /// <param name="baseAddress">基地址</param>
    public IHttpRequest<TResult> BaseAddress(string baseAddress)
    {
        BaseAddressUri = baseAddress;
        return this;
    }

    #endregion

    #region ContentType(设置内容类型)

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    public IHttpRequest<TResult> ContentType(HttpContentType contentType) => ContentType(contentType.Description());

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    public IHttpRequest<TResult> ContentType(string contentType)
    {
        HttpContentType = contentType;
        return this;
    }

    #endregion

    #region Encoding(设置字符编码)

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码，范例：gb2312</param>
    public IHttpRequest<TResult> Encoding(string encoding) => Encoding(System.Text.Encoding.GetEncoding(encoding));

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码</param>
    public IHttpRequest<TResult> Encoding(Encoding encoding)
    {
        CharacterEncoding = encoding;
        return this;
    }

    #endregion

    #region BearerToken(设置访问令牌)

    /// <summary>
    /// 设置访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    public IHttpRequest<TResult> BearerToken(string token)
    {
        Header("Authorization", $"Bearer {token}");
        return this;
    }

    #endregion

    #region Certificate(设置证书)

    /// <summary>
    /// 设置证书
    /// </summary>
    /// <param name="path">证书路径</param>
    /// <param name="password">证书密码</param>
    public IHttpRequest<TResult> Certificate(string path, string password)
    {
        throw new NotSupportedException("Configure client certificates when registering a named HttpClient; per-request handler changes are unsafe.");
    }

    #endregion

    #region IgnoreSsl(忽略SSL证书)

    /// <summary>
    /// 忽略SSL证书
    /// </summary>
    public IHttpRequest<TResult> IgnoreSsl()
    {
        throw new NotSupportedException("Configure TLS validation when registering a named HttpClient; per-request handler changes are unsafe.");
    }

    #endregion

    #region JsonSerializerOptions(设置Json序列化配置)

    /// <summary>
    /// 设置Json序列化配置
    /// </summary>
    /// <param name="options">Json序列化配置</param>
    public IHttpRequest<TResult> JsonSerializerOptions(JsonSerializerOptions options)
    {
        _jsonSerializerOptions = options;
        return this;
    }

    #endregion

    #region GetJsonSerializerOptions(获取Json序列化配置)

    /// <summary>
    /// 获取Json序列化配置
    /// </summary>
    protected virtual JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (_jsonSerializerOptions != null)
            return _jsonSerializerOptions;
        return _defaultJsonSerializerOptions ??= new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
#if NET5_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
#else
            IgnoreNullValues = true,
#endif
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = {
                new DateTimeJsonConverter(),
                new NullableDateTimeJsonConverter()
            }
        };
    }

    #endregion

    #region Timeout(设置超时时间)

    /// <summary>
    /// 设置超时时间
    /// </summary>
    /// <param name="timeout">超时时间。单位：秒</param>
    public IHttpRequest<TResult> Timeout(int timeout) => Timeout(new TimeSpan(0, 0, timeout));

    /// <summary>
    /// 设置超时时间
    /// </summary>
    /// <param name="timeout">超时时间</param>
    public IHttpRequest<TResult> Timeout(TimeSpan timeout)
    {
        HttpTimeout = timeout;
        return this;
    }

    #endregion

    #region Header(设置请求头)

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Header(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (HeaderParams.ContainsKey(key))
            HeaderParams.Remove(key);
        HeaderParams.Add(key, value);
        return this;
    }

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="headers">请求头键值对集合</param>
    public IHttpRequest<TResult> Header(IDictionary<string, string> headers)
    {
        if (headers == null)
            return this;
        foreach (var header in headers)
            Header(header.Key, header.Value);
        return this;
    }

    #endregion

    #region QueryString(设置查询字符串)

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> QueryString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (QueryParams.ContainsKey(key))
            QueryParams.Remove(key);
        QueryParams.Add(key, value);
        return this;
    }

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串键值对集合</param>
    public IHttpRequest<TResult> QueryString(IDictionary<string, string> queryString)
    {
        if (queryString == null)
            return this;
        foreach (var param in queryString)
            QueryString(param.Key, param.Value);
        return this;
    }

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串对象</param>
    public IHttpRequest<TResult> QueryString(object queryString)
    {
        var dict = ToDictionary(queryString);
        foreach (var param in dict)
            QueryString(param.Key, param.Value.ToString());
        return this;
    }

    #endregion

    #region UseCookies(设置是否自动携带Cookie)

    /// <summary>
    /// 设置是否自动携带Cookie
    /// </summary>
    /// <param name="isUseCookies">是否自动携带Cookie</param>
    public IHttpRequest<TResult> UseCookies(bool isUseCookies = true)
    {
        throw new NotSupportedException("Configure cookie handling when registering a named HttpClient; pooled handlers cannot be changed per request.");
    }

    #endregion

    #region Cookie(设置Cookie)

    /// <summary>
    /// 设置Cookie
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Cookie(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (Cookies.ContainsKey(key))
            Cookies.Remove(key);
        Cookies.Add(key, value);
        return this;
    }

    /// <summary>
    /// 设置Cookie集合
    /// </summary>
    /// <param name="cookies">Cookie集合</param>
    public IHttpRequest<TResult> Cookie(IDictionary<string, string> cookies)
    {
        if (cookies == null)
            return this;
        foreach (var cookie in cookies)
            Cookie(cookie.Key, cookie.Value);
        return this;
    }

    #endregion

    #region Content(添加内容参数)

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Content(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (value == null)
            return this;
        if (Params.ContainsKey(key))
            Params.Remove(key);
        Params.Add(key, value);
        return this;
    }

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="parameters">参数字典</param>
    public IHttpRequest<TResult> Content(IDictionary<string, object> parameters)
    {
        if (parameters == null)
            return this;
        foreach (var param in parameters)
            Content(param.Key, param.Value);
        return this;
    }

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Content(object value)
    {
        Param = value;
        return this;
    }

    #endregion

    #region JsonContent(添加Json参数)

    /// <summary>
    /// 添加内容类型为 application/json 的参数
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> JsonContent(object value)
    {
        ContentType(Http.HttpContentType.Json);
        return Content(value);
    }

    #endregion

    #region XmlContent(添加Xml参数)

    /// <summary>
    /// 添加内容类型为 text/xml 的参数
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> XmlContent(string value)
    {
        ContentType(Http.HttpContentType.Xml);
        return Content(value);
    }

    #endregion

    #region FileContent(添加文件参数)

    /// <summary>
    /// 添加内容类型为 multipart/form-data 的文件参数
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="name">参数名。默认值：file</param>
    public IHttpRequest<TResult> FileContent(Stream stream, string fileName, string name = "file")
        => FileContent(stream, fileName, name, false);

    /// <inheritdoc />
    public IHttpRequest<TResult> FileContent(Stream stream, string fileName, string name, bool leaveOpen)
    {
        ContentType(Http.HttpContentType.FormData);
        ReplaceFile(name, stream);
        Files.Add(new FileData(stream, fileName, name, leaveOpen));
        return this;
    }

    /// <summary>
    /// 添加内容类型为 multipart/form-data 的文件参数
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="name">参数名。默认值：file</param>
    public IHttpRequest<TResult> FileContent(string filePath, string name = "file")
    {
        ContentType(Http.HttpContentType.FormData);
        ReplaceFile(name);
        Files.Add(new FileData(filePath, name));
        return this;
    }

    /// <summary>
    /// 替换同名上传文件，并按流所有权规则释放被替换的文件流。
    /// </summary>
    /// <param name="name">表单中的文件参数名。</param>
    /// <param name="replacement">新的文件流；为空时移除该参数。</param>
    private void ReplaceFile(string name, Stream replacement = null)
    {
        foreach (var file in Files.Where(file => file.Name == name).ToArray())
        {
            if (!file.LeaveOpen && file.Stream != null && !ReferenceEquals(file.Stream, replacement))
                file.Stream.Dispose();
            Files.Remove(file);
        }
    }

    #endregion

    #region FileParameterQuotes(文件上传参数是否添加双引号)

    /// <summary>
    /// 文件上传参数是否添加双引号
    /// </summary>
    /// <param name="isQuote">是否添加双引号</param>
    public IHttpRequest<TResult> FileParameterQuotes(bool isQuote = true)
    {
        IsFileParameterQuotes = isQuote;
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> MaxResponseBytes(long maxBytes)
    {
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        _maxResponseBytes = maxBytes;
        return this;
    }

    #endregion

    #region OnSendBefore(发送前事件)

    /// <summary>
    /// 发送前事件
    /// </summary>
    /// <param name="action">发送前操作，返回false取消发送</param>
    public IHttpRequest<TResult> OnSendBefore(Func<HttpRequestMessage, bool> action)
    {
        SendBeforeAction = action;
        return this;
    }

    #endregion

    #region OnSendAfter(发送后事件)

    /// <summary>
    /// 发送后事件
    /// </summary>
    /// <param name="action">发送后操作，自定义解析返回值</param>
    public IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, Task<TResult>> action)
    {
        SendAfterAction = action;
        SendAfterWithCancellationAction = null;
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, CancellationToken, Task<TResult>> action)
    {
        SendAfterWithCancellationAction = action;
        SendAfterAction = null;
        return this;
    }

    #endregion

    #region OnConvert(结果转换事件)

    /// <summary>
    /// 结果转换事件
    /// </summary>
    /// <param name="action">结果转换操作，参数为响应内容</param>
    public IHttpRequest<TResult> OnConvert(Func<string, TResult> action)
    {
        ConvertAction = action;
        return this;
    }

    #endregion

    #region OnSuccess(请求成功事件)

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    public IHttpRequest<TResult> OnSuccess(Action<TResult> action)
    {
        SuccessAction = action;
        return this;
    }

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    public IHttpRequest<TResult> OnSuccess(Func<TResult, Task> action)
    {
        SuccessFunc = action;
        return this;
    }

    #endregion

    #region OnFail(请求失败事件)

    /// <summary>
    /// 请求失败事件
    /// </summary>
    /// <param name="action">执行失败操作，参数为响应消息和响应内容</param>
    public IHttpRequest<TResult> OnFail(Action<HttpResponseMessage, object> action)
    {
        FailAction = action;
        return this;
    }

    #endregion

    #region OnComplete(请求完成事件)

    /// <summary>
    /// 请求完成事件，不论成功失败都会执行
    /// </summary>
    /// <param name="action">执行完成操作，参数为响应消息和响应内容</param>
    public IHttpRequest<TResult> OnComplete(Action<HttpResponseMessage, object> action)
    {
        CompleteAction = action;
        return this;
    }

    #endregion

    #region GetResultAsync(获取结果)

    /// <inheritdoc />
    public Task<HttpResponseResult<TResult>> GetResponseAsync(bool includeRawContent = false, CancellationToken cancellationToken = default)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
            return FailPreflightAsync<HttpResponseResult<TResult>>(
                new NotSupportedException("GetResponseAsync cannot be combined with OnSendAfter; remove the custom response handler."));
        return ExecuteAsync((response, token) => ReadResponseResultAsync(response, token, includeRawContent), cancellationToken);
    }

    /// <inheritdoc />
    public void AddInterceptor(HttpRequestInterceptor interceptor)
    {
        if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
        lock (_interceptors) _interceptors.Add(interceptor);
    }

    /// <summary>
    /// 获取结果
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async (response, token) =>
        {
            _operationToken.Value = token;
            try
            {
                if (_maxResponseBytes.HasValue && (SendAfterAction != null || SendAfterWithCancellationAction != null) && response.Content != null)
                {
                    try
                    {
                        var bytes = await ReadResponseBytesAsync(response, token);
                        var original = response.Content;
                        var buffered = new ByteArrayContent(bytes);
                        foreach (var header in original.Headers)
                            buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        response.Content = buffered;
                        original.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CompleteHandlerPreservingException(response, null, ex);
                        throw;
                    }
                }
                return await SendAfterAsync(response);
            }
            finally { _operationToken.Value = default; }
        }, cancellationToken);
    }

    #endregion

    #region CreateMessage(创建请求消息)

    /// <summary>
    /// 创建请求消息
    /// </summary>
    protected virtual HttpRequestMessage CreateMessage()
    {
        var uri = GetUrl(_url);
        if (!string.IsNullOrWhiteSpace(BaseAddressUri))
            uri = new Uri(new Uri(BaseAddressUri), uri).ToString();
        var message = new HttpRequestMessage(_httpMethod, uri);
        try
        {
            AddCookies();
            AddHeaders(message);
            message.Content = CreateHttpContent();
            return message;
        }
        catch (Exception ex)
        {
            DisposePreservingException(message, ex);
            throw;
        }
    }

    /// <summary>
    /// 获取服务地址
    /// </summary>
    /// <param name="url">服务地址</param>
    protected virtual string GetUrl(string url) => QueryHelpers.AddQueryString(url, QueryParams);

    /// <summary>
    /// 添加Cookie
    /// </summary>
    protected virtual void AddCookies()
    {
        if (Cookies.Count == 0)
            return;
        var cookieValues = new List<CookieHeaderValue>();
        foreach (var cookie in Cookies)
            cookieValues.Add(new CookieHeaderValue(cookie.Key, cookie.Value));
        Header("Cookie", string.Join("; ", cookieValues.Select(t => t.ToString())));
    }

    /// <summary>
    /// 添加请求头
    /// </summary>
    /// <param name="message">请求消息</param>
    protected virtual void AddHeaders(HttpRequestMessage message)
    {
        foreach (var header in HeaderParams)
            message.Headers.Add(header.Key, header.Value);
    }

    /// <summary>
    /// 创建请求内容
    /// </summary>
    protected virtual HttpContent CreateHttpContent()
    {
        if (!System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(HttpContentType, out var parsedContentType))
            return null;
        var contentType = parsedContentType.MediaType.ToLowerInvariant();
        if (contentType.StartsWith("application/", StringComparison.Ordinal) && contentType.EndsWith("+json", StringComparison.Ordinal))
            return CreateJsonContent();
        switch (contentType)
        {
            case "application/x-www-form-urlencoded":
                return CreateFormContent();
            case "application/json":
                return CreateJsonContent();
            case "text/xml":
                return CreateXmlContent();
            case "multipart/form-data":
                return CreateFileUploadContent();
        }
        return null;
    }

    /// <summary>
    /// 创建表单内容
    /// </summary>
    protected virtual HttpContent CreateFormContent() =>
        new FormUrlEncodedContent(GetParameters().ToDictionary(t => t.Key, t => t.Value.SafeString()));

    /// <summary>
    /// 获取参数
    /// </summary>
    protected IDictionary<string, object> GetParameters()
    {
        var result = new Dictionary<string, object>(Params);
        var dict = ToDictionary(Param);
        if (dict == null)
            return result;
        foreach (var param in dict)
        {
            if (result.ContainsKey(param.Key))
                continue;
            result.Add(param.Key, param.Value);
        }
        return result;
    }

    /// <summary>
    /// 对象转换为字典
    /// </summary>
    /// <param name="data">对象</param>
    protected IDictionary<string, object> ToDictionary(object data)
    {
        var result = Conv.ToDictionary(data);
        return result.Where(t => t.Value != null).ToDictionary(t => t.Key, t => t.Value);
    }

    /// <summary>
    /// 创建Json内容
    /// </summary>
    protected virtual HttpContent CreateJsonContent()
    {
        var content = GetJsonContentValue();
        if (string.IsNullOrWhiteSpace(content))
            return null;
        var parsedContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(HttpContentType);
        parsedContentType.CharSet = CharacterEncoding.WebName;
        var result = new StringContent(content, CharacterEncoding, parsedContentType.MediaType);
        result.Headers.ContentType = parsedContentType;
        return result;
    }

    /// <summary>
    /// 获取Json内容值
    /// </summary>
    private string GetJsonContentValue()
    {
        var options = GetJsonSerializerOptions();
        if (Param != null && Params.Count > 0)
            return Json.ToJson(GetParameters(), options);
        if (Param != null)
            return Json.ToJson(Param, options);
        if (Params.Count > 0)
            return Json.ToJson(Params, options);
        return null;
    }

    /// <summary>
    /// 创建Xml内容
    /// </summary>
    protected virtual HttpContent CreateXmlContent() => new StringContent(Param.SafeString(), CharacterEncoding, "text/xml");

    /// <summary>
    /// 创建文件上传内容
    /// </summary>
    protected virtual HttpContent CreateFileUploadContent()
    {
        var content = new MultipartFormDataContent(GetBoundary());
        try
        {
            AddFileParameters(content);
            AddFileData(content);
            ClearBoundaryQuotes(content);
            return content;
        }
        catch (Exception ex)
        {
            DisposePreservingException(content, ex);
            throw;
        }
    }

    /// <summary>
    /// 获取 multipart/form-data 分隔符
    /// </summary>
    protected virtual string GetBoundary() => $"-----{Guid.NewGuid()}";

    /// <summary>
    /// 添加文件参数
    /// </summary>
    /// <param name="content">表单内容</param>
    protected void AddFileParameters(MultipartFormDataContent content)
    {
        var parameters = GetParameters();
        foreach (var parameter in parameters)
        {
            var item = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(parameter.Value.SafeString()));
            content.Add(item, GetFileParameter(parameter.Key));
        }
    }

    /// <summary>
    /// 获取文件参数
    /// </summary>
    /// <param name="param">参数</param>
    protected string GetFileParameter(string param) => IsFileParameterQuotes ? "\"" + param + "\"" : param;

    /// <summary>
    /// 添加文件数据
    /// </summary>
    /// <param name="content">表单内容</param>
    protected void AddFileData(MultipartFormDataContent content)
    {
        foreach (var file in Files)
        {
            if (file.Stream != null)
            {
                if (file.Stream.CanSeek) file.Stream.Seek(0, SeekOrigin.Begin);
                AddFileStream(content, new NonClosingStream(file.Stream), file.Name, file.FileName);
                continue;
            }
            if (string.IsNullOrWhiteSpace(file.FilePath))
                continue;
            var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, true);
            AddFileStream(content, stream, file.Name, Path.GetFileName(file.FilePath));
        }
    }

    /// <summary>
    /// 将上传流加入多部分表单内容。
    /// </summary>
    private void AddFileStream(MultipartFormDataContent content, Stream stream, string name, string fileName)
    {
        var fileContent = new StreamContent(stream);
        try
        {
            content.Add(fileContent, GetFileParameter(name), GetFileParameter(fileName));
            if (fileContent.Headers.ContentDisposition != null)
                fileContent.Headers.ContentDisposition.FileNameStar = null;
        }
        catch (Exception ex)
        {
            DisposePreservingException(fileContent, ex);
            throw;
        }
    }

    /// <summary>
    /// 添加文件数据
    /// </summary>
    /// <param name="content">表单内容</param>
    /// <param name="bytes">文件字节数组</param>
    /// <param name="name">参数名</param>
    /// <param name="fileName">文件名</param>
    protected void AddFileData(MultipartFormDataContent content, byte[] bytes, string name, string fileName)
    {
        if (bytes == null)
            return;
        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, GetFileParameter(name), GetFileParameter(fileName));
        if (fileContent.Headers is { ContentDisposition: not null })
            fileContent.Headers.ContentDisposition.FileNameStar = null;
    }

    /// <summary>
    /// 清除分隔符双引号
    /// </summary>
    /// <param name="content">表单内容</param>
    protected void ClearBoundaryQuotes(MultipartFormDataContent content)
    {
        var boundary = content?.Headers?.ContentType.Parameters.FirstOrDefault(o => o.Name == "boundary");
        if (boundary == null)
            return;
        boundary.Value = boundary.Value?.Replace("\"", null);
    }

    #endregion

    #region SendBefore(发送前操作)

    /// <summary>
    /// 发送前操作
    /// </summary>
    /// <param name="message">请求消息</param>
    protected virtual bool SendBefore(HttpRequestMessage message)
    {
        if (SendBeforeAction == null)
            return true;
        return SendBeforeAction(message);
    }

    #endregion

    #region SendAsync(发送请求)

    private Task<T> ExecuteAsync<T>(Func<HttpResponseMessage, CancellationToken, Task<T>> process,
        CancellationToken cancellationToken, ObservationState observation = null, bool nested = false,
        bool allowRetries = true, Action<HttpRequestMessage, int, HttpClient> prepareMessage = null)
    {
        if (nested) return ExecuteCoreAsync(process, cancellationToken, observation, allowRetries, prepareMessage);
        if (_completionObserver == null) return ExecuteCoreAsync(process, cancellationToken, null, allowRetries, prepareMessage);
        return ObserveOperationAsync(state => ExecuteCoreAsync(process, cancellationToken, state, allowRetries, prepareMessage));
    }

    private async Task<T> ExecuteCoreAsync<T>(Func<HttpResponseMessage, CancellationToken, Task<T>> process,
        CancellationToken cancellationToken, ObservationState observation, bool allowRetries = true,
        Action<HttpRequestMessage, int, HttpClient> prepareMessage = null)
    {
        HttpClient client = null;
        CancellationTokenSource timeout = null;
        Exception executionError = null;
        var retryObserver = _retryObserver;
        var retryObservationErrorHandler = _retryObservationErrorHandler;
        try
        {
            var hasFiniteTimeout = HttpTimeout.HasValue && HttpTimeout.Value != System.Threading.Timeout.InfiniteTimeSpan;
            timeout = hasFiniteTimeout ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
            if (timeout != null) timeout.CancelAfter(HttpTimeout.Value);
            var token = timeout?.Token ?? cancellationToken;
            var deadline = hasFiniteTimeout ? GetUtcNow() + HttpTimeout.Value : DateTimeOffset.MaxValue;
            client = GetClient();
            var retries = allowRetries ? _retryOptions?.MaxRetries ?? 0 : 0;
            if (_retryOptions?.HostHandlerOwnsRetries == true)
                throw new InvalidOperationException("The host HTTP handler owns retries; request-level Retry cannot be configured at the same time.");
            HttpRequestInterceptor[] interceptors;
            lock (_interceptors) interceptors = _interceptors.ToArray();
            var items = interceptors.Length == 0 ? null : new Dictionary<string, object>();
            HttpMethod firstMethod = null;
            for (var attempt = 0; ; attempt++)
            {
                if (observation != null) observation.StatusCode = null;
                token.ThrowIfCancellationRequested();
                HttpRequestMessage message = null;
                Exception attemptError = null;
                try
                {
                    message = CreateMessage();
                    prepareMessage?.Invoke(message, attempt + 1, client);
                    var originalContent = message.Content;
                    var bodyReplaced = false;
                    bool shouldSend;
                    Exception beforeError = null;
                    try
                    {
                        shouldSend = SendBefore(message);
                    }
                    catch (Exception ex)
                    {
                        beforeError = ex;
                        throw;
                    }
                    finally
                    {
                        if (message.Content != originalContent)
                        {
                            bodyReplaced = true;
                            DisposePreservingException(originalContent, beforeError);
                        }
                    }
                    if (!shouldSend)
                    {
                        if (observation != null) observation.Rejected = true;
                        return default;
                    }
                    var context = interceptors.Length == 0 ? null : new HttpRequestInterceptorContext(message, attempt + 1, items);
                    var entered = 0;
                    try
                    {
                        foreach (var interceptor in interceptors)
                        {
                            var previousContent = message.Content;
                            Exception interceptorError = null;
                            try
                            {
                                await interceptor.BeforeSendAsync(context, token).ConfigureAwait(false);
                                entered++;
                            }
                            catch (Exception ex)
                            {
                                interceptorError = ex;
                                throw;
                            }
                            finally
                            {
                                if (!ReferenceEquals(message.Content, previousContent))
                                {
                                    bodyReplaced = true;
                                    DisposePreservingException(previousContent, interceptorError);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Exception = ex;
                        await RunAfterInterceptorsAsync(interceptors, entered, context, token).ConfigureAwait(false);
                        throw;
                    }
                    HttpResponseMessage response = null;
                    Exception sendError = null;
                    Exception afterError;
                    var attemptMethod = message.Method;
                    var attemptRetries = _retryOptions != null && _retryOptions.AllowedMethods.Contains(attemptMethod) ? retries : 0;
                    if (attemptRetries > 0 && _retryOptions.RequireIdempotencyKeyForUnsafeMethods &&
                        attemptMethod != HttpMethod.Get && attemptMethod != HttpMethod.Head && attemptMethod != HttpMethod.Options &&
                        (!message.Headers.TryGetValues("Idempotency-Key", out var idempotencyKeys) ||
                         !idempotencyKeys.Any(value => !string.IsNullOrWhiteSpace(value))))
                        throw new InvalidOperationException("Retrying an unsafe HTTP method requires a non-empty Idempotency-Key header.");
                    var watch = context == null ? null : System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        if (firstMethod == null)
                            firstMethod = attemptMethod;
                        else if (attemptMethod != firstMethod)
                            throw new NotSupportedException("OnSendBefore changed the HTTP method between retry attempts.");
                        if (attempt == 0 && attemptRetries > 0 && Files.Any(file => file.Stream != null && !file.Stream.CanSeek))
                            throw new NotSupportedException("Retrying a request with a non-seekable upload stream is not supported. Use a seekable stream or a file path.");
                        if (attemptRetries > 0 && bodyReplaced)
                            throw new NotSupportedException("The request body was replaced before sending; automatic retries require a replayable body.");
                        if (observation != null) observation.Attempts++;
                        response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                        if (observation != null) observation.StatusCode = response.StatusCode;
                        if (response.RequestMessage == null)
                            response.RequestMessage = message;
                    }
                    catch (Exception ex)
                    {
                        sendError = ex;
                    }
                    if (context != null)
                    {
                        watch.Stop();
                        context.Elapsed = watch.Elapsed;
                        context.Exception = sendError;
                        if (response != null)
                        {
                            context.StatusCode = response.StatusCode;
                            context.Headers = SnapshotHeaders(response.Headers);
                        }
                    }
                    afterError = context == null ? null : await RunAfterInterceptorsAsync(interceptors, entered, context, token).ConfigureAwait(false);
                    if (sendError != null)
                    {
                        DisposePreservingException(response, sendError);
                        if (afterError == null && sendError is HttpRequestException requestError &&
                            attempt < attemptRetries &&
                            !IsCertificateFailure(requestError) &&
                            await DelayBeforeRetryAsync(null, attempt, deadline, token, requestError, retryObserver, retryObservationErrorHandler).ConfigureAwait(false))
                            continue;
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(sendError).Throw();
                    }
                    if (afterError != null)
                    {
                        DisposePreservingException(response, afterError);
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(afterError).Throw();
                    }

                    Exception responseError = null;
                    try
                    {
                        if (attempt < attemptRetries && IsRetryable(response.StatusCode))
                        {
                            if (await DelayBeforeRetryAsync(response, attempt, deadline, token, null, retryObserver, retryObservationErrorHandler).ConfigureAwait(false))
                                continue;
                        }
                        var result = await process(response, token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        responseError = ex;
                        throw;
                    }
                    finally
                    {
                        DisposePreservingException(response, responseError);
                    }
                }
                catch (Exception ex)
                {
                    attemptError = ex;
                    throw;
                }
                finally { DisposePreservingException(message, attemptError); }
            }
        }
        catch (Exception ex)
        {
            executionError = ex;
            if (ex is OperationCanceledException && observation != null)
                observation.TimedOut = timeout?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested;
            throw;
        }
        finally
        {
            var cleanupError = DisposeAndCapture(timeout, null);
            if (_httpClient == null) cleanupError = DisposeAndCapture(client, cleanupError);
            cleanupError = DisposeUploadStreams(cleanupError);
            if (executionError == null && cleanupError != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }
    }

    /// <summary>
    /// 按进入顺序执行已进入的拦截器完成回调。
    /// </summary>
    private static async Task<Exception> RunAfterInterceptorsAsync(HttpRequestInterceptor[] interceptors, int entered,
        HttpRequestInterceptorContext context, CancellationToken token)
    {
        Exception firstError = null;
        for (var index = entered - 1; index >= 0; index--)
        {
            try { await interceptors[index].AfterSendAsync(context, token).ConfigureAwait(false); }
            catch (Exception ex) { firstError ??= ex; }
        }
        return firstError;
    }

    /// <summary>
    /// 释放资源并捕获释放时产生的异常。
    /// </summary>
    private static Exception DisposeAndCapture(IDisposable resource, Exception firstError)
    {
        try { resource?.Dispose(); }
        catch (Exception ex) { firstError ??= ex; }
        return firstError;
    }

    /// <summary>
    /// 释放请求拥有的上传流并返回首个清理异常。
    /// </summary>
    private Exception DisposeUploadStreams(Exception firstError)
    {
        foreach (var file in Files)
        {
            if (file.Stream != null && !file.LeaveOpen)
                firstError = DisposeAndCapture(file.Stream, firstError);
        }
        return firstError;
    }

    /// <summary>
    /// 释放请求拥有的上传流，并在存在主异常时保留该异常。
    /// </summary>
    /// <param name="primaryError">请求处理期间产生的主异常。</param>
    private void DisposeOwnedUploadStreams(Exception primaryError = null)
    {
        var cleanupError = DisposeUploadStreams(null);
        if (primaryError == null && cleanupError != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupError).Throw();
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="message">请求消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        var client = GetClient();
        client.CheckNull(nameof(client));
        return await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    #endregion

    #region GetClient(获取Http客户端)

    /// <summary>
    /// 获取Http客户端
    /// </summary>
    protected HttpClient GetClient()
    {
        if (_httpClient != null)
            return _httpClient;
        return string.IsNullOrWhiteSpace(_httpClientName)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(_httpClientName);
    }

    /// <summary>
    /// 创建Http客户端处理器
    /// </summary>
    protected HttpClientHandler CreateHttpClientHandler()
    {
        var handlerFactory = _httpClientFactory as IHttpMessageHandlerFactory;
        if (handlerFactory == null)
            return null;
        var handler = string.IsNullOrWhiteSpace(_httpClientName)
            ? handlerFactory.CreateHandler()
            : handlerFactory.CreateHandler(_httpClientName);
        while (handler is DelegatingHandler delegatingHandler)
            handler = delegatingHandler.InnerHandler;
        return handler as HttpClientHandler;
    }

    /// <summary>
    /// 初始化Http客户端处理器
    /// </summary>
    /// <param name="handler">Http客户端处理器</param>
    protected virtual void InitHttpClientHandler(HttpClientHandler handler)
    {
        if (handler == null)
            return;
        InitCertificate(handler);
        InitUseCookies(handler);
        IgnoreSsl(handler);
    }

    #endregion

    #region InitCertificate(初始化证书)

    /// <summary>
    /// 初始化证书
    /// </summary>
    /// <param name="handler">Http客户端处理器</param>
    protected virtual void InitCertificate(HttpClientHandler handler)
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
            return;
        var certificate = new X509Certificate2(CertificatePath, CertificatePassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
        handler.ClientCertificates.Clear();
        handler.ClientCertificates.Add(certificate);
    }

    #endregion

    #region InitUseCookies(初始化是否携带Cookie)

    /// <summary>
    /// 初始化是否携带Cookie
    /// </summary>
    /// <param name="handler">Http客户端处理器</param>
    protected virtual void InitUseCookies(HttpClientHandler handler)
    {
        if (handler.UseCookies != IsUseCookies)
            handler.UseCookies = IsUseCookies;
    }

    #endregion

    #region IgnoreSsl(忽略SSL证书错误)

    /// <summary>
    /// 忽略SSL证书错误
    /// </summary>
    /// <param name="handler">Http客户端处理器</param>
    protected virtual void IgnoreSsl(HttpClientHandler handler)
    {
        throw new NotSupportedException("Configure TLS validation when registering a named HttpClient.");
    }

    #endregion

    #region InitHttpClient(初始化Http客户端)

    /// <summary>
    /// 初始化Http客户端
    /// </summary>
    /// <param name="client">Http客户端</param>
    protected virtual void InitHttpClient(HttpClient client)
    {
        // Per-request settings are applied to the URI and cancellation token.
    }

    #endregion

    #region InitBaseAddress(初始化基地址)

    /// <summary>
    /// 初始化基地址
    /// </summary>
    /// <param name="client">Http客户端</param>
    protected virtual void InitBaseAddress(HttpClient client)
    {
        if (string.IsNullOrWhiteSpace(BaseAddressUri))
            return;
        client.BaseAddress = new Uri(BaseAddressUri);
    }

    #endregion

    #region InitTimeout(初始化超时间隔)

    /// <summary>
    /// 初始化超时间隔
    /// </summary>
    /// <param name="client">Http客户端</param>
    protected virtual void InitTimeout(HttpClient client)
    {
        if (HttpTimeout == null)
            return;
        client.Timeout = HttpTimeout.SafeValue();
    }

    #endregion

    #region SendAfterAsync(发送后操作)

    /// <summary>
    /// 发送后操作
    /// </summary>
    /// <param name="message">Http响应消息</param>
    protected virtual Task<TResult> SendAfterAsync(HttpResponseMessage message)
        => SendAfterAsync(message, _operationToken.Value);

    /// <summary>
    /// 按配置处理响应回调，并确保响应在回调完成后释放。
    /// </summary>
    /// <param name="message">收到的 HTTP 响应消息。</param>
    /// <param name="cancellationToken">当前请求操作的取消令牌。</param>
    /// <returns>响应回调生成的结果。</returns>
    private async Task<TResult> SendAfterAsync(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        if (SendAfterAction != null || SendAfterWithCancellationAction != null)
        {
            Exception processingError = null;
            try
            {
                var result = SendAfterWithCancellationAction != null
                    ? await SendAfterWithCancellationAction(message, cancellationToken)
                    : await SendAfterAction(message);
                if (result is Stream && message.Content != null &&
                    ReferenceEquals(result, await message.Content.ReadAsStreamAsync()))
                    throw new NotSupportedException("OnSendAfter cannot return a response-owned stream after the response is disposed.");
                return result;
            }
            catch (Exception ex) { processingError = ex; throw; }
            finally { CompleteHandlerPreservingException(message, null, processingError); }
        }
        string content = null;
        MemoryStream buffer = null;
        Exception responseError = null;
        try
        {
            buffer = await ReadResponseBufferAsync(message, cancellationToken);
            if (message.IsSuccessStatusCode && HasNoResponseBody(message))
            {
                content = string.Empty;
                SuccessAction?.Invoke(null);
                if (SuccessFunc != null) await SuccessFunc(null);
                return null;
            }
            content = buffer.Length != 0 ? await DecodeResponseTextAsync(message, buffer) : string.Empty;
            if (message.IsSuccessStatusCode) return await SuccessHandlerAsync(message, content);
            FailHandler(message, content);
            return null;
        }
        catch (Exception ex) { responseError = ex; throw; }
        finally
        {
            buffer?.Dispose();
            CompleteHandlerPreservingException(message, content, responseError);
        }
    }

    #endregion

    #region SuccessHandler(成功处理操作)

    /// <summary>
    /// 成功处理操作
    /// </summary>
    /// <param name="message">Http响应消息</param>
    /// <param name="content">内容</param>
    protected virtual async Task<TResult> SuccessHandlerAsync(HttpResponseMessage message, string content)
    {
        var result = ConvertTo(content, message.GetContentType());
        SuccessAction?.Invoke(result);
        if (SuccessFunc != null) await SuccessFunc(result);
        return result;
    }

    #endregion

    #region FailHandler(失败处理操作)

    /// <summary>
    /// 失败处理操作
    /// </summary>
    /// <param name="message">Http响应消息</param>
    /// <param name="content">内容</param>
    protected virtual void FailHandler(HttpResponseMessage message, object content) => FailAction?.Invoke(message, content);

    #endregion

    #region CompleteHandler(执行完成操作)

    /// <summary>
    /// 执行完成操作
    /// </summary>
    /// <param name="message">Http响应消息</param>
    /// <param name="content">内容</param>
    protected virtual void CompleteHandler(HttpResponseMessage message, object content) => CompleteAction?.Invoke(message, content);

    /// <summary>
    /// 执行完成回调，同时保留响应处理阶段已经发生的异常。
    /// </summary>
    /// <param name="message">当前响应消息。</param>
    /// <param name="content">已读取的响应内容。</param>
    /// <param name="processingError">响应处理阶段已发生的异常；无异常时为空。</param>
    private void CompleteHandlerPreservingException(HttpResponseMessage message, object content, Exception processingError)
    {
        try { CompleteHandler(message, content); }
        catch when (processingError != null) { }
    }

    #endregion

    #region GetStreamAsync(获取流)

    /// <summary>
    /// 获取流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<byte[]> GetStreamAsync(CancellationToken cancellationToken = default)
        => await GetByteResultAsync(cancellationToken, null, false).ConfigureAwait(false);

    /// <summary>
    /// 获取响应字节，并按调用层级决定是否发送完成观测。
    /// </summary>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <param name="observation">当前请求的观测状态。</param>
    /// <param name="nested">是否由另一请求操作嵌套调用。</param>
    /// <returns>响应正文的字节数组。</returns>
    private Task<byte[]> GetByteResultAsync(CancellationToken cancellationToken, ObservationState observation, bool nested)
    {
        return ExecuteAsync(async (response, token) =>
        {
            _operationToken.Value = token;
            try { return await GetStreamAsync(response); }
            finally { _operationToken.Value = default; }
        }, cancellationToken, observation, nested);
    }

    /// <summary>
    /// 发送后操作
    /// </summary>
    /// <param name="message">Http响应消息</param>
    protected virtual Task<byte[]> GetStreamAsync(HttpResponseMessage message)
        => ReadByteResultAsync(message, _operationToken.Value);

    /// <summary>
    /// 读取响应正文并应用响应大小限制及取消令牌。
    /// </summary>
    /// <param name="message">待读取的 HTTP 响应消息。</param>
    /// <param name="cancellationToken">正文读取的取消令牌。</param>
    /// <returns>响应正文的字节数组。</returns>
    private async Task<byte[]> ReadByteResultAsync(HttpResponseMessage message, CancellationToken cancellationToken)
    {
        byte[] content = null;
        Exception processingError = null;
        try
        {
            content = await ReadResponseBytesAsync(message, cancellationToken);
            if (message.IsSuccessStatusCode) return content;
            FailHandler(message, content);
            return null;
        }
        catch (Exception ex) { processingError = ex; throw; }
        finally { CompleteHandlerPreservingException(message, content, processingError); }
    }

    /// <summary>
    /// 将响应正文复制到目标流，并在复制过程中执行大小限制。
    /// </summary>
    /// <param name="response">待复制的 HTTP 响应。</param>
    /// <param name="destination">接收正文的目标流。</param>
    /// <param name="token">复制操作的取消令牌。</param>
    /// <returns>写入目标流的字节数。</returns>
    private async Task<long> CopyResponseAsync(HttpResponseMessage response, Stream destination, CancellationToken token)
    {
        if (HasNoResponseBody(response) || response.Content == null)
            return 0;
        var length = response.Content.Headers.ContentLength;
        if (_maxResponseBytes.HasValue && length.HasValue && length.Value > _maxResponseBytes.Value)
            throw new HttpResponseSizeLimitException(_maxResponseBytes.Value);
        var source = await response.Content.ReadAsStreamAsync();
        byte[] buffer = null;
        long copied = 0;
        Exception copyError = null;
        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(81920);
            while (true)
            {
                var count = await source.ReadAsync(buffer, 0, buffer.Length, token);
                if (count == 0) break;
                if (_maxResponseBytes.HasValue && copied + count > _maxResponseBytes.Value)
                    throw new HttpResponseSizeLimitException(_maxResponseBytes.Value);
                await destination.WriteAsync(buffer, 0, count, token);
                copied += count;
            }
        }
        catch (Exception ex) { copyError = ex; throw; }
        finally
        {
            if (buffer != null) ArrayPool<byte>.Shared.Return(buffer);
            DisposePreservingException(source, copyError);
        }
        return copied;
    }

    #endregion

    #region WriteAsync(写入文件)

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WriteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var bytes = await GetStreamAsync(cancellationToken);
        await FileHelper.WriteAsync(bytes, filePath, cancellationToken: cancellationToken);
    }

    #endregion
}
