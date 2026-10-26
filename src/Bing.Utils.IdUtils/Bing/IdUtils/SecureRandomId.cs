using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// 使用密码学安全随机数生成器创建指定字符集的随机标识文本。
/// </summary>
/// <remarks>
/// 输出长度按请求精确生成，不保证全局唯一；随机字节采用拒绝采样，避免字符索引分布偏差。
/// </remarks>
public static class SecureRandomId
{
    /// <summary>
    /// 包含小写字母、大写字母和数字的 62 字符唯一字符集。
    /// </summary>
    public const string AlphaNumericAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// 每轮从密码学随机源读取的固定字节数。
    /// </summary>
    private const int RandomByteChunkSize = 256;

    /// <summary>
    /// 使用密码学安全随机数生成器创建指定长度的随机标识文本。
    /// </summary>
    /// <param name="length">结果长度，必须大于零。</param>
    /// <param name="alphabet">包含 2 到 256 个不同非代理 UTF-16 字符的字符集。</param>
    /// <returns>由指定字符集构成且长度精确为 <paramref name="length"/> 的随机文本。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 不大于零。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="alphabet"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">字符集长度不在 2 到 256 之间，含重复字符或代理字符。</exception>
    public static string Create(int length, string alphabet)
    {
        ValidateArguments(length, alphabet);
        using (var random = RandomNumberGenerator.Create())
            return Generate(length, alphabet, random.GetBytes);
    }

    /// <summary>
    /// 使用可替换随机填充委托生成结果，供确定性测试验证映射和拒绝采样。
    /// </summary>
    /// <param name="length">结果长度，必须大于零。</param>
    /// <param name="alphabet">通过参数校验的字符集。</param>
    /// <param name="fillRandomBytes">填充每个固定长度随机缓冲区的委托。</param>
    /// <returns>由指定字符集构成且长度精确为 <paramref name="length"/> 的文本。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 不大于零。</exception>
    /// <exception cref="ArgumentNullException">字符集或随机填充委托为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">字符集无效。</exception>
    internal static string CreateCore(int length, string alphabet, Action<byte[]> fillRandomBytes)
    {
        ValidateArguments(length, alphabet);
        if (fillRandomBytes == null)
            throw new ArgumentNullException(nameof(fillRandomBytes));

        return Generate(length, alphabet, fillRandomBytes);
    }

    /// <summary>
    /// 按固定随机块和无偏采样规则填充结果字符。
    /// </summary>
    /// <param name="length">已经校验的输出长度。</param>
    /// <param name="alphabet">已经校验的字符集。</param>
    /// <param name="fillRandomBytes">填充随机缓冲区的委托。</param>
    /// <returns>长度精确的随机文本。</returns>
    private static string Generate(int length, string alphabet, Action<byte[]> fillRandomBytes)
    {
        var result = new char[length];
        var randomBytes = new byte[RandomByteChunkSize];
        var usableByteCount = byte.MaxValue + 1 - (byte.MaxValue + 1) % alphabet.Length;
        var resultIndex = 0;
        while (resultIndex < result.Length)
        {
            fillRandomBytes(randomBytes);
            for (var byteIndex = 0; byteIndex < randomBytes.Length && resultIndex < result.Length; byteIndex++)
            {
                var value = randomBytes[byteIndex];
                if (value >= usableByteCount)
                    continue;

                result[resultIndex++] = alphabet[value % alphabet.Length];
            }
        }

        return new string(result);
    }

    /// <summary>
    /// 验证长度和字符集范围，并拒绝代理字符与重复字符。
    /// </summary>
    /// <param name="length">请求的输出长度。</param>
    /// <param name="alphabet">待验证的字符集。</param>
    private static void ValidateArguments(int length, string alphabet)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "长度必须大于零。");
        if (alphabet == null)
            throw new ArgumentNullException(nameof(alphabet));
        if (alphabet.Length < 2 || alphabet.Length > 256)
            throw new ArgumentException("字符集必须包含 2 到 256 个字符。", nameof(alphabet));

        var uniqueCharacters = new HashSet<char>();
        foreach (var character in alphabet)
        {
            if (char.IsSurrogate(character))
                throw new ArgumentException("字符集不能包含 UTF-16 代理字符。", nameof(alphabet));
            if (!uniqueCharacters.Add(character))
                throw new ArgumentException("字符集不能包含重复字符。", nameof(alphabet));
        }
    }
}
