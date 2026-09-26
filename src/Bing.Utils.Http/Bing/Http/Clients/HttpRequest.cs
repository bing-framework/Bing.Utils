using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Bing.Extensions;
using Bing.Helpers;
using Bing.IO;
using Bing.Http.Clients.Internal;
using Bing.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Bing.Http.Clients;

/// <summary>
/// Http请求
/// </summary>
/// <typeparam name="TResult">结果类型</typeparam>
public class HttpRequest<TResult> : IHttpRequest<TResult> where TResult : class
{
    #region 字段

    /// <summary>
    /// 在每次发送时创建客户端的工厂。
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 由调用方提供并管理生命周期的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 通过工厂创建客户端时使用的注册名称。
    /// </summary>
    private string _httpClientName;

    /// <summary>
    /// 创建请求消息时使用的 HTTP 方法。
    /// </summary>
    private readonly HttpMethod _httpMethod;

    /// <summary>
    /// 与基地址和查询参数合并前的请求地址。
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// 请求内容序列化和响应反序列化使用的自定义 JSON 配置。
    /// </summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// 指示是否跳过本次请求的服务器证书验证。
    /// </summary>
    private bool _ignoreSsl;

    /// <summary>
    /// 指示调用方是否显式设置自动 Cookie，用于校验客户端的请求隔离能力。
    /// </summary>
    private bool _useCookiesSpecified;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="HttpRequest{TResult}" /> 类的新实例。
    /// </summary>
    /// <param name="httpClientFactory">HTTP 客户端工厂；未提供客户端时必须指定。</param>
    /// <param name="httpClient">由调用方管理生命周期的客户端；为 null 时使用工厂。</param>
    /// <param name="httpMethod">HTTP 请求方法。</param>
    /// <param name="url">请求地址。</param>
    /// <exception cref="ArgumentNullException">客户端和工厂均为 null，或请求地址为空白。</exception>
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
    /// 获取基地址。
    /// </summary>
    protected string BaseAddressUri { get; private set; }

    /// <summary>
    /// 获取客户端证书路径。
    /// </summary>
    protected string CertificatePath { get; private set; }

    /// <summary>
    /// 获取客户端证书密码。
    /// </summary>
    protected string CertificatePassword { get; private set; }

    /// <summary>
    /// 获取请求内容类型。
    /// </summary>
    protected string HttpContentType { get; private set; }

    /// <summary>
    /// 获取请求字符编码。
    /// </summary>
    protected Encoding CharacterEncoding { get; private set; }

    /// <summary>
    /// 获取请求超时间隔。
    /// </summary>
    protected TimeSpan? HttpTimeout { get; private set; }

    /// <summary>
    /// 获取请求头参数集合。
    /// </summary>
    protected IDictionary<string, string> HeaderParams { get; }

    /// <summary>
    /// 获取显式 Cookie 集合。
    /// </summary>
    protected IDictionary<string, string> Cookies { get; }

    /// <summary>
    /// 获取查询参数集合。
    /// </summary>
    protected IDictionary<string, string> QueryParams { get; }

    /// <summary>
    /// 获取请求内容参数集合。
    /// </summary>
    protected IDictionary<string, object> Params { get; }

    /// <summary>
    /// 获取请求内容对象。
    /// </summary>
    protected object Param { get; private set; }

    /// <summary>
    /// 获取上传文件列表。
    /// </summary>
    protected List<FileData> Files { get; private set; }

    /// <summary>
    /// 获取是否自动携带 Cookie。
    /// </summary>
    protected bool IsUseCookies { get; private set; }

    /// <summary>
    /// 获取文件上传参数是否添加双引号。
    /// </summary>
    protected bool IsFileParameterQuotes { get; private set; }

    /// <summary>
    /// 获取发送前回调。
    /// </summary>
    protected Func<HttpRequestMessage, bool> SendBeforeAction { get; private set; }

    /// <summary>
    /// 获取异步响应处理回调。
    /// </summary>
    protected Func<HttpResponseMessage, Task<TResult>> SendAfterAction { get; private set; }

    /// <summary>
    /// 获取结果转换回调。
    /// </summary>
    protected Func<string, TResult> ConvertAction { get; private set; }

    /// <summary>
    /// 获取请求成功回调。
    /// </summary>
    protected Action<TResult> SuccessAction { get; private set; }

    /// <summary>
    /// 获取异步请求成功回调。
    /// </summary>
    protected Func<TResult, Task> SuccessFunc { get; private set; }

    /// <summary>
    /// 获取请求失败回调。
    /// </summary>
    protected Action<HttpResponseMessage, object> FailAction { get; private set; }

    /// <summary>
    /// 获取请求完成回调。
    /// </summary>
    protected Action<HttpResponseMessage, object> CompleteAction { get; private set; }

    #endregion

    #region HttpClientName(设置Http客户端名称)

    /// <inheritdoc />
    public IHttpRequest<TResult> HttpClientName(string name)
    {
        _httpClientName = name;
        return this;
    }

    #endregion

    #region BaseAddress(设置基地址)

    /// <inheritdoc />
    public IHttpRequest<TResult> BaseAddress(string baseAddress)
    {
        BaseAddressUri = baseAddress;
        return this;
    }

    #endregion

    #region ContentType(设置内容类型)

    /// <inheritdoc />
    public IHttpRequest<TResult> ContentType(HttpContentType contentType) => ContentType(contentType.Description());

    /// <inheritdoc />
    public IHttpRequest<TResult> ContentType(string contentType)
    {
        HttpContentType = contentType;
        return this;
    }

    #endregion

    #region Encoding(设置字符编码)

    /// <inheritdoc />
    public IHttpRequest<TResult> Encoding(string encoding) => Encoding(System.Text.Encoding.GetEncoding(encoding));

    /// <inheritdoc />
    public IHttpRequest<TResult> Encoding(Encoding encoding)
    {
        CharacterEncoding = encoding;
        return this;
    }

    #endregion

    #region BearerToken(设置访问令牌)

    /// <inheritdoc />
    public IHttpRequest<TResult> BearerToken(string token)
    {
        Header("Authorization", $"Bearer {token}");
        return this;
    }

    #endregion

    #region Certificate(设置证书)

    /// <inheritdoc />
    public IHttpRequest<TResult> Certificate(string path, string password)
    {
        CertificatePath = path;
        CertificatePassword = password;
        return this;
    }

    #endregion

    #region IgnoreSsl(忽略SSL证书)

    /// <inheritdoc />
    public IHttpRequest<TResult> IgnoreSsl()
    {
        _ignoreSsl = true;
        return this;
    }

    #endregion

    #region JsonSerializerOptions(设置Json序列化配置)

    /// <inheritdoc />
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
    /// <returns>显式配置或新建的默认 JSON 序列化配置。</returns>
    protected virtual JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (_jsonSerializerOptions != null)
            return _jsonSerializerOptions;
        return new JsonSerializerOptions
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

    /// <inheritdoc />
    public IHttpRequest<TResult> Timeout(int timeout) => Timeout(new TimeSpan(0, 0, timeout));

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">超时不是无限值，且不大于零或毫秒数超过 Int32.MaxValue。</exception>
    public IHttpRequest<TResult> Timeout(TimeSpan timeout)
    {
        if (timeout != System.Threading.Timeout.InfiniteTimeSpan &&
            (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        HttpTimeout = timeout;
        return this;
    }

    #endregion

    #region Header(设置请求头)

    /// <inheritdoc />
    public IHttpRequest<TResult> Header(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (HeaderParams.ContainsKey(key))
            HeaderParams.Remove(key);
        HeaderParams.Add(key, value);
        return this;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IHttpRequest<TResult> QueryString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (QueryParams.ContainsKey(key))
            QueryParams.Remove(key);
        QueryParams.Add(key, value);
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> QueryString(IDictionary<string, string> queryString)
    {
        if (queryString == null)
            return this;
        foreach (var param in queryString)
            QueryString(param.Key, param.Value);
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> QueryString(object queryString)
    {
        var dict = ToDictionary(queryString);
        foreach (var param in dict)
            QueryString(param.Key, param.Value.ToString());
        return this;
    }

    #endregion

    #region UseCookies(设置是否自动携带Cookie)

    /// <inheritdoc />
    public IHttpRequest<TResult> UseCookies(bool isUseCookies = true)
    {
        _useCookiesSpecified = true;
        IsUseCookies = isUseCookies;
        return this;
    }

    #endregion

    #region Cookie(设置Cookie)

    /// <inheritdoc />
    public IHttpRequest<TResult> Cookie(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return this;
        if (Cookies.ContainsKey(key))
            Cookies.Remove(key);
        Cookies.Add(key, value);
        return this;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IHttpRequest<TResult> Content(IDictionary<string, object> parameters)
    {
        if (parameters == null)
            return this;
        foreach (var param in parameters)
            Content(param.Key, param.Value);
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> Content(object value)
    {
        Param = value;
        return this;
    }

    #endregion

    #region JsonContent(添加Json参数)

    /// <inheritdoc />
    public IHttpRequest<TResult> JsonContent(object value)
    {
        ContentType(Http.HttpContentType.Json);
        return Content(value);
    }

    #endregion

    #region XmlContent(添加Xml参数)

    /// <inheritdoc />
    public IHttpRequest<TResult> XmlContent(string value)
    {
        ContentType(Http.HttpContentType.Xml);
        return Content(value);
    }

    #endregion

    #region FileContent(添加文件参数)

    /// <inheritdoc />
    /// <remarks>
    /// 流须在请求发送时保持可读；构建上传内容时读取并释放该流。
    /// </remarks>
    public IHttpRequest<TResult> FileContent(Stream stream, string fileName, string name = "file")
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        ContentType(Http.HttpContentType.FormData);
        if (Files.Any(t => t.Name == name))
            Files.RemoveAll(t => t.Name == name);
        Files.Add(new FileData(stream, fileName, name));
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> FileContent(string filePath, string name = "file")
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("上传文件路径不能为空。", nameof(filePath));
        ContentType(Http.HttpContentType.FormData);
        if (Files.Any(t => t.Name == name))
            Files.RemoveAll(t => t.Name == name);
        Files.Add(new FileData(filePath, name));
        return this;
    }

    #endregion

    #region FileParameterQuotes(文件上传参数是否添加双引号)

    /// <inheritdoc />
    public IHttpRequest<TResult> FileParameterQuotes(bool isQuote = true)
    {
        IsFileParameterQuotes = isQuote;
        return this;
    }

    #endregion

    #region OnSendBefore(发送前事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnSendBefore(Func<HttpRequestMessage, bool> action)
    {
        SendBeforeAction = action;
        return this;
    }

    #endregion

    #region OnSendAfter(发送后事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, Task<TResult>> action)
    {
        SendAfterAction = action;
        return this;
    }

    #endregion

    #region OnConvert(结果转换事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnConvert(Func<string, TResult> action)
    {
        ConvertAction = action;
        return this;
    }

    #endregion

    #region OnSuccess(请求成功事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnSuccess(Action<TResult> action)
    {
        SuccessAction = action;
        return this;
    }

    /// <inheritdoc />
    public IHttpRequest<TResult> OnSuccess(Func<TResult, Task> action)
    {
        SuccessFunc = action;
        return this;
    }

    #endregion

    #region OnFail(请求失败事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnFail(Action<HttpResponseMessage, object> action)
    {
        FailAction = action;
        return this;
    }

    #endregion

    #region OnComplete(请求完成事件)

    /// <inheritdoc />
    public IHttpRequest<TResult> OnComplete(Action<HttpResponseMessage, object> action)
    {
        CompleteAction = action;
        return this;
    }

    #endregion

    #region GetResultAsync(获取结果)

    /// <inheritdoc />
    /// <returns>异步操作的响应结果；发送前回调取消发送、响应失败或默认转换不支持内容类型时返回 null。</returns>
    /// <remarks>
    /// 若配置发送后回调，则由该回调决定返回结果。
    /// </remarks>
    public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var message = CreateMessage();
        if (SendBefore(message) == false)
            return default;
        using var response = await SendAsync(message, cancellationToken).ConfigureAwait(false);
        return await SendAfterAsync(response);
    }

    #endregion

    #region CreateMessage(创建请求消息)

    /// <summary>
    /// 创建请求消息
    /// </summary>
    /// <returns>包含地址、请求头和内容的请求消息，由调用方负责释放。</returns>
    protected virtual HttpRequestMessage CreateMessage()
    {
        var message = new HttpRequestMessage(_httpMethod, GetUrl(_url));
        try
        {
            AddCookies();
            AddHeaders(message);
            message.Content = CreateHttpContent();
            return message;
        }
        catch
        {
            message.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 获取服务地址
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <returns>合并查询参数后的请求地址。</returns>
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
    /// <returns>请求内容；内容类型不受支持或 JSON 内容为空时返回 null。</returns>
    protected virtual HttpContent CreateHttpContent()
    {
        var contentType = HttpContentType.SafeString().ToLower();
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
    /// <returns>URL 编码的表单内容。</returns>
    protected virtual HttpContent CreateFormContent() =>
        new FormUrlEncodedContent(GetParameters().ToDictionary(t => t.Key, t => t.Value.SafeString()));

    /// <summary>
    /// 获取参数
    /// </summary>
    /// <returns>合并后的参数字典，显式键值参数优先于对象属性。</returns>
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
    /// <returns>移除空值项后的对象属性字典。</returns>
    protected IDictionary<string, object> ToDictionary(object data)
    {
        var result = Conv.ToDictionary(data);
        return result.Where(t => t.Value != null).ToDictionary(t => t.Key, t => t.Value);
    }

    /// <summary>
    /// 创建Json内容
    /// </summary>
    /// <returns>JSON 请求内容；没有可发送的 JSON 文本时返回 null。</returns>
    protected virtual HttpContent CreateJsonContent()
    {
        var content = GetJsonContentValue();
        if (string.IsNullOrWhiteSpace(content))
            return null;
        return new StringContent(content, CharacterEncoding, "application/json");
    }

    /// <summary>
    /// 获取Json内容值
    /// </summary>
    /// <returns>序列化后的 JSON 文本；没有内容参数时返回 null。</returns>
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
    /// <returns>使用当前字符编码创建的 XML 请求内容。</returns>
    protected virtual HttpContent CreateXmlContent() => new StringContent(Param.SafeString(), CharacterEncoding, "text/xml");

    /// <summary>
    /// 创建文件上传内容
    /// </summary>
    /// <returns>包含表单参数和文件数据的多部分请求内容。</returns>
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
        catch
        {
            content.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 获取 multipart/form-data 分隔符
    /// </summary>
    /// <returns>本次多部分请求使用的随机分隔符。</returns>
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
            try { content.Add(item, GetFileParameter(parameter.Key)); }
            catch { item.Dispose(); throw; }
        }
    }

    /// <summary>
    /// 获取文件参数
    /// </summary>
    /// <param name="param">参数</param>
    /// <returns>根据配置保留原值或添加双引号后的参数文本。</returns>
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
                using var fileStream = file.Stream;
                if (fileStream.CanRead == false)
                    throw new InvalidOperationException("上传文件流不可读。流必须在请求发送时保持可读。");
                var bytes = FileHelper.ReadToBytes(fileStream);
                if (bytes == null)
                    throw new InvalidOperationException("无法读取上传文件流。");
                AddFileData(content, bytes, file.Name, file.FileName);
                continue;
            }
            if (string.IsNullOrWhiteSpace(file.FilePath))
                throw new ArgumentException("上传文件路径不能为空。", nameof(file.FilePath));
            if (File.Exists(file.FilePath) == false)
                throw new FileNotFoundException("上传文件不存在。", file.FilePath);
            var fileName = Path.GetFileName(file.FilePath);
            AddFileData(content, File.ReadAllBytes(file.FilePath), file.Name, fileName);
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
        try { content.Add(fileContent, GetFileParameter(name), GetFileParameter(fileName)); }
        catch { fileContent.Dispose(); throw; }
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
    /// <returns>允许发送时返回 true；回调取消发送时返回 false。</returns>
    protected virtual bool SendBefore(HttpRequestMessage message)
    {
        if (SendBeforeAction == null)
            return true;
        return SendBeforeAction(message);
    }

    #endregion

    #region SendAsync(发送请求)

    /// <summary>
    /// 异步发送 HTTP 请求。
    /// </summary>
    /// <param name="message">请求消息。</param>
    /// <param name="cancellationToken">用于取消发送的令牌。</param>
    /// <returns>异步操作返回的响应消息，响应体已缓冲，由调用方负责释放。</returns>
    /// <remarks>
    /// 工厂创建的客户端和请求隔离上下文由本次调用释放；注入的客户端仍由调用方管理。请求超时不修改客户端配置。
    /// </remarks>
    /// <exception cref="NotSupportedException">使用请求级 TLS、证书或显式自动 Cookie 配置，但客户端未注册请求隔离。</exception>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        var client = GetClient();
        client.CheckNull(nameof(client));
        // 工厂客户端由本次调用拥有；注入客户端始终由调用方管理。
        using var ownedClient = _httpClient == null ? client : null;
        InitHttpClient(client);
        if (message.RequestUri == null || !message.RequestUri.IsAbsoluteUri)
        {
            var baseAddress = string.IsNullOrWhiteSpace(BaseAddressUri) ? client.BaseAddress : new Uri(BaseAddressUri, UriKind.Absolute);
            if (baseAddress != null)
                message.RequestUri = message.RequestUri == null ? baseAddress : new Uri(baseAddress, message.RequestUri);
        }

        var registration = BingHttpClientBuilderExtensions.GetRegistration(client);
        if (registration == null && (_ignoreSsl || _useCookiesSpecified || !string.IsNullOrWhiteSpace(CertificatePath)))
            throw new NotSupportedException("请求级 TLS、证书和自动 Cookie 设置需要通过 AddHttpClient(...).UseBingRequestIsolation(...) 注册客户端。");

        using var isolation = registration == null ? null : new RequestIsolationContext(registration,
            IsUseCookies, _ignoreSsl, CertificatePath, CertificatePassword);
        isolation?.Attach(message);
        using var timeout = HttpTimeout.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
        if (timeout != null && HttpTimeout.Value != System.Threading.Timeout.InfiniteTimeSpan)
            timeout.CancelAfter(HttpTimeout.Value);
        // 使用 ResponseContentRead；缓冲响应体完成后才能释放本次传输资源。
        return await client.SendAsync(message, HttpCompletionOption.ResponseContentRead,
            timeout?.Token ?? cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region GetClient(获取Http客户端)

    /// <summary>
    /// 获取Http客户端
    /// </summary>
    /// <returns>调用方提供的客户端，或由工厂新建并由本次发送负责释放的客户端。</returns>
    protected HttpClient GetClient()
    {
        if (_httpClient != null)
            return _httpClient;
        return string.IsNullOrWhiteSpace(_httpClientName)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(_httpClientName);
    }

    /// <summary>
    /// 保留旧版处理器访问入口。
    /// </summary>
    /// <returns>始终返回 null。</returns>
    /// <remarks>
    /// 此兼容入口不再暴露工厂池中的可变主处理器。
    /// </remarks>
    protected HttpClientHandler CreateHttpClientHandler()
    {
        // 不再暴露工厂池中的可变主处理器。
        return null;
    }

    /// <summary>
    /// 校验旧版处理器初始化配置。
    /// </summary>
    /// <param name="handler">待校验的处理器；为 null 时直接返回。</param>
    /// <remarks>
    /// 兼容入口不修改处理器；请求级证书、自动 Cookie 和 TLS 验证配置须通过 UseBingRequestIsolation 生效。
    /// </remarks>
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
    /// 校验旧版客户端证书配置。
    /// </summary>
    /// <param name="handler">兼容签名保留的处理器参数。</param>
    /// <exception cref="NotSupportedException">已设置客户端证书路径，必须改用请求隔离配置。</exception>
    protected virtual void InitCertificate(HttpClientHandler handler)
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
            return;
        throw new NotSupportedException("请通过 UseBingRequestIsolation 配置请求级客户端证书。");
    }

    #endregion

    #region InitUseCookies(初始化是否携带Cookie)

    /// <summary>
    /// 校验旧版自动 Cookie 配置。
    /// </summary>
    /// <param name="handler">兼容签名保留的处理器参数。</param>
    /// <exception cref="NotSupportedException">已显式设置自动 Cookie，必须改用请求隔离配置。</exception>
    protected virtual void InitUseCookies(HttpClientHandler handler)
    {
        if (_useCookiesSpecified)
            throw new NotSupportedException("请通过 UseBingRequestIsolation 配置请求级自动 Cookie。");
    }

    #endregion

    #region IgnoreSsl(忽略SSL证书错误)

    /// <summary>
    /// 校验旧版 TLS 验证配置。
    /// </summary>
    /// <param name="handler">兼容签名保留的处理器参数。</param>
    /// <exception cref="NotSupportedException">已启用跳过服务器证书验证，必须改用请求隔离配置。</exception>
    protected virtual void IgnoreSsl(HttpClientHandler handler)
    {
        if (_ignoreSsl == false)
            return;
        throw new NotSupportedException("请通过 UseBingRequestIsolation 配置请求级 TLS 验证。");
    }

    #endregion

    #region InitHttpClient(初始化Http客户端)

    /// <summary>
    /// 初始化Http客户端
    /// </summary>
    /// <param name="client">Http客户端</param>
    protected virtual void InitHttpClient(HttpClient client)
    {
        InitBaseAddress(client);
        InitTimeout(client);
    }

    #endregion

    #region InitBaseAddress(初始化基地址)

    /// <summary>
    /// 保留基地址初始化扩展点。
    /// </summary>
    /// <param name="client">HTTP 客户端。</param>
    /// <remarks>
    /// 基类不修改客户端；发送时在请求消息上合并基地址。
    /// </remarks>
    protected virtual void InitBaseAddress(HttpClient client)
    {
        if (string.IsNullOrWhiteSpace(BaseAddressUri))
            return;
        // 地址在请求消息上合并，不能更改已启动的共享客户端。
    }

    #endregion

    #region InitTimeout(初始化超时间隔)

    /// <summary>
    /// 保留超时间隔初始化扩展点。
    /// </summary>
    /// <param name="client">HTTP 客户端。</param>
    /// <remarks>
    /// 基类不修改客户端；发送时使用请求独立的关联取消令牌。
    /// </remarks>
    protected virtual void InitTimeout(HttpClient client)
    {
        if (HttpTimeout == null)
            return;
        // 超时通过每次发送独立的关联取消令牌实现。
    }

    #endregion

    #region SendAfterAsync(发送后操作)

    /// <summary>
    /// 异步处理响应结果。
    /// </summary>
    /// <param name="message">Http响应消息</param>
    /// <returns>异步操作的响应结果；默认处理失败响应时返回 null，自定义回调可决定返回值。</returns>
    protected virtual async Task<TResult> SendAfterAsync(HttpResponseMessage message)
    {
        if (SendAfterAction != null)
            return await SendAfterAction(message);
        string content = null;
        try
        {
            content = await message.Content.ReadAsStringAsync();
            if (message.IsSuccessStatusCode)
                return await SuccessHandlerAsync(message, content);
            FailHandler(message, content);
            return null;
        }
        finally
        {
            CompleteHandler(message, content);
        }
    }

    #endregion

    #region SuccessHandler(成功处理操作)

    /// <summary>
    /// 异步处理成功响应。
    /// </summary>
    /// <param name="message">Http响应消息</param>
    /// <param name="content">内容</param>
    /// <returns>异步处理完成后的转换结果；内容转换无结果时为 null。</returns>
    protected virtual async Task<TResult> SuccessHandlerAsync(HttpResponseMessage message, string content)
    {
        var result = ConvertTo(content, message.GetContentType());
        SuccessAction?.Invoke(result);
        if (SuccessFunc != null)
            await SuccessFunc(result);
        return result;
    }

    #endregion

    #region ConvertTo(将内容转换为结果)

    /// <summary>
    /// 将内容转换为结果
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>转换后的响应结果；默认转换不支持内容类型或反序列化无结果时返回 null。</returns>
    protected virtual TResult ConvertTo(string content, string contentType)
    {
        if (ConvertAction != null)
            return ConvertAction(content);
        if (typeof(TResult) == typeof(string))
            return (TResult)(object)content;
        return contentType.SafeString().ToLower() == "application/json"
            ? Json.ToObject<TResult>(content, GetJsonSerializerOptions())
            : null;
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

    #endregion

    #region GetStreamAsync(获取流)

    /// <inheritdoc />
    /// <returns>异步操作读取的响应字节；发送前回调取消发送或响应失败时返回 null。</returns>
    public async Task<byte[]> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var message = CreateMessage();
        if (SendBefore(message) == false)
            return default;
        using var response = await SendAsync(message, cancellationToken).ConfigureAwait(false);
        return await GetStreamAsync(response);
    }

    /// <summary>
    /// 异步读取响应字节。
    /// </summary>
    /// <param name="message">HTTP 响应消息。</param>
    /// <returns>异步读取的响应字节；响应状态不成功时返回 null。</returns>
    /// <remarks>
    /// 失败响应触发失败回调；读取结束时触发完成回调。
    /// </remarks>
    protected virtual async Task<byte[]> GetStreamAsync(HttpResponseMessage message)
    {
        byte[] content = null;
        try
        {
            content = await message.Content.ReadAsByteArrayAsync();
            if (message.IsSuccessStatusCode)
                return content;
            FailHandler(message, content);
            return null;
        }
        finally
        {
            CompleteHandler(message, content);
        }
    }

    #endregion

    #region WriteAsync(写入文件)

    /// <inheritdoc />
    /// <remarks>
    /// 异步下载完成后通过同目录临时文件提交；下载失败或取消时不覆盖已有目标文件。
    /// </remarks>
    /// <exception cref="ArgumentException">目标文件路径为空白。</exception>
    /// <exception cref="HttpRequestException">下载未成功，未获得响应字节。</exception>
    public async Task WriteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("目标文件路径不能为空。", nameof(filePath));
        var bytes = await GetStreamAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (bytes == null)
            throw new HttpRequestException("下载未成功，目标文件未修改。");
        var destination = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(destination);
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".bing-http-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       81920, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            // 同目录提交；替换失败时不采用“删除旧文件再移动”的破坏性回退。
            if (File.Exists(destination))
                File.Replace(temporary, destination, null);
            else
                File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    #endregion
}
