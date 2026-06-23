using System.Security.Cryptography;

namespace Bing.Drawing;

/// <summary>
/// 验证码文本生成器
/// </summary>
internal static class CaptchaCodeGenerator
{
    private const string NumberAndLetterSeed = "23456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghkmnpqrstuvwxyz";
    private const string NumberSeed = "0123456789";

    /// <summary>
    /// 生成去歧义字母数字验证码文本
    /// </summary>
    /// <param name="length">验证码长度</param>
    internal static string Generate(int length)
    {
        return Generate(length, CaptchaType.NumberAndLetter);
    }

    /// <summary>
    /// 按类型生成验证码文本
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <param name="captchaType">验证码类型</param>
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
    /// 从指定字符集生成随机文本
    /// </summary>
    private static string GenerateFromSeed(string seed, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = seed[DrawingCompatibilityHelper.GetRandomInt32(seed.Length)];
        return new string(chars);
    }

    /// <summary>
    /// 生成随机汉字字符串（使用受控白名单字符集）
    /// </summary>
    private static string GenerateChinese(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Internal.ChineseCaptchaGlyphSet.Chars[DrawingCompatibilityHelper.GetRandomInt32(Internal.ChineseCaptchaGlyphSet.CharCount)];
        return new string(chars);
    }
}
