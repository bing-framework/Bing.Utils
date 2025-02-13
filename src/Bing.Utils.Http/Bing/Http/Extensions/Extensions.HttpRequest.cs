﻿using System.Text;
using System.Text.RegularExpressions;
using Bing.Extensions;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Bing.Http;

/// <summary>
/// Http请求(<see cref="HttpRequest"/>) 扩展
/// </summary>
public static class HttpRequestExtensions
{
    #region GetAbsoluteUri(获取Http请求的绝对路径)

    /// <summary>
    /// 获取Http请求的绝对路径
    /// </summary>
    /// <param name="request">Http请求</param>
    public static string GetAbsoluteUri(this HttpRequest request) => new StringBuilder()
        .Append(request.Scheme)
        .Append("://")
        .Append(request.Host)
        .Append(request.PathBase)
        .Append(request.Path)
        .Append(request.QueryString)
        .ToString();

    #endregion

    #region Query(获取查询参数)

    /// <summary>
    /// 获取查询参数
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="request">Http请求</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    public static T Query<T>(this HttpRequest request, string key, T defaultValue = default)
        where T : IConvertible
    {
        var value = request.Query.FirstOrDefault(x => x.Key == key);
        if (string.IsNullOrWhiteSpace(value.Value.ToString()))
            return defaultValue;
        try
        {
            return (T)Convert.ChangeType(value.Value.ToString(), typeof(T));
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }

    #endregion

    #region Form(获取表单参数)

    /// <summary>
    /// 获取表单参数
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="request">Http请求</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    public static T Form<T>(this HttpRequest request, string key, T defaultValue = default)
        where T : IConvertible
    {
        var value = request.Form.FirstOrDefault(x => x.Key == key);
        if (string.IsNullOrWhiteSpace(value.Value.ToString()))
            return defaultValue;
        try
        {
            return (T)Convert.ChangeType(value.Value.ToString(), typeof(T));
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }

    #endregion

    #region Params(获取参数)

    /// <summary>
    /// 获取参数
    /// </summary>
    /// <param name="request">请求信息</param>
    /// <param name="key">键名</param>
    public static string Params(this HttpRequest request, string key)
    {
        if (request.Query.ContainsKey(key))
            return request.Query[key];
        if (request.HasFormContentType)
            return request.Form[key];
        return null;
    }

    #endregion

    #region IsAjaxRequest(是否Ajax请求)

    /// <summary>
    /// 是否Ajax请求
    /// </summary>
    /// <param name="request">Http请求</param>
    public static bool IsAjaxRequest(this HttpRequest request)
    {
        request.CheckNotNull(nameof(request));
        var isHxr = request.Headers?["X-Requested-With"].ToString()
            ?.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isHxr.HasValue && isHxr.Value)
            return true;
        if (!string.IsNullOrEmpty(request.ContentType))
            return request.ContentType.ToLower().Equals("application/x-www-form-urlencoded") ||
                   request.ContentType.ToLower().Equals("application/json");
        return false;
    }

    #endregion

    #region IsJsonContentType(是否Json内容类型)

    /// <summary>
    /// 是否Json内容类型
    /// </summary>
    /// <param name="request">Http请求</param>
    public static bool IsJsonContentType(this HttpRequest request)
    {
        request.CheckNotNull(nameof(request));
        bool flag =
            request.Headers?["Content-Type"].ToString()
                .IndexOf("application/json", StringComparison.OrdinalIgnoreCase) > -1 || request
                .Headers?["Content-Type"].ToString().IndexOf("text/json", StringComparison.OrdinalIgnoreCase) > -1;

        if (flag)
            return true;
        flag =
            request.Headers?["Accept"].ToString().IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >
            -1 || request.Headers?["Accept"].ToString().IndexOf("text/json", StringComparison.OrdinalIgnoreCase) >
            -1;
        return flag;
    }

    #endregion

    #region IsMobileBrowser(是否移动端浏览器)

    /// <summary>
    /// 浏览器正则表达式
    /// </summary>
    private static readonly Regex BrowserRegex = new Regex(
        @"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    /// <summary>
    /// 版本号正则表达式
    /// </summary>
    private static readonly Regex VersionRegex = new Regex(
        @"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    /// <summary>
    /// 是否移动端浏览器
    /// </summary>
    /// <param name="request">Http请求</param>
    public static bool IsMobileBrowser(this HttpRequest request)
    {
        var userAgent = request.UserAgent();
        return BrowserRegex.IsMatch(userAgent) || VersionRegex.IsMatch(userAgent.Substring(0, 4));
    }

    #endregion

    #region UserAgent(用户代理)

    /// <summary>
    /// 用户代理
    /// </summary>
    /// <param name="request">Http请求</param>
    public static string UserAgent(this HttpRequest request) => request.Headers["User-Agent"];

    #endregion
}