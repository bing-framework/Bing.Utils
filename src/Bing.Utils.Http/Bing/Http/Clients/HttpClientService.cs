using System.Net.Http;
using Bing.Extensions;

namespace Bing.Http.Clients;

/// <summary>
/// Http客户端服务
/// </summary>
public class HttpClientService : IHttpClient
{
    #region 字段

    /// <summary>
    /// Http客户端工厂
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Http客户端
    /// </summary>
    private HttpClient _httpClient;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="HttpClientService"/>类型的实例
    /// </summary>
    /// <param name="httpClientFactory">Http客户端工厂</param>
    public HttpClientService(IHttpClientFactory httpClientFactory = null)
    {
        _httpClientFactory = httpClientFactory;
    }

    #endregion

    #region SetHttpClient(设置Http客户端)

    /// <summary>
    /// 设置Http客户端
    /// </summary>
    /// <param name="client">Http客户端</param>
    public HttpClientService SetHttpClient(HttpClient client)
    {
        _httpClient = client;
        return this;
    }

    #endregion

    #region Get

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <param name="url">服务地址</param>
    public IHttpRequest<string> Get(string url) => Get<string>(url);

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="queryString">查询字符串对象</param>
    public IHttpRequest<string> Get(string url, object queryString) => Get<string>(url, queryString);

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    public IHttpRequest<TResult> Get<TResult>(string url) where TResult : class => Get<TResult>(url, null);

    /// <summary>
    /// 发送Get请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="queryString">查询字符串对象</param>
    public IHttpRequest<TResult> Get<TResult>(string url, object queryString) where TResult : class
    {
        var result = new HttpRequest<TResult>(_httpClientFactory, _httpClient, HttpMethod.Get, url);
        if (string.IsNullOrWhiteSpace(queryString.SafeString()))
            return result;
        return result.QueryString(queryString);
    }

    #endregion

    #region Post

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <param name="url">服务地址</param>
    public IHttpRequest<string> Post(string url) => Post<string>(url);

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    public IHttpRequest<string> Post(string url, object content) => Post<string>(url, content);

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    public IHttpRequest<TResult> Post<TResult>(string url) where TResult : class => Post<TResult>(url, null);

    /// <summary>
    /// 发送Post请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    public IHttpRequest<TResult> Post<TResult>(string url, object content) where TResult : class
    {
        var result = new HttpRequest<TResult>(_httpClientFactory, _httpClient, HttpMethod.Post, url);
        if (content == null)
            return result;
        return result.Content(content);
    }

    #endregion

    #region Put

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <param name="url">服务地址</param>
    public IHttpRequest<string> Put(string url) => Put<string>(url);

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    public IHttpRequest<string> Put(string url, object content) => Put<string>(url, content);

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    public IHttpRequest<TResult> Put<TResult>(string url) where TResult : class => Put<TResult>(url, null);

    /// <summary>
    /// 发送Put请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    /// <param name="content">内容参数</param>
    public IHttpRequest<TResult> Put<TResult>(string url, object content) where TResult : class
    {
        var result = new HttpRequest<TResult>(_httpClientFactory, _httpClient, HttpMethod.Put, url);
        if (content == null)
            return result;
        return result.Content(content);
    }

    #endregion

    #region Delete

    /// <summary>
    /// 发送Delete请求
    /// </summary>
    /// <param name="url">服务地址</param>
    public IHttpRequest<string> Delete(string url) => Delete<string>(url);

    /// <summary>
    /// 发送Delete请求
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="url">服务地址</param>
    public IHttpRequest<TResult> Delete<TResult>(string url) where TResult : class => new HttpRequest<TResult>(_httpClientFactory, _httpClient, HttpMethod.Delete, url);

    #endregion
}