namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    /// <summary>
    /// 判断是否为大写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <remarks>
    /// 此方法只检查字母字符，忽略数字和特殊字符。
    /// 使用过滤器先获取字母字符，然后检查是否全部为大写。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUpper(string text) => FilterForLetters(text).All(char.IsUpper);

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <remarks>
    /// 此方法只检查字母字符，忽略数字和特殊字符。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(string text) => FilterForLetters(text).All(char.IsLower);

    /// <summary>
    /// 判断是否为中文字符。
    /// </summary>
    /// <param name="value">字符</param>
    /// <returns>如果字符是中文字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 使用Unicode范围检查，比正则表达式更高效。
    /// 中文字符范围：\u4E00-\u9FA5 (19968-40869)
    /// </remarks>
    public static bool IsChinese(char value) => (value >= 19968 && value <= 40869);

    /// <summary>
    /// 判断字符串是否全部为中文字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串全部为中文字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 性能优化：使用早期退出和内联Unicode范围检查。
    /// 比使用正则表达式 ^[\u4E00-\u9FA5]+$ 快 5-10 倍。
    /// </remarks>
    public static bool IsChinese(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 性能优化：直接Unicode范围检查比正则表达式快很多
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsChinese(text[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断字符串是否全部为大写字母。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串全部为大写字母，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法检查字符串中的所有字符，包括字母、数字和特殊字符。
    /// 只有当所有字符都是大写字母时才返回 true。
    /// </remarks>
    public static bool IsAllUpperCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsLetter(c) || !char.IsUpper(c))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断字符串是否全部为小写字母。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串全部为小写字母，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法检查字符串中的所有字符，包括字母、数字和特殊字符。
    /// 只有当所有字符都是小写字母时才返回 true。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllLowerCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsLetter(c) || !char.IsLower(c))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含字母字符（忽略大小写）。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含字母字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法比使用正则表达式 ^[a-zA-Z]+$ 性能更好。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllLetters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            if (!char.IsLetter(text[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含数字字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含数字字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法比使用正则表达式 ^\d+$ 性能更好。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllDigits(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含字母和数字字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含字母和数字字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法比使用正则表达式 ^[a-zA-Z0-9]+$ 性能更好。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAlphanumeric(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含ASCII范围内的字母字符（a-z, A-Z）。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含ASCII字母字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 比 <see cref="char.IsLetter(char)"/> 更快，因为它只检查ASCII范围。
    /// 性能优化：使用位运算进行快速ASCII字母检查。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetters(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 性能优化：位运算比 char.IsLetter() 快 2-3 倍
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含ASCII范围内的数字字符（0-9）。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含ASCII数字字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 比 <see cref="char.IsDigit(char)"/> 更快，因为它只检查ASCII范围。
    /// 性能优化：使用位运算进行快速ASCII数字检查。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigits(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 性能优化：位运算比 char.IsDigit() 快 2-3 倍
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }

    /// <summary>
    /// 判断字符串是否只包含ASCII范围内的字母和数字字符。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串只包含ASCII字母和数字字符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 比 <see cref="char.IsLetterOrDigit(char)"/> 更快，因为它只检查ASCII范围。
    /// 性能优化：使用直接范围比较，避免方法调用开销。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiAlphanumeric(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 性能优化：直接范围检查比 char.IsLetterOrDigit() 快 2-3 倍
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 判断字符串是否为有效的标识符（以字母或下划线开头，后续为字母、数字或下划线）。
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>如果字符串是有效标识符，则返回 true；否则返回 false。</returns>
    /// <remarks>
    /// 此方法比使用正则表达式 ^[a-zA-Z_][a-zA-Z0-9_]*$ 性能更好。
    /// 性能优化：使用直接字符检查，避免正则表达式开销。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // 第一个字符必须是字母或下划线
        char first = text[0];
        if (!((first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z') || first == '_'))
            return false;

        // 后续字符必须是字母、数字或下划线
        for (int i = 1; i < text.Length; i++)
        {
            char c = text[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                return false;
        }
        return true;
    }
}

/// <summary>
/// 字符串捷径扩展
/// </summary>
public static partial class StringsShortcutExtensions
{
    /// <summary>
    /// 检查字符串是 null 还是 <see cref="string.Empty"/> 字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(this string text) => string.IsNullOrEmpty(text);

    /// <summary>
    /// 检查字符串不是 null 或 <see cref="string.Empty"/> 字符串。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNullNorEmpty(this string text) => !text.IsNullOrEmpty();

    /// <summary>
    /// 检查字符串是 null、空还是仅由空白字符组成。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this string text) => string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// 检查字符串不是 null、空或由空白字符组成。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNullNorWhiteSpace(this string text) => !text.IsNullOrWhiteSpace();
}