﻿using System.Drawing;
using System.Text.RegularExpressions;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 加载
/// </summary>
public static partial class ImageHelper
{
    #region FromFile(从指定文件创建图片)

    /// <summary>
    /// 从指定文件创建图片
    /// </summary>
    /// <param name="filePath">文件的绝对路径</param>
    public static Image FromFile(string filePath) => Image.FromFile(filePath);

    #endregion

    #region FromStream(从指定流创建图片)

    /// <summary>
    /// 从指定流创建图片
    /// </summary>
    /// <param name="stream">流</param>
    public static Image FromStream(Stream stream) => Image.FromStream(stream);

    #endregion

    #region FromBytes(从指定字节数组创建图片)

    /// <summary>
    /// 从指定字节数组创建图片
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Image FromBytes(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        using var ms = new MemoryStream(bytes);
        return Image.FromStream(ms);
    }

    #endregion

    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <param name="base64String">Base64字符串</param>
    public static Image FromBase64String(string base64String)
    {
        var bytes = Convert.FromBase64String(GetBase64String(base64String));
        using var ms = new MemoryStream(bytes);
        return Image.FromStream(ms);
    }

    /// <summary>
    /// 获取真正的图片Base64数据。
    /// 即去掉data:image/jpg;base64,这样的格式
    /// </summary>
    /// <param name="base64String">带前缀的Base64图片字符串</param>
    private static string GetBase64String(string base64String)
    {
        var pattern = "^(data:image/.*?;base64,).*?$";
        var match = Regex.Match(base64String, pattern);
        return base64String.Replace(match.Groups[1].ToString(), "");
    }

    #endregion
}