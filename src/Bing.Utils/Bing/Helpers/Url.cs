using System.Text.RegularExpressions;
using System.Web;

namespace Bing.Helpers;

/// <summary>
/// URL 操作工具类，提供 URL 合并、连接、编码解码等功能。
/// </summary>
public static partial class Url
{
    #region Combine(合并Url)

    /// <summary>
    /// 合并 URL 路径片段，自动处理斜杠分隔符
    /// </summary>
    /// <param name="urls">URL 片段数组，例如 "http://a.com", "b" 将返回 "http://a.com/b"</param>
    /// <returns>合并后的完整 URL 字符串</returns>
    public static string Combine(params string[] urls)
    {
        if (urls == null || urls.Length == 0)
            return string.Empty;

        // 过滤空白 URL 并标准化路径分隔符
        var validUrls = urls.Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Replace(@"\", "/"))
            .ToArray();

        if (validUrls.Length == 0)
            return string.Empty;

        var firstUrl = validUrls.First();
        var lastUrl = validUrls.Last();

        // 移除每个片段首尾的斜杠，避免重复
        var trimmedUrls = validUrls.Select(url => url.Trim('/')).ToArray();

        // 使用 Path.Combine 然后标准化为正斜杠
        var result = Path.Combine(trimmedUrls).Replace(@"\", "/");

        // 保持原始 URL 的开头和结尾格式
        if (firstUrl.StartsWith("/"))
            result = $"/{result}";
        if (lastUrl.EndsWith("/"))
            result = $"{result}/";
        return result;
    }

    #endregion

    #region Join(连接Url)

    /// <summary>
    /// 将查询参数连接到 URL 上
    /// </summary>
    /// <param name="url">基础 URL，例如 "http://a.com"</param>
    /// <param name="param">查询参数，例如 "b=1"</param>
    /// <returns>连接后的 URL，例如 "http://a.com?b=1"</returns>
    /// <exception cref="ArgumentNullException">当 URL 为 null 或空时抛出</exception>
    public static string Join(string url, string param)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        if (string.IsNullOrWhiteSpace(param))
            return url;
        return $"{GetUrlWithSeparator(url)}{param}";
    }

    /// <summary>
    /// 将多个查询参数连接到 URL 上
    /// </summary>
    /// <param name="url">基础 URL，例如 "http://a.com"</param>
    /// <param name="parameters">查询参数数组，例如 ["b=1"]</param>
    /// <returns>连接后的 URL，例如 "http://a.com?b=1"</returns>
    /// <exception cref="ArgumentNullException">当 URL 为 null 或空时抛出</exception>
    public static string Join(string url, params string[] parameters)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        if (parameters == null || parameters.Length == 0)
            return url;
        var result = url;
        var validParams = parameters.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        foreach (var param in validParams) 
            result = $"{GetUrlWithSeparator(result)}{param}";
        return result;
    }

    /// <summary>
    /// 获取带有适当分隔符的 URL
    /// </summary>
    /// <param name="url">URL 字符串</param>
    /// <returns>带有查询参数分隔符的 URL</returns>
    private static string GetUrlWithSeparator(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "?";
        if (!url.Contains("?"))
            return $"{url}?";
        if (url.EndsWith("?") || url.EndsWith("&"))
            return url;
        return $"{url}&";
    }

    /// <summary>
    /// 将查询参数连接到 Uri 对象上
    /// </summary>
    /// <param name="url">Uri 对象</param>
    /// <param name="param">查询参数</param>
    /// <returns>连接后的新 Uri 对象</returns>
    /// <exception cref="ArgumentNullException">当 Uri 为 null 时抛出</exception>
    /// <exception cref="UriFormatException">当生成的 URL 格式无效时抛出</exception>
    public static Uri Join(Uri url, string param)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url));
        var result = Join(url.AbsoluteUri, param);
        return new Uri(result);
    }

    /// <summary>
    /// 将多个查询参数连接到 Uri 对象上
    /// </summary>
    /// <param name="url">Uri 对象</param>
    /// <param name="parameters">查询参数数组</param>
    /// <returns>连接后的新 Uri 对象</returns>
    /// <exception cref="ArgumentNullException">当 Uri 为 null 时抛出</exception>
    /// <exception cref="UriFormatException">当生成的 URL 格式无效时抛出</exception>
    public static Uri Join(Uri url, params string[] parameters)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url));
        var result = Join(url.AbsoluteUri, parameters);
        return new Uri(result);
    }

    #endregion

    #region GetMainDomain(获取主域名)

    /// <summary>
    /// 从 URL 中提取主域名（二级域名）
    /// </summary>
    /// <param name="url">完整的 URL 地址</param>
    /// <returns>主域名，如果无法解析则返回原 URL</returns>
    /// <exception cref="ArgumentException">当 URL 格式严重错误时抛出</exception>
    public static string GetMainDomain(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;
        try
        {
            // 尝试解析为 Uri 对象
            var uri = url.StartsWith("http://") || url.StartsWith("https://") ?
                new Uri(url) : new Uri($"http://{url}");

            var host = uri.Host;
            return ExtractMainDomain(host);
        }
        catch (UriFormatException)
        {
            // 如果 Uri 解析失败，尝试直接从字符串中提取
            return ExtractMainDomainFromString(url);
        }
    }

    /// <summary>
    /// 从主机名中提取主域名
    /// </summary>
    /// <param name="host">主机名</param>
    /// <returns>主域名</returns>
    private static string ExtractMainDomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return host;
        var parts = host.Split('.');
        // 处理 IP 地址
        if (IsIpAddress(host))
            return host;

        // 处理特殊的本地域名
        if (IsSpecialLocalDomain(host))
            return GetSpecialLocalDomain(host);

        // 至少需要两个部分才能构成有效域名
        if (parts.Length < 2)
            return host;

        // 获取最后两个部分作为主域名
        var mainDomain = $"{parts[^2]}.{parts[^1]}";
        return mainDomain;
    }

    /// <summary>
    /// 从字符串中直接提取主域名
    /// </summary>
    /// <param name="url">URL 字符串</param>
    /// <returns>主域名</returns>
    private static string ExtractMainDomainFromString(string url)
    {
        // 移除协议部分
        var cleanUrl = Regex.Replace(url, @"^https?://", "", RegexOptions.IgnoreCase);
        // 移除路径和查询参数
        var domainPart = cleanUrl.Split('/')[0].Split('?')[0];
        return ExtractMainDomain(domainPart);
    }

    /// <summary>
    /// 检查是否为特殊的本地域名
    /// </summary>
    /// <param name="host">主机名</param>
    /// <returns>如果是特殊本地域名返回 true，否则返回 false</returns>
    private static bool IsSpecialLocalDomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        // 检查是否以 .localhost 结尾或就是 localhost
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取特殊本地域名的主域名
    /// </summary>
    /// <param name="host">主机名</param>
    /// <returns>本地主域名</returns>
    private static string GetSpecialLocalDomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return host;
        // 对于 localhost 类型的域名，统一返回 localhost
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return "localhost";
        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            return "localhost";
        return host;
    }

    /// <summary>
    /// 检查字符串是否为 IP 地址
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>如果是 IP 地址返回 true，否则返回 false</returns>
    private static bool IsIpAddress(string input) => System.Net.IPAddress.TryParse(input, out _);

    #endregion

    #region UrlEncode(Url编码)

    /// <summary>
    /// 对 URL 进行编码，使用 UTF-8 字符编码
    /// </summary>
    /// <param name="url">要编码的 URL 字符串</param>
    /// <param name="isUpper">编码后的十六进制字符是否使用大写，例如 "%3A" 而不是 "%3a"</param>
    /// <returns>编码后的 URL 字符串</returns>
    public static string UrlEncode(string url, bool isUpper = false)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        return UrlEncode(url, Encoding.UTF8, isUpper);
    }

    /// <summary>
    /// 对 URL 进行编码，使用指定的字符编码名称
    /// </summary>
    /// <param name="url">要编码的 URL 字符串</param>
    /// <param name="encoding">字符编码名称，例如 "UTF-8"、"GBK"</param>
    /// <param name="isUpper">编码后的十六进制字符是否使用大写</param>
    /// <returns>编码后的 URL 字符串</returns>
    /// <exception cref="ArgumentException">当编码名称无效时抛出</exception>
    public static string UrlEncode(string url, string encoding, bool isUpper = false)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        if (string.IsNullOrWhiteSpace(encoding))
            encoding = "UTF-8";
        try
        {
            var encodingObject = Encoding.GetEncoding(encoding);
            return UrlEncode(url, encodingObject, isUpper);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"不支持的字符编码: {encoding}", nameof(encoding), ex);
        }
        
    }

    /// <summary>
    /// 对 URL 进行编码，使用指定的字符编码对象
    /// </summary>
    /// <param name="url">要编码的 URL 字符串</param>
    /// <param name="encoding">字符编码对象</param>
    /// <param name="isUpper">编码后的十六进制字符是否使用大写</param>
    /// <returns>编码后的 URL 字符串</returns>
    /// <exception cref="ArgumentNullException">当编码对象为 null 时抛出</exception>
    public static string UrlEncode(string url, Encoding encoding, bool isUpper = false)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        var result = HttpUtility.UrlEncode(url, encoding);
        return isUpper ? ConvertToUppercaseHex(result) : result;
    }

    /// <summary>
    /// 将编码字符串中的十六进制部分转换为大写
    /// </summary>
    /// <param name="encoded">已编码的字符串</param>
    /// <returns>十六进制部分为大写的编码字符串</returns>
    private static string ConvertToUppercaseHex(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return encoded;

        var result = new StringBuilder(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            if (encoded[i] == '%' && i + 2 < encoded.Length)
            {
                // 添加 % 符号和接下来的两个字符（转为大写）
                result.Append('%');
                result.Append(char.ToUpper(encoded[i + 1]));
                result.Append(char.ToUpper(encoded[i + 2]));
                i += 2; // 跳过接下来的两个字符
            }
            else
            {
                result.Append(encoded[i]);
            }
        }

        return result.ToString();
    }

    #endregion

    #region UrlDecode(Url解码)

    /// <summary>
    /// 对 URL 进行解码，使用 UTF-8 字符编码
    /// </summary>
    /// <param name="url">要解码的 URL 字符串</param>
    /// <returns>解码后的字符串</returns>
    public static string UrlDecode(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        return HttpUtility.UrlDecode(url);
    }

    /// <summary>
    /// 对 URL 进行解码，使用指定的字符编码
    /// </summary>
    /// <param name="url">要解码的 URL 字符串</param>
    /// <param name="encoding">字符编码对象</param>
    /// <returns>解码后的字符串</returns>
    /// <exception cref="ArgumentNullException">当编码对象为 null 时抛出</exception>
    public static string UrlDecode(string url, Encoding encoding)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        return HttpUtility.UrlDecode(url, encoding);
    }

    #endregion
}