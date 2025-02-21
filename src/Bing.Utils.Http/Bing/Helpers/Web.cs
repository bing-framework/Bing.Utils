﻿using System.Net;
using System.Text;
using System.Web;
using Bing.Extensions;
using Bing.IO;
using Microsoft.AspNetCore.Http;
#if !NETSTANDARD2_1
using Microsoft.AspNetCore.Http.Extensions;
#endif
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Bing.Helpers;

/// <summary>
/// Web操作
/// </summary>
public static class Web
{
    #region 属性

    #region HttpContextAccessor(Http上下文访问器)

    /// <summary>
    /// Http上下文访问器
    /// </summary>
    public static IHttpContextAccessor HttpContextAccessor { get; set; }

    #endregion

    #region HttpContext(当前Http上下文)

    /// <summary>
    /// 当前Http上下文
    /// </summary>
    public static HttpContext HttpContext => HttpContextAccessor?.HttpContext;

    #endregion

    #region Environment(宿主环境)

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// 宿主环境
        /// </summary>
        public static Microsoft.AspNetCore.Hosting.IWebHostEnvironment Environment { get; set; }
#elif NETSTANDARD2_0
    /// <summary>
    /// 宿主环境
    /// </summary>
    public static Microsoft.AspNetCore.Hosting.IHostingEnvironment Environment { get; set; }
#endif
    #endregion

    #region Request(当前Http请求)

    /// <summary>
    /// 当前Http请求
    /// </summary>
    public static HttpRequest Request => HttpContext?.Request;

    #endregion

    #region Response(当前Http响应)

    /// <summary>
    /// 当前Http响应
    /// </summary>
    public static HttpResponse Response => HttpContext?.Response;

    #endregion

    #region LocalIpAddress(本地IP)

    /// <summary>
    /// 本地IP
    /// </summary>
    public static string LocalIpAddress
    {
        get
        {
            try
            {
                var ipAddress = HttpContext.Connection.LocalIpAddress;
                return IPAddress.IsLoopback(ipAddress)
                    ? IPAddress.Loopback.ToString()
                    : ipAddress.MapToIPv4().ToString();
            }
            catch
            {
                return IPAddress.Loopback.ToString();
            }
        }
    }

    #endregion

    #region RequestType(请求类型)

    /// <summary>
    /// 请求类型
    /// </summary>
    public static string RequestType => HttpContext?.Request?.Method;

    #endregion

    #region Form(表单)

    /// <summary>
    /// Form表单
    /// </summary>
    public static IFormCollection Form => HttpContext?.Request?.Form;

    #endregion

    #region AccessToken(访问令牌)

    /// <summary>
    /// 访问令牌
    /// </summary>
    public static string AccessToken
    {
        get
        {
            var authorization = Request?.Headers["Authorization"].SafeString();
            if (string.IsNullOrWhiteSpace(authorization))
                return null;
            var list = authorization.Split(' ');
            if (list.Length == 2)
                return list[1];
            return null;
        }
    }

    #endregion

    #region Body(请求正文)

    /// <summary>
    /// 请求正文
    /// </summary>
    public static string Body
    {
        get
        {
            Request.EnableBuffering();
            return FileHelper.ToString(Request.Body, isCloseStream: false);
        }
    }

    #endregion

    #region Url(请求地址)

#if NETSTANDARD2_1
    /// <summary>
    /// 请求地址
    /// </summary>
    public static string Url => throw new NotSupportedException($"{nameof(Url)} 不支持在 NETSTANDARD2_1");
#else
    /// <summary>
    /// 请求地址
    /// </summary>
    public static string Url => Request?.GetDisplayUrl();
#endif
    #endregion

    #region Host(主机)

    /// <summary>
    /// 主机
    /// </summary>
    public static string Host => HttpContext == null ? Dns.GetHostName() : GetClientHostName();

    /// <summary>
    /// 获取Web客户端主机名
    /// </summary>
    private static string GetClientHostName()
    {
        var address = GetRemoteAddress();
        if (string.IsNullOrWhiteSpace(address))
            return Dns.GetHostName();
        var result = Dns.GetHostEntry(IPAddress.Parse(address)).HostName;
        if (result == "localhost.localdomain")
            result = Dns.GetHostName();
        return result;
    }

    /// <summary>
    /// 获取远程地址
    /// </summary>
    private static string GetRemoteAddress() =>
        HttpContext?.Request?.Headers["HTTP_X_FORWARDED_FOR"] ??
        HttpContext?.Request?.Headers["REMOTE_ADDR"];

    #endregion

    #region Browser(浏览器)

    /// <summary>
    /// 浏览器
    /// </summary>
    public static string Browser => HttpContext?.Request?.Headers["User-Agent"];

    #endregion

    #region RootPath(根路径)
#if !NETSTANDARD2_1

    /// <summary>
    /// 根路径
    /// </summary>
    public static string RootPath => Environment?.ContentRootPath;
#endif
    #endregion

    #region WebRootPath(Web根路径)

#if !NETSTANDARD2_1
    /// <summary>
    /// Web根路径，即wwwroot
    /// </summary>
    public static string WebRootPath => Environment?.WebRootPath;
#endif

    #endregion

    #region ContentType(内容类型)

    /// <summary>
    /// 内容类型
    /// </summary>
    public static string ContentType => HttpContext?.Request?.ContentType;

    #endregion

    #region QueryString(参数)

    /// <summary>
    /// 参数
    /// </summary>
    public static string QueryString => HttpContext?.Request?.QueryString.ToString();

    #endregion

    #region IsLocal(是否本地请求)

    /// <summary>
    /// 是否本地请求
    /// </summary>
    public static bool IsLocal
    {
        get
        {
            var connection = HttpContext?.Request?.HttpContext?.Connection;
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (connection.RemoteIpAddress.IsSet())
                return connection.LocalIpAddress.IsSet()
                    ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                    : IPAddress.IsLoopback(connection.RemoteIpAddress);
            return true;
        }
    }

    /// <summary>
    /// 空IP地址
    /// </summary>
    private const string NullIpAddress = "::1";

    /// <summary>
    /// 是否已设置IP地址
    /// </summary>
    /// <param name="address">IP地址</param>
    private static bool IsSet(this IPAddress address) => address != null && address.ToString() != NullIpAddress;

    #endregion

    #endregion

    #region 构造函数

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static Web() => ServicePointManager.DefaultConnectionLimit = 200;

    #endregion

    #region GetFiles(获取客户端文件集合)

    /// <summary>
    /// 获取客户端文件集合
    /// </summary>
    public static List<IFormFile> GetFiles()
    {
        var files = HttpContext?.Request?.Form?.Files;
        if (files == null || files.Count == 0)
            return new List<IFormFile>();

        var result = new List<IFormFile>(files.Count);
        foreach (var file in files)
        {
            if (file?.Length > 0)
                result.Add(file);
        }
        return result;
    }

    #endregion

    #region GetFile(获取客户端文件)

    /// <summary>
    /// 获取客户端文件
    /// </summary>
    /// <returns>第一个有效的客户端文件，如果没有则返回 null。</returns>
    public static IFormFile GetFile()
    {
        var files = GetFiles();
        return files.FirstOrDefault();
    }

    #endregion

    #region GetParam(获取请求参数)

    /// <summary>
    /// 获取请求参数，搜索路径：查询参数->表单参数->请求头
    /// </summary>
    /// <param name="name">参数名</param>
    public static string GetParam(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Request == null)
            return string.Empty;

        string result = Request.Query[name];
        if (string.IsNullOrWhiteSpace(result) == false)
            return result;
        result = Request.Form[name];
        if (string.IsNullOrWhiteSpace(result) == false)
            return result;
        return Request.Headers[name];
    }

    #endregion

    #region UrlEncode(Url编码)

    /// <summary>
    /// Url编码
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="isUpper">编码字符是否转成大写，范例："http://"转成"http%3A%2F%2F"</param>
    public static string UrlEncode(string url, bool isUpper = false) => UrlEncode(url, Encoding.UTF8, isUpper);

    /// <summary>
    /// Url编码
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="isUpper">编码字符是否转成大写，范例："http://"转成"http%3A%2F%2F"</param>
    public static string UrlEncode(string url, string encoding, bool isUpper = false)
    {
        encoding = string.IsNullOrWhiteSpace(encoding) ? "UTF-8" : encoding;
        return UrlEncode(url, Encoding.GetEncoding(encoding), isUpper);
    }

    /// <summary>
    /// Url编码
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="isUpper">编码字符是否转成大写，范例："http://"转成"http%3A%2F%2F"</param>
    public static string UrlEncode(string url, Encoding encoding, bool isUpper = false)
    {
        var result = HttpUtility.UrlEncode(url, encoding);
        return isUpper == false ? result : GetUpperEncode(result);
    }

    /// <summary>
    /// 获取大写编码字符串
    /// </summary>
    /// <param name="encode">编码字符串</param>
    private static string GetUpperEncode(string encode)
    {
        var result = new StringBuilder();
        for (var i = 0; i < encode.Length; i++)
        {
            if (encode[i] == '%' && i + 2 < encode.Length)
            {
                result.Append('%');
                result.Append(char.ToUpper(encode[i + 1]));
                result.Append(char.ToUpper(encode[i + 2]));
                i += 2;
            }
            else
            {
                result.Append(encode[i]);
            }
        }
        return result.ToString();
    }

    #endregion

    #region UrlDecode(Url解码)

    /// <summary>
    /// Url解码
    /// </summary>
    /// <param name="url">url</param>
    public static string UrlDecode(string url) => HttpUtility.UrlDecode(url);

    /// <summary>
    /// Url解码
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="encoding">字符编码</param>
    public static string UrlDecode(string url, Encoding encoding) => string.IsNullOrEmpty(url) ? string.Empty : HttpUtility.UrlDecode(url, encoding);

    #endregion

    #region Redirect(跳转到指定链接)

    /// <summary>
    /// 跳转到指定链接
    /// </summary>
    /// <param name="url">链接</param>
    public static void Redirect(string url) => Response?.Redirect(url);

    #endregion

    #region Write(输出内容)

    /// <summary>
    /// 输出内容
    /// </summary>
    /// <param name="text">内容</param>
    public static void Write(string text)
    {
        Response.ContentType = "text/plain;charset=utf-8";
        Task.Run(async () => { await Response.WriteAsync(text); }).GetAwaiter().GetResult();
    }

    #endregion

    #region Write(输出文件)

    /// <summary>
    /// 输出文件
    /// </summary>
    /// <param name="stream">文件流</param>
    public static void Write(FileStream stream)
    {
        long size = stream.Length;
        byte[] buffer = new byte[size];
        stream.Read(buffer, 0, (int)size);
        stream.Dispose();
        File.Delete(stream.Name);

        Response.ContentType = "application/octet-stream";
        Response.Headers.Add("Content-Disposition", "attachment;filename=" + WebUtility.UrlEncode(Path.GetFileName(stream.Name)));
        Response.Headers.Add("Content-Length", size.ToString());

        Task.Run(async () => { await Response.Body.WriteAsync(buffer, 0, (int)size); }).GetAwaiter().GetResult();
        Response.Body.Close();
    }

    #endregion

    #region GetBodyAsync(获取请求正文)

    /// <summary>
    /// 获取请求正文
    /// </summary>
    public static async Task<string> GetBodyAsync()
    {
        Request.EnableBuffering();
        return await FileHelper.ToStringAsync(Request.Body, isCloseStream: false);
    }

    #endregion

    #region DownloadAsync(下载)

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    public static Task DownloadFileAsync(string filePath, string fileName) =>
        DownloadFileAsync(filePath, fileName, Encoding.UTF8);

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    /// <param name="encoding">字符编码</param>
    public static async Task DownloadFileAsync(string filePath, string fileName, Encoding encoding)
    {
        var bytes = await FileHelper.ReadToBytesAsync(filePath);
        await DownloadAsync(bytes, fileName, encoding);
    }

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    public static Task DownloadAsync(Stream stream, string fileName) =>
        DownloadAsync(stream, fileName, Encoding.UTF8);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="stream">流</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    /// <param name="encoding">字符编码</param>
    public static async Task DownloadAsync(Stream stream, string fileName, Encoding encoding)
    {
        var bytes = await FileHelper.ToBytesAsync(stream);
        await DownloadAsync(bytes, fileName, encoding, Response);
    }

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="bytes">字节流</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    public static Task DownloadAsync(byte[] bytes, string fileName) =>
        DownloadAsync(bytes, fileName, Encoding.UTF8, Response);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="bytes">字节流</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    /// <param name="encoding">字符编码</param>
    public static Task DownloadAsync(byte[] bytes, string fileName, Encoding encoding) =>
        DownloadAsync(bytes, fileName, encoding, Response);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="bytes">字节流</param>
    /// <param name="fileName">文件名。包含扩展名</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="response">HTTP 响应</param>
    /// <exception cref="ArgumentException">当文件字节数组为空或文件名为空时抛出</exception>
    /// <exception cref="ArgumentNullException">当字符编码或HTTP响应为空时抛出</exception>
    public static async Task DownloadAsync(byte[] bytes, string fileName, Encoding encoding, HttpResponse response)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("文件字节数组不能为空", nameof(bytes));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        fileName = UrlEncode(fileName.Replace(" ", ""), encoding);
        response.ContentType = "application/octet-stream";
        response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
        response.Headers["Content-Length"] = bytes.Length.ToString();
        await response.Body.WriteAsync(bytes, 0, bytes.Length);
    }

    #endregion

    #region GetCookie(获取Cookie值)

    /// <summary>
    /// 获取Cookie值
    /// </summary>
    /// <param name="key">cookie键名</param>
    public static string GetCookie(string key) => Request?.Cookies[key];

    #endregion

    #region SetCookie(设置Cookie值)

    /// <summary>
    /// 设置Cookie值。未设置过期时间，则写的是浏览器进程Cookie，一旦浏览器（是浏览器，非标签页）关闭，则Cookie自动失效
    /// </summary>
    /// <param name="key">cookie键名</param>
    /// <param name="value">值</param>
    public static void SetCookie(string key, string value) => Response?.Cookies.Append(key, value);

    /// <summary>
    /// 设置Cookie值。
    /// </summary>
    /// <param name="key">cookie键名</param>
    /// <param name="value">值</param>
    /// <param name="options">Cookie配置</param>
    public static void SetCookie(string key, string value, CookieOptions options) => Response?.Cookies.Append(key, value, options);

    #endregion

    #region RemoveCookie(移除Cookie)

    /// <summary>
    /// 移除Cookie
    /// </summary>
    /// <param name="key">cookie键名</param>
    public static void RemoveCookie(string key) => Response?.Cookies.Delete(key);

    /// <summary>
    /// 移除Cookie
    /// </summary>
    /// <param name="key">cookie键名</param>
    /// <param name="options">Cookie配置</param>
    public static void RemoveCookie(string key, CookieOptions options) => Response?.Cookies.Delete(key, options);

    #endregion
}