using System.Net;
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
    #region 常量

    /// <summary>
    /// 默认连接限制
    /// </summary>
    private const int DefaultConnectionLimit = 200;

    /// <summary>
    /// 默认字符编码名称
    /// </summary>
    private const string DefaultEncodingName = "UTF-8";

    /// <summary>
    /// 授权请求头名称
    /// </summary>
    private const string AuthorizationHeaderName = "Authorization";

    /// <summary>
    /// Bearer Token 前缀
    /// </summary>
    private const string BearerTokenPrefix = "Bearer ";

    /// <summary>
    /// 本地域名标识
    /// </summary>
    private const string LocalDomainIdentifier = "localhost.localdomain";

    /// <summary>
    /// 空 IP 地址标识
    /// </summary>
    private const string NullIpAddress = "::1";

    /// <summary>
    /// 纯文本内容类型
    /// </summary>
    private const string PlainTextContentType = "text/plain;charset=utf-8";

    /// <summary>
    /// 二进制流内容类型
    /// </summary>
    private const string OctetStreamContentType = "application/octet-stream";

    #endregion

    #region 属性

    #region HttpContextAccessor(Http上下文访问器)

    /// <summary>
    /// HTTP 上下文访问器
    /// </summary>
    public static IHttpContextAccessor HttpContextAccessor { get; set; }

    #endregion

    #region HttpContext(当前Http上下文)

    /// <summary>
    /// 当前 HTTP 上下文
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
    /// 当前 HTTP 请求
    /// </summary>
    public static HttpRequest Request => HttpContext?.Request;

    #endregion

    #region Response(当前Http响应)

    /// <summary>
    /// 当前 HTTP 响应
    /// </summary>
    public static HttpResponse Response => HttpContext?.Response;

    #endregion

    #region LocalIpAddress(本地IP)

    /// <summary>
    /// 本地 IP 地址
    /// </summary>
    public static string LocalIpAddress
    {
        get
        {
            try
            {
                var ipAddress = HttpContext?.Connection?.LocalIpAddress;
                if (ipAddress == null)
                    return IPAddress.Loopback.ToString();
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
    /// HTTP 请求方法
    /// </summary>
    public static string RequestType => HttpContext?.Request?.Method;

    #endregion

    #region Form(表单)

    /// <summary>
    /// 表单数据集合
    /// </summary>
    public static IFormCollection Form => HttpContext?.Request?.Form;

    #endregion

    #region AccessToken(访问令牌)

    /// <summary>
    /// 访问令牌
    /// </summary>
    /// <returns>从 Authorization 请求头中提取的 Bearer Token，如果不存在则返回 null</returns>
    public static string AccessToken
    {
        get
        {
            var authorization = Request?.Headers[AuthorizationHeaderName].SafeString();
            if (string.IsNullOrWhiteSpace(authorization))
                return null;

            if (authorization.StartsWith(BearerTokenPrefix, StringComparison.OrdinalIgnoreCase))
                return authorization.Substring(BearerTokenPrefix.Length);

            var parts = authorization.Split(' ');
            return parts.Length == 2 ? parts[1] : null;
        }
    }

    #endregion

    #region Body(请求正文)

    /// <summary>
    /// 请求正文内容
    /// </summary>
    /// <returns>请求正文的字符串表示</returns>
    /// <exception cref="InvalidOperationException">当无法访问请求流时</exception>
    public static string Body
    {
        get
        {
            if (Request == null)
                return string.Empty;
            try
            {
                Request.EnableBuffering();
                return FileHelper.ToString(Request.Body, isCloseStream: false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("无法读取请求正文", ex);
            }
        }
    }

    #endregion

    #region Url(请求地址)

#if NETSTANDARD2_1
    /// <summary>
    /// 请求地址
    /// </summary>
    /// <exception cref="NotSupportedException">在 .NET Standard 2.1 中不支持此功能</exception>
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
    /// 主机名称
    /// </summary>
    public static string Host => HttpContext == null ? Dns.GetHostName() : GetClientHostName();

    /// <summary>
    /// 获取客户端主机名
    /// </summary>
    /// <returns>客户端主机名</returns>
    private static string GetClientHostName()
    {
        try
        {
            var address = GetRemoteAddress();
            if (string.IsNullOrWhiteSpace(address))
                return Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(IPAddress.Parse(address));
            var hostName = hostEntry.HostName;
            return hostName == LocalDomainIdentifier ? Dns.GetHostName() : hostName;
        }
        catch
        {
            return Dns.GetHostName();
        }
    }

    /// <summary>
    /// 获取远程地址
    /// </summary>
    private static string GetRemoteAddress() =>
        HttpContext?.Request?.Headers["HTTP_X_FORWARDED_FOR"].ToString() ??
        HttpContext?.Request?.Headers["REMOTE_ADDR"].ToString();

    #endregion

    #region Browser(浏览器)

    /// <summary>
    /// 用户代理字符串
    /// </summary>
    public static string Browser => HttpContext?.Request?.Headers["User-Agent"];

    #endregion

    #region RootPath(根路径)
#if !NETSTANDARD2_1

    /// <summary>
    /// 应用程序根路径
    /// </summary>
    public static string RootPath => Environment?.ContentRootPath;
#endif
    #endregion

    #region WebRootPath(Web根路径)

#if !NETSTANDARD2_1
    /// <summary>
    /// Web 根路径（wwwroot 目录）
    /// </summary>
    public static string WebRootPath => Environment?.WebRootPath;
#endif

    #endregion

    #region ContentType(内容类型)

    /// <summary>
    /// 请求内容类型
    /// </summary>
    public static string ContentType => HttpContext?.Request?.ContentType;

    #endregion

    #region QueryString(参数)

    /// <summary>
    /// 查询字符串参数
    /// </summary>
    public static string QueryString => HttpContext?.Request?.QueryString.ToString();

    #endregion

    #region IsLocal(是否本地请求)

    /// <summary>
    /// 判断是否为本地请求
    /// </summary>
    /// <returns>如果是本地请求返回 true，否则返回 false</returns>
    /// <exception cref="InvalidOperationException">当无法获取连接信息时</exception>
    public static bool IsLocal
    {
        get
        {
            var connection = HttpContext?.Connection;
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            try
            {
                if (connection.RemoteIpAddress.IsSet())
                    return connection.LocalIpAddress.IsSet()
                        ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                        : IPAddress.IsLoopback(connection.RemoteIpAddress);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("无法判断是否为本地请求", ex);
            }
        }
    }

    /// <summary>
    /// 判断 IP 地址是否已设置
    /// </summary>
    /// <param name="address">IP 地址</param>
    /// <returns>如果 IP 地址已设置返回 true，否则返回 false</returns>
    private static bool IsSet(this IPAddress address) => address != null && address.ToString() != NullIpAddress;

    #endregion

    #endregion

    #region 构造函数

    /// <summary>
    /// 静态构造函数
    /// </summary>
    static Web() => ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;

    #endregion

    #region GetFiles(获取客户端文件集合)

    /// <summary>
    /// 获取客户端上传的文件集合
    /// </summary>
    /// <returns>有效的文件集合，如果没有文件则返回空集合</returns>
    public static List<IFormFile> GetFiles()
    {
        var files = HttpContext?.Request?.Form?.Files;
        if (files == null || files.Count == 0)
            return [];

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
    /// 获取客户端上传的第一个文件
    /// </summary>
    /// <returns>第一个有效的文件，如果没有文件则返回 null</returns>
    public static IFormFile GetFile()
    {
        var files = GetFiles();
        return files.FirstOrDefault();
    }

    #endregion

    #region GetParam(获取请求参数)

    /// <summary>
    /// 获取请求参数值
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <returns>参数值，如果不存在则返回空字符串</returns>
    /// <exception cref="ArgumentException">当参数名为空或空白字符串时</exception>
    /// <remarks>
    /// 搜索顺序：查询参数 → 表单参数 → 请求头
    /// </remarks>
    public static string GetParam(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("参数名称不能为空或空白字符串", nameof(name));
        if (Request == null)
            return string.Empty;

        // 优先从查询参数获取
        var result = Request.Query[name].ToString();
        if (!string.IsNullOrWhiteSpace(result))
            return result;
        // 其次从表单参数获取
        try
        {
            result = Request.Form[name].ToString();
            if (!string.IsNullOrWhiteSpace(result))
                return result;
        }
        catch
        {
            // 忽略表单读取异常，继续尝试请求头
        }
        // 最后从请求头获取
        return Request.Headers[name].ToString();
    }

    #endregion

    #region UrlEncode(Url编码)

    /// <summary>
    /// URL 编码
    /// </summary>
    /// <param name="url">待编码的 URL 字符串</param>
    /// <param name="isUpper">编码字符是否转换为大写</param>
    /// <returns>编码后的字符串</returns>
    /// <example>
    /// 当 isUpper 为 true 时："http://" 转换为 "http%3A%2F%2F"
    /// </example>
    public static string UrlEncode(string url, bool isUpper = false) => UrlEncode(url, Encoding.UTF8, isUpper);

    /// <summary>
    /// URL 编码
    /// </summary>
    /// <param name="url">待编码的 URL 字符串</param>
    /// <param name="encoding">字符编码名称</param>
    /// <param name="isUpper">编码字符是否转换为大写</param>
    /// <returns>编码后的字符串</returns>
    /// <exception cref="ArgumentException">当编码名称无效时</exception>
    public static string UrlEncode(string url, string encoding, bool isUpper = false)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;
        var encodingName = string.IsNullOrWhiteSpace(encoding) ? DefaultEncodingName : encoding;
        try
        {
            return UrlEncode(url, Encoding.GetEncoding(encodingName), isUpper);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"无效的字符编码：{encodingName}", nameof(encoding), ex);
        }
    }

    /// <summary>
    /// URL 编码
    /// </summary>
    /// <param name="url">待编码的 URL 字符串</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="isUpper">编码字符是否转换为大写</param>
    /// <returns>编码后的字符串</returns>
    /// <exception cref="ArgumentNullException">当编码对象为空时</exception>
    public static string UrlEncode(string url, Encoding encoding, bool isUpper = false)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        var result = HttpUtility.UrlEncode(url, encoding);
        return isUpper == false ? result : GetUpperEncode(result);
    }

    /// <summary>
    /// 获取大写编码字符串
    /// </summary>
    /// <param name="encode">编码字符串</param>
    /// <returns>大写编码字符串</returns>
    private static string GetUpperEncode(string encode)
    {
        if (string.IsNullOrEmpty(encode))
            return encode;
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
    /// URL 解码
    /// </summary>
    /// <param name="url">待解码的 URL 字符串</param>
    /// <returns>解码后的字符串</returns>
    public static string UrlDecode(string url) => string.IsNullOrEmpty(url) ? string.Empty : HttpUtility.UrlDecode(url);

    /// <summary>
    /// URL 解码
    /// </summary>
    /// <param name="url">待解码的 URL 字符串</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>解码后的字符串</returns>
    /// <exception cref="ArgumentNullException">当编码对象为空时</exception>
    public static string UrlDecode(string url, Encoding encoding)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        return HttpUtility.UrlDecode(url, encoding);
    }

    #endregion

    #region Redirect(跳转到指定链接)

    /// <summary>
    /// 重定向到指定 URL
    /// </summary>
    /// <param name="url">目标 URL</param>
    /// <exception cref="ArgumentException">当 URL 为空或空白字符串时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static void Redirect(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("重定向 URL 不能为空或空白字符串", nameof(url));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        Response.Redirect(url);
    }

    #endregion

    #region Write(输出内容)

    /// <summary>
    /// 输出文本内容到响应流
    /// </summary>
    /// <param name="text">要输出的文本内容</param>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static void Write(string text)
    {
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        try
        {
            WriteAsync(text).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("输出内容失败", ex);
        }
    }

    /// <summary>
    /// 输出文本内容到响应流
    /// </summary>
    /// <param name="text">要输出的文本内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static async Task WriteAsync(string text, CancellationToken cancellationToken = default)
    {
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        if (Response.HasStarted)
            throw new InvalidOperationException("响应已开始发送，无法设置内容类型");
        try
        {
            Response.ContentType = PlainTextContentType;
            await Response.WriteAsync(text ?? string.Empty, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            throw new InvalidOperationException("输出内容失败", ex);
        }
    }

    #endregion

    #region Write(输出文件)

    /// <summary>
    /// 输出文件流到响应
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <exception cref="ArgumentNullException">当文件流为空时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用或输出失败时</exception>
    public static void Write(FileStream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        try
        {
            var size = stream.Length;
            var buffer = new byte[size];
            _ = stream.Read(buffer, 0, (int)size);

            var fileName = Path.GetFileName(stream.Name);
            stream.Dispose();

            // 删除临时文件
            if (File.Exists(stream.Name))
                File.Delete(stream.Name);

            Response.ContentType = OctetStreamContentType;
            Response.Headers["Content-Disposition"] = $"attachment;filename={WebUtility.UrlEncode(fileName)}";
            Response.Headers["Content-Length"] = size.ToString();

            Task.Run(async () => { await Response.Body.WriteAsync(buffer, 0, (int)size); })
                .GetAwaiter()
                .GetResult();
            Response.Body.Close();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("输出文件失败", ex);
        }
    }

    #endregion

    #region GetBodyAsync(获取请求正文)

    /// <summary>
    /// 获取请求正文内容
    /// </summary>
    /// <returns>请求正文的字符串表示</returns>
    /// <exception cref="InvalidOperationException">当无法访问请求流时</exception>
    public static async Task<string> GetBodyAsync()
    {
        if (Request == null)
            return string.Empty;
        try
        {
            Request.EnableBuffering();
            return await FileHelper.ToStringAsync(Request.Body, isCloseStream: false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("无法读取请求正文", ex);
        }
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
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    /// <param name="encoding">字符编码</param>
    /// <exception cref="ArgumentException">当文件路径为空时</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时</exception>
    public static async Task DownloadFileAsync(string filePath, string fileName, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在：{filePath}");
        var bytes = await FileHelper.ReadToBytesAsync(filePath);
        await DownloadAsync(bytes, fileName, encoding);
    }

    /// <summary>
    /// 下载流内容
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    public static Task DownloadAsync(Stream stream, string fileName) =>
        DownloadAsync(stream, fileName, Encoding.UTF8);

    /// <summary>
    /// 下载流内容
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    /// <param name="encoding">字符编码</param>
    /// <exception cref="ArgumentNullException">当流为空时</exception>
    public static async Task DownloadAsync(Stream stream, string fileName, Encoding encoding)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        var bytes = await FileHelper.ToBytesAsync(stream);
        await DownloadAsync(bytes, fileName, encoding, Response);
    }

    /// <summary>
    /// 下载字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    public static Task DownloadAsync(byte[] bytes, string fileName) =>
        DownloadAsync(bytes, fileName, Encoding.UTF8, Response);

    /// <summary>
    /// 下载字节数组
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    /// <param name="encoding">字符编码</param>
    public static Task DownloadAsync(byte[] bytes, string fileName, Encoding encoding) =>
        DownloadAsync(bytes, fileName, encoding, Response);

    /// <summary>
    /// 下载字节数组
    /// </summary>
    /// <param name="bytes">文件字节数组</param>
    /// <param name="fileName">下载文件名（包含扩展名）</param>
    /// <param name="encoding">字符编码</param>
    /// <param name="response">HTTP 响应对象</param>
    /// <exception cref="ArgumentException">当文件字节数组为空或文件名为空时</exception>
    /// <exception cref="ArgumentNullException">当字符编码或HTTP响应为空时</exception>
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

        var encodedFileName = UrlEncode(fileName.Replace(" ", ""), encoding);
        response.ContentType = OctetStreamContentType;
        response.Headers["Content-Disposition"] = $"attachment; filename={encodedFileName}";
        response.Headers["Content-Length"] = bytes.Length.ToString();
        await response.Body.WriteAsync(bytes, 0, bytes.Length);
    }

    #endregion

    #region GetCookie(获取Cookie值)

    /// <summary>
    /// 获取 Cookie 值
    /// </summary>
    /// <param name="key">Cookie 键名</param>
    /// <returns>Cookie 值，如果不存在则返回 null</returns>
    /// <exception cref="ArgumentException">当键名为空或空白字符串时</exception>
    public static string GetCookie(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cookie 键名不能为空或空白字符串", nameof(key));
        return Request?.Cookies[key];
    }

    #endregion

    #region SetCookie(设置Cookie值)

    /// <summary>
    /// 设置 Cookie 值
    /// </summary>
    /// <param name="key">Cookie 键名</param>
    /// <param name="value">Cookie 值</param>
    /// <exception cref="ArgumentException">当键名为空或空白字符串时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    /// <remarks>
    /// 未设置过期时间，写入的是浏览器进程 Cookie，浏览器关闭后自动失效
    /// </remarks>
    public static void SetCookie(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cookie 键名不能为空或空白字符串", nameof(key));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        Response.Cookies.Append(key, value ?? string.Empty);
    }

    /// <summary>
    /// 设置 Cookie 值
    /// </summary>
    /// <param name="key">Cookie 键名</param>
    /// <param name="value">Cookie 值</param>
    /// <param name="options">Cookie 配置选项</param>
    /// <exception cref="ArgumentException">当键名为空或空白字符串时</exception>
    /// <exception cref="ArgumentNullException">当配置选项为空时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static void SetCookie(string key, string value, CookieOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cookie 键名不能为空或空白字符串", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        Response.Cookies.Append(key, value ?? string.Empty, options);
    }

    #endregion

    #region RemoveCookie(移除Cookie)

    /// <summary>
    /// 移除 Cookie
    /// </summary>
    /// <param name="key">Cookie 键名</param>
    /// <exception cref="ArgumentException">当键名为空或空白字符串时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static void RemoveCookie(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cookie 键名不能为空或空白字符串", nameof(key));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        Response.Cookies.Delete(key);
    }

    /// <summary>
    /// 移除 Cookie
    /// </summary>
    /// <param name="key">Cookie 键名</param>
    /// <param name="options">Cookie 配置选项</param>
    /// <exception cref="ArgumentException">当键名为空或空白字符串时</exception>
    /// <exception cref="ArgumentNullException">当配置选项为空时</exception>
    /// <exception cref="InvalidOperationException">当响应对象不可用时</exception>
    public static void RemoveCookie(string key, CookieOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cookie 键名不能为空或空白字符串", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (Response == null)
            throw new InvalidOperationException("HTTP 响应对象不可用");
        Response.Cookies.Delete(key, options);
    }

    #endregion
}