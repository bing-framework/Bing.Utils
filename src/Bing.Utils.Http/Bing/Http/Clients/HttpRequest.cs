using System.Net.Http;
using System.Text;
using System.Text.Json;
using Bing.Extensions;

namespace Bing.Http.Clients;

///// <summary>
///// Http请求
///// </summary>
//public class HttpRequest : HttpRequestBase<IHttpRequest>, IHttpRequest
//{
//    /// <summary>
//    /// 执行成功的回调函数
//    /// </summary>
//    private Action<string> _successAction;

//    /// <summary>
//    /// 执行成功的回调函数
//    /// </summary>
//    private Action<string, HttpStatusCode> _successStatusCodeAction;

//    /// <summary>
//    /// 初始化一个<see cref="HttpRequest"/>类型的实例
//    /// </summary>
//    /// <param name="httpMethod">Http请求方法</param>
//    /// <param name="url">请求地址</param>
//    public HttpRequest(HttpMethod httpMethod, string url) : base(httpMethod, url)
//    {
//    }

//    /// <summary>
//    /// 请求成功回调函数
//    /// </summary>
//    /// <param name="action">执行成功的回调函数，参数为响应结果</param>
//    public IHttpRequest OnSuccess(Action<string> action)
//    {
//        _successAction = action;
//        return this;
//    }

//    /// <summary>
//    /// 请求成功回调函数
//    /// </summary>
//    /// <param name="action">执行成功的回调函数，第一个参数为响应结果，第二个参数为状态码</param>
//    public IHttpRequest OnSuccess(Action<string, HttpStatusCode> action)
//    {
//        _successStatusCodeAction = action;
//        return this;
//    }

//    /// <summary>
//    /// 成功处理操作
//    /// </summary>
//    /// <param name="result">结果</param>
//    /// <param name="statusCode">状态码</param>
//    /// <param name="contentType">内容类型</param>
//    protected override void SuccessHandler(string result, HttpStatusCode statusCode, string contentType)
//    {
//        _successAction?.Invoke(result);
//        _successStatusCodeAction?.Invoke(result, statusCode);
//    }

//    /// <summary>
//    /// 获取Json结果
//    /// </summary>
//    /// <typeparam name="TResult">返回结果类型</typeparam>
//    public async Task<TResult> ResultFromJsonAsync<TResult>() => JsonHelper.ToObject<TResult>(await ResultAsync());
//}

///// <summary>
///// Http请求
///// </summary>
///// <typeparam name="TResult">结果类型</typeparam>
//public class HttpRequest<TResult> : HttpRequestBase<IHttpRequest<TResult>>, IHttpRequest<TResult>
//    where TResult : class
//{
//    /// <summary>
//    /// 执行成功的回调函数
//    /// </summary>
//    private Action<TResult> _successAction;

//    /// <summary>
//    /// 执行成功的回调函数
//    /// </summary>
//    private Action<TResult, HttpStatusCode> _successStatusCodeAction;

//    /// <summary>
//    /// 执行成功的转换函数
//    /// </summary>
//    private Func<string, TResult> _convertAction;

//    /// <summary>
//    /// 初始化一个<see cref="HttpRequest{TResult}"/>类型的实例
//    /// </summary>
//    /// <param name="httpMethod">Http请求方法</param>
//    /// <param name="url">请求地址</param>
//    public HttpRequest(HttpMethod httpMethod, string url) : base(httpMethod, url)
//    {
//    }

//    /// <summary>
//    /// 请求成功回调函数
//    /// </summary>
//    /// <param name="action">执行成功的回调函数，参数为响应结果</param>
//    /// <param name="convertAction">将结果字符串转换为指定类型，当默认转换实现无法转换时使用</param>
//    public IHttpRequest<TResult> OnSuccess(Action<TResult> action, Func<string, TResult> convertAction = null)
//    {
//        _successAction = action;
//        _convertAction = convertAction;
//        return this;
//    }

//    /// <summary>
//    /// 请求成功回调函数
//    /// </summary>
//    /// <param name="action">执行成功的回调函数，第一个参数为响应结果，第二个参数为状态码</param>
//    /// <param name="convertAction">将结果字符串转换为指定类型，当默认转换实现无法转换时使用</param>
//    public IHttpRequest<TResult> OnSuccess(Action<TResult, HttpStatusCode> action, Func<string, TResult> convertAction = null)
//    {
//        _successStatusCodeAction = action;
//        _convertAction = convertAction;
//        return this;
//    }

//    /// <summary>
//    /// 成功处理操作
//    /// </summary>
//    /// <param name="result">结果</param>
//    /// <param name="statusCode">状态码</param>
//    /// <param name="contentType">内容类型</param>
//    protected override void SuccessHandler(string result, HttpStatusCode statusCode, string contentType)
//    {
//        var successResult = ConvertTo(result, contentType);
//        _successAction?.Invoke(successResult);
//        _successStatusCodeAction?.Invoke(successResult, statusCode);
//    }

//    /// <summary>
//    /// 将结果字符串转换为指定类型
//    /// </summary>
//    /// <param name="result">结果</param>
//    /// <param name="contentType">内容类型</param>
//    private TResult ConvertTo(string result, string contentType)
//    {
//        if (typeof(TResult) == typeof(string))
//            return Conv.To<TResult>(result);
//        if (_convertAction != null)
//            return _convertAction(result);
//        if (contentType.SafeString().ToLower() == "application/json")
//            return JsonHelper.ToObject<TResult>(result);
//        return null;
//    }

//    /// <summary>
//    /// 获取Json结果
//    /// </summary>
//    public async Task<TResult> ResultFromJsonAsync() => JsonHelper.ToObject<TResult>(await ResultAsync());
//}

/// <summary>
/// Http请求
/// </summary>
/// <typeparam name="TResult">结果类型</typeparam>
public class HttpRequest<TResult> : IHttpRequest<TResult> where TResult : class
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

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="HttpRequest{TResult}"/>类型的实例
    /// </summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="httpClient"></param>
    /// <param name="httpMethod"></param>
    /// <param name="url"></param>
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
        HeaderParams = new Dictionary<string, string>();
        QueryParams = new Dictionary<string, string>();
        Params = new Dictionary<string, object>();
        Cookies = new Dictionary<string, string>();
        HttpContentType = Http.HttpContentType.Json.Description();
        CharacterEncoding = System.Text.Encoding.UTF8;
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
    /// 是否自动携带cookie
    /// </summary>
    protected bool? IsUseCookies { get; private set; }

    /// <summary>
    /// 发送前操作
    /// </summary>
    protected Func<HttpRequestMessage, bool> SendBeforeAction { get; private set; }

    /// <summary>
    /// 发送后操作
    /// </summary>
    protected Func<HttpResponseMessage, Task<TResult>> SendAfterAction { get; private set; }

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

    /// <summary>
    /// 设置外部配置的HttpClient实例名称
    /// </summary>
    /// <param name="name">HttpClient名称</param>
    public IHttpRequest<TResult> HttpClientName(string name)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置基地址
    /// </summary>
    /// <param name="baseAddress">基地址</param>
    public IHttpRequest<TResult> BaseAddress(string baseAddress)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码，范例：gb2312</param>
    public IHttpRequest<TResult> Encoding(string encoding)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置字符编码
    /// </summary>
    /// <param name="encoding">字符编码</param>
    public IHttpRequest<TResult> Encoding(Encoding encoding)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    public IHttpRequest<TResult> BearerToken(string token)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    public IHttpRequest<TResult> ContentType(HttpContentType contentType)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    public IHttpRequest<TResult> ContentType(string contentType)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置证书
    /// </summary>
    /// <param name="path">证书路径</param>
    /// <param name="password">证书密码</param>
    public IHttpRequest<TResult> Certificate(string path, string password)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置Json序列化配置
    /// </summary>
    /// <param name="options">Json序列化配置</param>
    public IHttpRequest<TResult> JsonSerializerOptions(JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Header(string key, string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置请求头
    /// </summary>
    /// <param name="headers">请求头键值对集合</param>
    public IHttpRequest<TResult> Header(IDictionary<string, string> headers)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> QueryString(string key, string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串键值对集合</param>
    public IHttpRequest<TResult> QueryString(IDictionary<string, string> queryString)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置查询字符串。即url中?后面的参数
    /// </summary>
    /// <param name="queryString">查询字符串对象</param>
    public IHttpRequest<TResult> QueryString(object queryString)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置是否自动携带Cookie
    /// </summary>
    /// <param name="isUseCookies">是否自动携带Cookie</param>
    public IHttpRequest<TResult> UseCookies(bool isUseCookies = true)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置Cookie
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Cookie(string key, string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置Cookie集合
    /// </summary>
    /// <param name="cookies">Cookie集合</param>
    public IHttpRequest<TResult> Cookie(IDictionary<string, string> cookies)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Content(string key, object value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="parameters">参数字典</param>
    public IHttpRequest<TResult> Content(IDictionary<string, object> parameters)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 添加参数。作为请求内容发送
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> Content(object value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 添加内容类型为 application/json 的参数
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> JsonContent(object value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 添加内容类型为 text/xml 的参数
    /// </summary>
    /// <param name="value">值</param>
    public IHttpRequest<TResult> XmlContent(string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 发送前事件
    /// </summary>
    /// <param name="action">发送前操作，返回false取消发送</param>
    public IHttpRequest<TResult> OnSendBefore(Func<HttpRequestMessage, bool> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 发送后事件
    /// </summary>
    /// <param name="action">发送后操作，自定义解析返回值</param>
    public IHttpRequest<TResult> OnSendAfter(Func<HttpResponseMessage, Task<TResult>> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 结果转换事件
    /// </summary>
    /// <param name="action">结果转换操作，参数为响应内容</param>
    public IHttpRequest<TResult> OnConvert(Func<string, TResult> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    public IHttpRequest<TResult> OnSuccess(Action<TResult> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 请求成功事件
    /// </summary>
    /// <param name="action">执行成功操作，参数为响应结果</param>
    public IHttpRequest<TResult> OnSuccess(Action<TResult, Task> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 请求失败事件
    /// </summary>
    /// <param name="action">执行失败操作，参数为响应消息和响应内容</param>
    public IHttpRequest<TResult> OnFail(Action<HttpResponseMessage, object> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 请求完成事件，不论成功失败都会执行
    /// </summary>
    /// <param name="action">执行完成操作，参数为响应消息和响应内容</param>
    public IHttpRequest<TResult> OnComplete(Action<HttpResponseMessage, object> action)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取结果
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<TResult> GetResultAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取流
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<byte[]> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WriteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}