﻿using System.Text;

namespace Bing.Text;

/// <summary>
/// 字符串操作 - 工具
/// </summary>
public static partial class Str
{
    /// <summary>
    /// 新行
    /// </summary>
    public const string NewLine = "\r\n";

    /// <summary>
    /// 将 null 转换为 Empty
    /// </summary>
    /// <param name="str">字符串</param>
    public static string NullToEmpty(string str) => str.AvoidNull();

    /// <summary>
    /// 将 Empty 转换为 null
    /// </summary>
    /// <param name="str">字符串</param>
    public static string EmptyToNul(string str) => str.IsNullOrEmpty() ? null : str;

    /// <summary>
    /// 通用前缀。从左到右，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    public static string CommonPrefix(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;
        var sb = new StringBuilder();
        var rangeTimes = left.Length < right.Length ? left.Length : right.Length;
        for (var i = 0; i < rangeTimes; i++)
        {
            if(left[i]!=right[i])
                break;
            sb.Append(left[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 通用后缀。从右到左，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    public static string CommonSuffix(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;
        var sb = new StringBuilder();
        var rangeTimes = left.Length < right.Length ? left.Length : right.Length;
        int leftPointer = left.Length - 1, rightPointer = right.Length - 1;
        for (var i = 0; i < rangeTimes; i++, leftPointer--, rightPointer--)
        {
            if (left[leftPointer] != right[rightPointer])
                break;
            sb.Append(left[leftPointer]);
        }
        return sb.ToReverseString();
    }

    /// <summary>
    /// 重复指定次数的字符串
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="times">次数</param>
    public static string Repeat(string source, int times) => source.Repeat(times);

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="source">字符</param>
    /// <param name="times">次数</param>
    public static string Repeat(char source, int times) => source.Repeat(times);

    /// <summary>
    /// 填充。向左填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadStart(string source, int width, char appendChar) => source.PadLeft(width, appendChar);

    /// <summary>
    /// 填充。向右填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadEnd(string source, int width, char appendChar) => source.PadRight(width, appendChar);

    /// <summary>
    /// 移除起始字符串
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    /// <param name="start">要移除的值</param>
    public static StringBuilder RemoveStart(StringBuilder builder, string start)
    {
        if (builder == null)
            return null;
        if (builder.Length == 0)
            return builder;
        if(string.IsNullOrEmpty(start))
            return builder;
        if (start.Length > builder.Length)
            return builder;
        var chars = start.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (builder[i] != chars[i])
                return builder;
        }
        return builder.Remove(0, start.Length);
    }

    /// <summary>
    /// 移除末尾字符串
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    /// <param name="end">要移除的值</param>
    public static StringBuilder RemoveEnd(StringBuilder builder, string end)
    {
        if (builder == null)
            return null;
        if (builder.Length == 0)
            return builder;
        if (string.IsNullOrEmpty(end))
            return builder;
        if (end.Length > builder.Length)
            return builder;
        var chars = end.ToCharArray();
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            var j = builder.Length - (chars.Length - i);
            if (builder[j] != chars[i])
                return builder;
        }

        return builder.Remove(builder.Length-end.Length, end.Length);
    }
}