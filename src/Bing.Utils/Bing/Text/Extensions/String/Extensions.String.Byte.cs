﻿
// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    public static byte[] ToUtf8Bytes(this string value) => value.ToBytes(Encoding.UTF8);

    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    public static byte[] ToUtf7Bytes(this string value) => value.ToBytes(Encoding.UTF7);

    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    // ReSharper disable once InconsistentNaming
    public static byte[] ToASCIIBytes(this string value) => value.ToBytes(Encoding.ASCII);

    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    public static byte[] ToBigEndianUnicodeBytes(this string value) => value.ToBytes(Encoding.BigEndianUnicode);

    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    public static byte[] ToDefaultBytes(this string value) => value.ToBytes(Encoding.Default);

    /// <summary>
    /// 转换为byte[]
    /// </summary>
    /// <param name="value">字符串</param>
    public static byte[] ToUnicodeBytes(this string value) => value.ToBytes(Encoding.Unicode);
}