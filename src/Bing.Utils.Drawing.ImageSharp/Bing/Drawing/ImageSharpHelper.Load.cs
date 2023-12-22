using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bing.Drawing;

/// <summary>
/// 图片操作辅助类 - 加载
/// </summary>
public static partial class ImageSharpHelper
{
    #region FromBase64String(从指定Base64字符串创建图片)

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <param name="base64String">Base64字符串</param>
    public static Image? FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return default;
        try
        {
            return Image.Load(Convert.FromBase64String(GetBase64String(base64String)));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 从指定Base64字符串创建图片
    /// </summary>
    /// <typeparam name="TPixel">像素类型</typeparam>
    /// <param name="base64String">Base64字符串</param>
    public static Image<TPixel>? FromBase64String<TPixel>(string base64String)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return default;
        try
        {
            return Image.Load<TPixel>(Convert.FromBase64String(GetBase64String(base64String)));
        }
        catch
        {
            return default;
        }
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