﻿

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 移除字符串。移除指定字符串之后的字符串
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="removeFromThis">移除字符串</param>
    public static string RemoveFromIgnoreCase(this string text, string removeFromThis)
    {
        var index = text.IndexOfIgnoreCase(removeFromThis);
        return index < 0 ? text : text.Substring(0, index);
    }

    /// <summary>
    /// 移除字符串。移除重复的空格
    /// </summary>
    /// <param name="text">字符串</param>
    public static string RemoveDuplicateSpaces(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        string ante = null;
        while (ante!=text)
        {
            ante = text;
            text = text.Replace("  ", " ");
        }
        return text;
    }

    /// <summary>
    /// 移除字符串。移除音调
    /// </summary>
    /// <param name="text">字符串</param>
    public static string RemoveAccentsIgnoreCaseAndN(this string text) =>
        string.IsNullOrEmpty(text)
            ? text
            : RemoveAccentsIgnoreCase(text).Replace('Ñ', 'N').Replace('ñ', 'n');

    /// <summary>
    /// 移除字符串。移除音调
    /// </summary>
    /// <param name="text">字符串</param>
    public static string RemoveAccentsIgnoreCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return text.Replace('Á', 'A')
            .Replace('É', 'E')
            .Replace('Í', 'I')
            .Replace('Ó', 'O')
            .Replace('Ú', 'U')
            .Replace('ü', 'u')
            .Replace('Ü', 'U')
            .Replace('á', 'a')
            .Replace('é', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ú', 'u');
    }
}