namespace Bing.Drawing;

/// <summary>
/// 验证码类型
/// </summary>
public enum CaptchaType
{
    /// <summary>
    /// 数字
    /// </summary>
    Number,

    /// <summary>
    /// 字母数字混合
    /// </summary>
    NumberAndLetter,

    /// <summary>
    /// 汉字
    /// </summary>
    ChineseChar
}