using System.Security.Cryptography;

namespace Bing.Drawing;

/// <summary>
/// 验证码文本生成器
/// </summary>
internal static class CaptchaCodeGenerator
{
    private const string Seed = "23456789ABCDEFGHJKMNPQRSTUVWXYZabcdefghkmnpqrstuvwxyz";

    /// <summary>
    /// 生成验证码文本
    /// </summary>
    /// <param name="length">验证码长度</param>
    internal static string Generate(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Seed[DrawingCompatibilityHelper.GetRandomInt32(Seed.Length)];

        return new string(chars);
    }
}
