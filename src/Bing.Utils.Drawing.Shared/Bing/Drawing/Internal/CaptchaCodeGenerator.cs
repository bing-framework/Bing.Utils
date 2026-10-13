using System.Security.Cryptography;

namespace Bing.Drawing;

/// <summary>
/// 验证码文本生成器
/// </summary>
internal static class CaptchaCodeGenerator
{
    /// <summary>
    /// 用于生成去歧义字母数字验证码的字符集合。
    /// </summary>
    private const string NumberAndLetterSeed = "23456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghkmnpqrstuvwxyz";
    /// <summary>
    /// 用于生成数字验证码的十进制数字集合。
    /// </summary>
    private const string NumberSeed = "0123456789";

    /// <summary>
    /// 生成指定长度的验证码。
    /// </summary>
    /// <param name="length">验证码长度。</param>
    /// <returns>生成的验证码文本。</returns>
    internal static string Generate(int length)
    {
        return Generate(length, CaptchaType.NumberAndLetter);
    }

    /// <summary>
    /// 按指定长度和类型生成验证码。
    /// </summary>
    /// <param name="length">验证码长度。</param>
    /// <param name="captchaType">验证码类型。</param>
    /// <returns>生成的验证码文本。</returns>
    internal static string Generate(int length, CaptchaType captchaType)
    {
        switch (captchaType)
        {
            case CaptchaType.Number:
                return GenerateFromSeed(NumberSeed, length);
            case CaptchaType.ChineseChar:
                return GenerateChinese(length);
            default:
                return GenerateFromSeed(NumberAndLetterSeed, length);
        }
    }

    /// <summary>
    /// 根据种子生成指定长度的验证码。
    /// </summary>
    /// <param name="seed">用于生成验证码的种子。</param>
    /// <param name="length">验证码长度。</param>
    /// <returns>生成的验证码文本。</returns>
    private static string GenerateFromSeed(string seed, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = seed[DrawingCompatibilityHelper.GetRandomInt32(seed.Length)];
        return new string(chars);
    }

    /// <summary>
    /// 生成指定长度的中文验证码。
    /// </summary>
    /// <param name="length">验证码长度。</param>
    /// <returns>生成的验证码文本。</returns>
    private static string GenerateChinese(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Internal.ChineseCaptchaGlyphSet.Chars[DrawingCompatibilityHelper.GetRandomInt32(Internal.ChineseCaptchaGlyphSet.CharCount)];
        return new string(chars);
    }
}
