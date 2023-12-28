namespace Bing.Text;

/// <summary>
/// 辅助字符串帮助类
/// </summary>
internal static class AuxiliaryStringHelper
{
    /// <summary>
    /// 合并
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="t">对象</param>
    /// <param name="ts">对象集合</param>
    public static IEnumerable<T> M<T>(T t, IEnumerable<T> ts)
    {
        yield return t;
        if (ts is not null)
            foreach (var t0 in ts)
                yield return t0;
    }
}

/// <summary>
/// 差异字符串帮助类
/// </summary>
internal static class DiffStringHelper
{
    /// <summary>
    /// 检查参数长度
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="check">待检查的字符串</param>
    /// <param name="ret">返回长度</param>
    /// <param name="times">循环次数</param>
    public static bool CheckLengthForParams(string text, string check, out int ret, out int times)
    {
        times = 0;
        ret = -1;
        if (text is null && check is null)
            return false;

        ret = 0;
        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(check))
            return false;

        var safeText = Strings.NullToEmpty(text);
        var safeCheck = Strings.NullToEmpty(check);
        ret = Math.Abs(safeText.Length - safeCheck.Length);

        if (string.IsNullOrEmpty(safeText) || string.IsNullOrEmpty(safeCheck))
            return false;
        times = safeText.Length > safeCheck.Length ? safeCheck.Length : safeText.Length;
        return ret > -1;
    }
}

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    #region Count

    /// <summary>
    /// 返回字符串中所包含字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLetters(string text) => string.IsNullOrEmpty(text) ? 0 : FilterForLetters(text).Count();

    /// <summary>
    /// 返回字符串中所包含大写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLettersUpperCase(string text) => string.IsNullOrEmpty(text) ? 0 : FilterForLetters(text).Where(char.IsUpper).Count();

    /// <summary>
    /// 返回字符串中所包含小写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLetterLowerCase(string text) => string.IsNullOrEmpty(text) ? 0 : FilterForLetters(text).Where(char.IsLower).Count();

    /// <summary>
    /// 返回字符串中所包含数字的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForNumbers(string text) => string.IsNullOrEmpty(text) ? 0 : FilterForNumbers(text).Count();

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(string text, char toCheck) => CountOccurrences(text, toCheck.ToString());

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    public static int CountOccurrences(string text, string toCheck)
    {
        if (string.IsNullOrEmpty(toCheck))
            return 0;
        int res = 0, offset = 0;
        while ((offset = text.IndexOf(toCheck, offset, StringComparison.Ordinal)) != -1)
        {
            offset += toCheck.Length;
            res++;
        }
        return res;
    }

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrencesIgnoreCase(string text, char toCheck) => CountOccurrencesIgnoreCase(text, toCheck.ToString());

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    public static int CountOccurrencesIgnoreCase(string text, string toCheck)
    {
        if (string.IsNullOrEmpty(toCheck))
            return 0;
        int res = 0, offset = 0;
        while ((offset = text.IndexOf(toCheck, offset, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            offset += toCheck.Length;
            res++;
        }
        return res;
    }

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(string text, char toCheck, IgnoreCase @case)
    {
        return @case switch
        {
            IgnoreCase.True => CountOccurrencesIgnoreCase(text, toCheck),
            IgnoreCase.False => CountOccurrences(text, toCheck),
            _ => CountOccurrences(text, toCheck)
        };
    }

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(string text, string toCheck, IgnoreCase @case)
    {
        return @case switch
        {
            IgnoreCase.True => CountOccurrencesIgnoreCase(text, toCheck),
            IgnoreCase.False => CountOccurrences(text, toCheck),
            _ => CountOccurrences(text, toCheck)
        };
    }

    /// <summary>
    /// 比较字符串，获取不相等字符的数量。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    public static int CountForDiffChars(string text, string toCheck)
    {
        if (!DiffStringHelper.CheckLengthForParams(text, toCheck, out var ret, out var times))
            return ret;
        for (var i = 0; i < times; i++)
            if (text[i] != toCheck[i])
                ret++;
        return ret;
    }

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    public static int CountForDiffCharsIgnoreCase(string text, string toCheck)
    {
        if (!DiffStringHelper.CheckLengthForParams(text, toCheck, out var ret, out var times))
            return ret;
        for (var i = 0; i < times; i++)
            if (!text[i].EqualsIgnoreCase(toCheck[i]))
                ret++;
        return ret;
    }

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffChars(string text, string toCheck, IgnoreCase @case)
    {
        return @case switch
        {
            IgnoreCase.True => CountForDiffCharsIgnoreCase(text, toCheck),
            IgnoreCase.False => CountForDiffChars(text, toCheck),
            _ => CountForDiffChars(text, toCheck)
        };
    }

    #endregion

    #region Empty

    /// <summary>
    /// 将 null 转换为 Empty
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AvoidNull(string text) => text ?? string.Empty;

    /// <summary>
    /// 将 null 转换为 Empty
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NullToEmpty(string text) => AvoidNull(text);

    /// <summary>
    /// 将 Empty 转换为 null
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EmptyToNull(string text) => string.IsNullOrEmpty(text) ? null : text;

    #endregion

    #region Filter

    /// <summary>
    /// 过滤为字符
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="predicate">条件</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<char> FilterByChar(string text, Func<char, bool> predicate) => text.ToCharArray().Where(predicate);

    /// <summary>
    /// 只获取字母和数字
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<char> FilterForNumbersAndLetters(string text)
    {
        return FilterByChar(text, LocalCheck);

        bool LocalCheck(char @char) => (@char >= 'a' && @char <= 'z') || (@char >= 'A' && @char <= 'Z') || (@char >= '0' && @char <= '9');
    }

    /// <summary>
    /// 只获取数字
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<char> FilterForNumbers(string text)
    {
        return FilterByChar(text, LocalCheck);

        bool LocalCheck(char @char) => @char >= '0' && @char <= '9';
    }

    /// <summary>
    /// 只获取字母
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<char> FilterForLetters(string text)
    {
        return FilterByChar(text, LocalCheck);

        bool LocalCheck(char @char) => (@char >= 'a' && @char <= 'z') || (@char >= 'A' && @char <= 'Z');
    }

    #endregion

    #region Format With

    /// <summary>
    /// 将指定字符串中的格式项替换为指定数组中相应对象的字符串表示形式。
    /// </summary>
    /// <param name="format">字符串格式，占位符以{n}表示</param>
    /// <param name="args">用于填充占位符的参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatWith(string format, params object[] args) => string.Format(format, args);

    /// <summary>
    /// 将指定字符串中的格式项替换为指定数组中相应对象的字符串表示形式，参数提供区域性特定的格式设置信息。
    /// </summary>
    /// <param name="format">字符串格式，占位符以{n}表示</param>
    /// <param name="provider">区域性特定的格式设置信息</param>
    /// <param name="args">用于填充占位符的参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatWith(string format, IFormatProvider provider, params object[] args) => string.Format(provider, format, args);

    #endregion

    #region Get

    /// <summary>
    /// 只获取字母和数字。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNumbersAndLetters(string text) => Merge(FilterForNumbersAndLetters(text));

    /// <summary>
    /// 只获取数字。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNumbers(string text) => Merge(FilterForNumbers(text));

    /// <summary>
    /// 只获取字母。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLetters(string text) => Merge(FilterForLetters(text));

    #endregion

    #region Left and Right

    /// <summary>
    /// 从左向右截取字符串。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static string Left(string text, int length)
    {
        if (length < 0)
            throw new ArgumentException("Length 必须大于0", nameof(length));
        if (length == 0 || text is null)
            return string.Empty;
        return length >= text.Length ? text : text.Substring(0, length);
    }

    /// <summary>
    /// 从右向左截取字符串。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentException"></exception>
    public static string Right(string text, int length)
    {
        if (length < 0)
            throw new ArgumentException("Length 必须大于0", nameof(length));
        if (length == 0 || text is null)
            return string.Empty;
        var strLength = text.Length;
        return length >= strLength ? text : text.Substring(strLength - length, length);
    }

    #endregion

    #region Merge

    /// <summary>
    /// 将字符集合合并为一个字符串
    /// </summary>
    /// <param name="chars">字符集合</param>
    public static string Merge(IEnumerable<char> chars)
    {
        var builder = new StringBuilder();
        if (chars is not null)
            foreach (var val in chars)
                builder.Append(val);
        return builder.ToString();
    }

    /// <summary>
    /// 将字符串集合合并为一个字符串
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="strings">字符集合</param>
    public static string Merge(string text, params string[] strings)
    {
        var builder = new StringBuilder();
        builder.Append(text);
        if (strings is not null)
            foreach (var val in strings)
                builder.Append(val);
        return builder.ToString();
    }

    /// <summary>
    /// 将字符串集合合并为一个字符串
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="string">字符串</param>
    /// <param name="strings">字符串集合</param>
    public static string Merge(string text, string @string, params string[] strings)
    {
        var builder = new StringBuilder();
        builder.Append(text);
        foreach (var val in AuxiliaryStringHelper.M(@string, strings))
            builder.Append(val);
        return builder.ToString();
    }

    /// <summary>
    /// 将字符集合合并为一个字符串
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="char">字符</param>
    /// <param name="chars">字符集合</param>
    public static string Merge(string text, char @char, params char[] chars)
    {
        var builder = new StringBuilder();
        builder.Append(text);
        foreach (var val in AuxiliaryStringHelper.M(@char, chars))
            builder.Append(val);
        return builder.ToString();
    }

    #endregion

    #region Remove

    /// <summary>
    /// 从字符串中移除给定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeText">移除字符串</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Remove(string text, string removeText, IgnoreCase @case = IgnoreCase.False)
    {
        return @case.X()
            ? text.Replace(removeText, string.Empty, StringComparison.OrdinalIgnoreCase)
            : text.Replace(removeText, string.Empty);
    }

    /// <summary>
    /// 移除所有指定的字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="toRemove">移除字符集合</param>
    public static string RemoveChars(string text, params char[] toRemove)
    {
        var builder = new StringBuilder(text);
        foreach (var remove in toRemove)
            builder.Replace(remove, char.MinValue);
        builder.Replace(char.MinValue.ToString(), string.Empty);
        return builder.ToString();
    }

    /// <summary>
    /// 移除所有空格。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveWhiteSpace(string text) => RemoveChars(text, ' ');

    /// <summary>
    /// 从字符串中移除所有重复的空格。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveDuplicateWhiteSpaces(string text) => RemoveDuplicateChar(text, ' ');

    /// <summary>
    /// 从字符串中移除所有重复的字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="charRemove">移除字符</param>
    /// <param name="case">忽略大小写选项</param>
    public static string RemoveDuplicateChar(string text, char charRemove, IgnoreCase @case = IgnoreCase.False)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        var builder = new StringBuilder();
        int index = 0, offset, length = text.Length;

        Func<char, char, bool> equals = @case.X()
            ? (l, r) => l.EqualsIgnoreCase(r)
            : (l, r) => l == r;

        while (true)
        {
            if (index >= length)
                break;
            var @char = text[index];
            if (!equals(@char, charRemove))
            {
                builder.Append(@char);
                index++;
            }
            else
            {
                builder.Append(charRemove);
                UpdateOffset();
                index += offset;
            }
        }

        return builder.ToString();

        void UpdateOffset()
        {
            offset = 0;
            while (IsMatchedNextChar()) ++offset;
        }

        bool IsMatchedNextChar()
        {
            if (index + offset >= length)
                return false;
            return text[index + offset] == charRemove;
        }
    }

    /// <summary>
    /// 从给定的位置开始移除所有字符，位置从 0 开始计算。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="indexOfStartToRemove">索引</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(string text, int indexOfStartToRemove)
    {
        return indexOfStartToRemove <= 0 ? text : text[..indexOfStartToRemove];
    }

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    public static string RemoveSince(string text, string removeFromThis)
    {
        if (string.IsNullOrEmpty(removeFromThis))
            return text;
        var index = text.IndexOf(removeFromThis, StringComparison.Ordinal);
        return RemoveSince(text, index);
    }

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符，并忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    public static string RemoveSinceIgnoreCase(string text, string removeFromThis)
    {
        if (string.IsNullOrEmpty(removeFromThis))
            return text;
        var index = text.IndexOf(removeFromThis, StringComparison.OrdinalIgnoreCase);
        return RemoveSince(text, index);
    }

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(string text, string removeFromThis, IgnoreCase @case)
    {
        return @case switch
        {
            IgnoreCase.True => RemoveSinceIgnoreCase(text, removeFromThis),
            IgnoreCase.False => RemoveSince(text, removeFromThis),
            _ => RemoveSince(text, removeFromThis)
        };
    }

    /// <summary>
    /// 移除起始字符串
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="start">要移除的值</param>
    public static string RemoveStart(string value, string start)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (string.IsNullOrEmpty(start))
            return value;
        if (value.StartsWith(start, StringComparison.Ordinal) == false)
            return value;
        return value.Substring(start.Length, value.Length - start.Length);
    }

    /// <summary>
    /// 移除末尾字符串
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="end">要移除的值</param>
    public static string RemoveEnd(string value, string end)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (string.IsNullOrEmpty(end))
            return value;
        if (value.EndsWith(end, StringComparison.Ordinal) == false)
            return value;
        return value.Substring(0, value.LastIndexOf(end, StringComparison.Ordinal));
    }

    #endregion

    #region Repeat

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="text">值</param>
    /// <param name="repeatTimes">重复次数</param>
    public static string Repeat(string text, int repeatTimes)
    {
        if (repeatTimes < 0)
            return string.Empty;
        if (string.IsNullOrEmpty(text) || repeatTimes == 0)
            return string.Empty;
        if (text.Length == 1)
            return new string(text[0], repeatTimes);
        switch (repeatTimes)
        {
            case 1:
                return text;
            case 2:
                return string.Concat(text, text);
            case 3:
                return string.Concat(text, text, text);
            case 4:
                return string.Concat(text, text, text, text);
        }
        var sb = new StringBuilder(text.Length * repeatTimes);
        for (var i = 0; i < repeatTimes; i++)
            sb.Append(text);
        return sb.ToString();
    }

    #endregion

    #region Prefix / Suffix

    /// <summary>
    /// 从左到右，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CommonPrefix(string left, string right) => CommonPrefix(left, right, out _);

    /// <summary>
    /// 从左到右，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    /// <param name="len">长度</param>
    public static string CommonPrefix(string left, string right, out int len)
    {
        len = 0;
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;
        var rangeTimes = left.Length < right.Length ? left.Length : right.Length;
        for (var i = 0; i < rangeTimes; i++, len++)
            if (left[i] != right[i])
                break;
        return left.Left(len);
    }

    /// <summary>
    /// 从右到左，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CommonSuffix(string left, string right) => CommonSuffix(left, right, out _);

    /// <summary>
    /// 从右到左，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    /// <param name="len">长度</param>
    public static string CommonSuffix(string left, string right, out int len)
    {
        len = 0;
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Empty;
        var rangeTimes = left.Length < right.Length ? left.Length : right.Length;
        int leftPointer = left.Length - 1, rightPointer = right.Length - 1;
        for (var i = 0; i < rangeTimes; i++, len++, leftPointer--, rightPointer--)
            if (left[leftPointer] != right[rightPointer])
                break;
        return left.Right(len);
    }

    #endregion

    #region Convert

    /// <summary>
    /// 将字节数组转换为字符串，去除可能存在的BOM（字节顺序标记）
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="encoding">字符编码。如果未提供，则默认使用 UTF-8 编码</param>
    public static string ConvertFromBytesWithoutBom(byte[] bytes, Encoding encoding = null)
    {
        if (bytes == null)
            return null;
        encoding ??= Encoding.UTF8;
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        if (hasBom)
            return encoding.GetString(bytes, 3, bytes.Length - 3);
        return encoding.GetString(bytes);
    }

    #endregion
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    #region Count

    /// <summary>
    /// 返回字符串中所包含字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLetters(this string text) => Strings.CountForLetters(text);

    /// <summary>
    /// 返回字符串中所包含大写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLettersUpperCase(this string text) => Strings.CountForLettersUpperCase(text);

    /// <summary>
    /// 返回字符串中所包含小写字母的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForLetterLowerCase(this string text) => Strings.CountForLetterLowerCase(text);

    /// <summary>
    /// 返回字符串中所包含数字的数量。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForNumbers(this string text) => Strings.CountForNumbers(text);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, char toCheck) => Strings.CountOccurrences(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, string toCheck) => Strings.CountOccurrences(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrencesIgnoreCase(this string text, char toCheck) => Strings.CountOccurrencesIgnoreCase(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrencesIgnoreCase(this string text, string toCheck) => Strings.CountOccurrencesIgnoreCase(text, toCheck);

    /// <summary>
    /// 计算给定字符串中有多少个指定的字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, char toCheck, IgnoreCase @case) => Strings.CountOccurrences(text, toCheck, @case);

    /// <summary>
    /// 计算给定字符串中有多少个指定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountOccurrences(this string text, string toCheck, IgnoreCase @case) => Strings.CountOccurrences(text, toCheck, @case);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffChars(this string text, string toCheck) => Strings.CountForDiffChars(text, toCheck);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffCharsIgnoreCase(this string text, string toCheck) => Strings.CountForDiffCharsIgnoreCase(text, toCheck);

    /// <summary>
    /// 比较字符串，获取不相等字符的数量，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="case">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountForDiffChars(this string text, string toCheck, IgnoreCase @case) => Strings.CountForDiffChars(text, toCheck, @case);

    #endregion

    #region Equals

    /// <summary>
    /// 相等判断，忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="targetText">目标文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase(this string text, string targetText) => string.Equals(text, targetText, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 相等判断，只要给定的字符串与字符串集合中任意一个值相等，就返回 True，忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="targetTexts">目标文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsToAnyIgnoreCase(this string text, params string[] targetTexts) =>
        targetTexts != null && targetTexts.Any(t => string.Equals(text, t, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region Filter

    /// <summary>
    /// 过滤为字符
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="predicate">条件</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<char> Where(this string text, Func<char, bool> predicate) => Strings.FilterByChar(text, predicate);

    #endregion

    #region Format With

    /// <summary>
    /// 将指定字符串中的格式项替换为指定数组中相应对象的字符串表示形式。
    /// </summary>
    /// <param name="format">字符串格式，占位符以{n}表示</param>
    /// <param name="args">用于填充占位符的参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatWith(this string format, params object[] args) => Strings.FormatWith(format, args);

    /// <summary>
    /// 将指定字符串中的格式项替换为指定数组中相应对象的字符串表示形式，参数提供区域性特定的格式设置信息。
    /// </summary>
    /// <param name="format">字符串格式，占位符以{n}表示</param>
    /// <param name="provider">区域性特定的格式设置信息</param>
    /// <param name="args">用于填充占位符的参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatWith(this string format, IFormatProvider provider, params object[] args) => Strings.FormatWith(format, provider, args);

    #endregion

    #region Left and Right

    /// <summary>
    /// 从左向右截取字符串。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Left(this string text, int length) => Strings.Left(text, length);

    /// <summary>
    /// 从右向左截取字符串。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text, int length) => Strings.Right(text, length);

    #endregion

    #region Remove

    /// <summary>
    /// 从字符串中移除给定的子字符串，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeText">移除字符串</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Remove(this string text, string removeText, IgnoreCase @case = IgnoreCase.False) => Strings.Remove(text, removeText, @case);

    /// <summary>
    /// 移除所有指定的字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="toRemove">移除字符集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveChars(this string text, params char[] toRemove) => Strings.RemoveChars(text, toRemove);

    /// <summary>
    /// 移除所有空格。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveWhiteSpace(this string text) => Strings.RemoveWhiteSpace(text);

    /// <summary>
    /// 从字符串中移除所有重复的空格。
    /// </summary>
    /// <param name="text">文本</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveDuplicateWhiteSpaces(this string text) => Strings.RemoveDuplicateWhiteSpaces(text);

    /// <summary>
    /// 从字符串中移除所有重复的字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="charRemove">移除字符</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveDuplicateChar(this string text, char charRemove, IgnoreCase @case = IgnoreCase.False) => Strings.RemoveDuplicateChar(text, charRemove, @case);

    /// <summary>
    /// 从给定的位置开始移除所有字符，位置从 0 开始计算。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="indexOfStartToRemove">索引</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(this string text, int indexOfStartToRemove) => Strings.RemoveSince(text, indexOfStartToRemove);

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(this string text, string removeFromThis) => Strings.RemoveSince(text, removeFromThis);

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符，并忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSinceIgnoreCase(this string text, string removeFromThis) => Strings.RemoveSinceIgnoreCase(text, removeFromThis);

    /// <summary>
    /// 根据给定子字符串在字符串中的位置，移除该位置之后的所有字符，根据给定的 <see cref="IgnoreCase"/> 选项来决定是否忽略大小写。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="removeFromThis">移除位置的字符串</param>
    /// <param name="case">忽略大小写选项</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveSince(string text, string removeFromThis, IgnoreCase @case) => Strings.RemoveSince(text, removeFromThis, @case);

    /// <summary>
    /// 移除起始字符串
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="start">要移除的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveStart(this string value, string start) => Strings.RemoveStart(value, start);

    /// <summary>
    /// 移除末尾字符串
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="end">要移除的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string RemoveEnd(this string value, string end) => Strings.RemoveEnd(value, end);

    #endregion

    #region Repeat

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="text">值</param>
    /// <param name="repeatTimes">重复次数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Repeat(this string text, int repeatTimes) => Strings.Repeat(text, repeatTimes);

    #endregion

    #region Prefix / Suffix

    /// <summary>
    /// 从左到右，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CommonPrefix(this string left, string right) => Strings.CommonPrefix(left, right);

    /// <summary>
    /// 从左到右，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    /// <param name="len">长度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CommonPrefix(this string left, string right, out int len) => Strings.CommonPrefix(left, right, out len);

    /// <summary>
    /// 从右到左，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CommonSuffix(this string left, string right) => Strings.CommonSuffix(left, right);

    /// <summary>
    /// 从右到左，返回共有的字符，直至遇到第一个不同的字符。
    /// </summary>
    /// <param name="left">比较字符串</param>
    /// <param name="right">比较字符串</param>
    /// <param name="len">长度</param>
    public static string CommonSuffix(this string left, string right, out int len) => Strings.CommonSuffix(left, right, out len);

    #endregion
}

/// <summary>
/// 字符串捷径扩展
/// </summary>
public static partial class StringsShortcutExtensions
{
    #region Encoding

    /// <summary>
    /// 将字符串转换为字节数组
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="encoding">字符编码。默认编码为：<see cref="Encoding.UTF8"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToBytes(this string value, Encoding encoding = null)
    {
        return value is null
            ? throw new ArgumentNullException(nameof(value))
            : (encoding ?? Encoding.UTF8).GetBytes(value);
    }

    /// <summary>
    /// 将字节数组转换为字符串
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="encoding">字符编码。默认编码为：<see cref="Encoding.UTF8"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetString(this byte[] bytes, Encoding encoding = null)
    {
        return bytes is null
            ? throw new ArgumentNullException(nameof(bytes))
            : (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    #endregion

    #region Index

    /// <summary>
    /// 查找给定子字符串位于字符串的位置，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfIgnoreCase(this string text, string toCheck)
    {
        return text.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 查找给定子字符串位于字符串的位置，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="startIndex">起始索引</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfIgnoreCase(this string text, int startIndex, string toCheck)
    {
        return text.IndexOf(toCheck, startIndex, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 查找给定子字符串位于字符串的最后的位置，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfIgnoreCase(this string text, string toCheck)
    {
        return text.LastIndexOf(toCheck, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 查找给定子字符串位于字符串的最后的位置，忽略大小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="startIndex">起始索引</param>
    /// <param name="count">计数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfIgnoreCase(this string text, string toCheck, int startIndex, int count)
    {
        return text.LastIndexOf(toCheck, startIndex, count, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 查找给定子字符串集合中，最靠结尾的那个字字符串的位置。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static int LastIndexOfAny(this string text, params string[] toCheck)
    {
        if (toCheck is null || toCheck.Length == 0)
            throw new ArgumentNullException(nameof(toCheck), $"The parameter '{nameof(toCheck)}' cannot be null or empty.");
        var res = -1;
        foreach (var checking in toCheck)
        {
            var index = text.LastIndexOf(checking, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index > res)
                res = index;
        }
        return res;
    }

    /// <summary>
    /// 查找给定短语在字符串中的位置。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="toCheck">待检查的字符串</param>
    /// <param name="startIndex">起始索引</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static int IndexOfWholePhrase(this string text, string toCheck, int startIndex = 0)
    {
        if (string.IsNullOrEmpty(toCheck))
            throw new ArgumentNullException(nameof(toCheck), $"The parameter '{nameof(toCheck)}' cannot be null or empty.");
        while (startIndex <= text.Length)
        {
            var index = text.IndexOfIgnoreCase(startIndex, toCheck);
            if (index < 0)
                return -1;
            var indexPreviousCar = index - 1;
            var indexNextCar = index + toCheck.Length;
            if ((index == 0 || !char.IsLetter(text[indexPreviousCar])) &&
                (index + toCheck.Length == text.Length || !char.IsLetter(text[indexNextCar])))
                return index;
            startIndex = index + toCheck.Length;
        }
        return -1;
    }

    #endregion

    #region Substrig

    /// <summary>
    /// 根据给定的子字符串在字符串中的位置开始截取。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="from">开始字符串</param>
    public static string SubstringSince(this string text, string from)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var index = text.IndexOfIgnoreCase(from);
        return index < 0 ? string.Empty : text[index..];
    }

    /// <summary>
    /// 根据给定的子字符串在字符串中的位置，截取至该位置。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="to">结尾字符串</param>
    public static string SubstringTo(this string text, string to)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var index = text.IndexOfIgnoreCase(to);
        return index < 0 ? string.Empty : text[..index];
    }

    #endregion

    #region Trim

    /// <summary>
    /// 移除所有空格。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimInner(this string text) => Strings.RemoveWhiteSpace(text);

    /// <summary>
    /// 对所有给定的字符串进行 Trim 操作
    /// </summary>
    /// <param name="texts">字符串集合</param>
    public static void TrimAll(this IList<string> texts)
    {
        if (texts is null)
            return;
        for (var i = 0; i < texts.Count; i++)
            texts[i] = texts[i].Trim();
    }

    /// <summary>
    /// 根据给定的短语，对字符串两端进行 Trim 操作。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="phrase">待修剪字词组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TrimPhrase(this string text, string phrase)
    {
        return text.TrimPhraseStart(phrase).TrimPhraseEnd(phrase);
    }

    /// <summary>
    /// 根据给定的短语，对字符串开始端进行 Trim 操作。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="phrase">待修剪词组</param>
    public static string TrimPhraseStart(this string text, string phrase)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (string.IsNullOrEmpty(phrase))
            return text;
        while (text.StartsWith(phrase))
            text = text[phrase.Length..];
        return text;
    }

    /// <summary>
    /// 根据给定的短语，对字符串结尾端进行 Trim 操作。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="phrase">待修剪词组</param>
    public static string TrimPhraseEnd(this string text, string phrase)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (string.IsNullOrEmpty(phrase))
            return text;
        while (text.EndsWithIgnoreCase(phrase))
            text = text[..^phrase.Length];
        return text;
    }

    #endregion
}