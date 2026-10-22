using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bing.Helpers.Internals;

/// <summary>
/// 为目标枚举类型提供非异常解析入口。
/// </summary>
/// <typeparam name="T">目标枚举或可空枚举类型。</typeparam>
internal static class ConvEnumParser<T>
{
    /// <summary>
    /// 目标类型对应的解析委托。
    /// </summary>
    private static readonly Parser Parse = CreateParser();

    /// <summary>
    /// 尝试解析枚举文本。
    /// </summary>
    /// <param name="text">待解析的文本。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    internal static bool TryParse(string text, out T result) => Parse(text, out result);

#if !NETSTANDARD2_0
    /// <summary>
    /// 尝试解析枚举文本片段。
    /// </summary>
    /// <param name="text">待解析的文本片段。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    internal static bool TryParse(ReadOnlySpan<char> text, out T result) => SpanParse.Parse(text, out result);

    /// <summary>
    /// 延迟初始化列表使用的枚举片段解析器。
    /// </summary>
    private static class SpanParse
    {
        /// <summary>
        /// 目标类型对应的片段解析委托。
        /// </summary>
        internal static readonly SpanParser Parse = CreateParser();

        /// <summary>
        /// 创建当前目标类型的片段解析委托。
        /// </summary>
        /// <returns>片段解析委托。</returns>
        private static SpanParser CreateParser()
        {
            var enumType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var methodName = enumType == typeof(T) ? nameof(ParseEnumSpan) : nameof(ParseNullableEnumSpan);
            var method = typeof(ConvEnumParser<T>).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            return (SpanParser)method.MakeGenericMethod(enumType).CreateDelegate(typeof(SpanParser));
        }
    }

    /// <summary>
    /// 解析非可空枚举文本片段。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="text">待解析的文本片段。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private static bool ParseEnumSpan<TEnum>(ReadOnlySpan<char> text, out T result) where TEnum : struct, System.Enum
    {
        if (System.Enum.TryParse<TEnum>(text, true, out var value))
        {
            result = Unsafe.As<TEnum, T>(ref value);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// 解析可空枚举文本片段。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="text">待解析的文本片段。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private static bool ParseNullableEnumSpan<TEnum>(ReadOnlySpan<char> text, out T result) where TEnum : struct, System.Enum
    {
        if (System.Enum.TryParse<TEnum>(text, true, out var value))
        {
            TEnum? nullable = value;
            result = Unsafe.As<TEnum?, T>(ref nullable);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// 解析枚举文本片段。
    /// </summary>
    /// <param name="text">待解析的文本片段。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private delegate bool SpanParser(ReadOnlySpan<char> text, out T result);
#endif

    /// <summary>
    /// 创建当前目标类型的解析委托。
    /// </summary>
    /// <returns>解析委托。</returns>
    private static Parser CreateParser()
    {
        var enumType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var methodName = enumType == typeof(T) ? nameof(ParseEnum) : nameof(ParseNullableEnum);
        var method = typeof(ConvEnumParser<T>).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        return (Parser)method.MakeGenericMethod(enumType).CreateDelegate(typeof(Parser));
    }

    /// <summary>
    /// 解析非可空枚举。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="text">待解析的文本。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private static bool ParseEnum<TEnum>(string text, out T result) where TEnum : struct, System.Enum
    {
        if (System.Enum.TryParse<TEnum>(text, true, out var value))
        {
            result = Unsafe.As<TEnum, T>(ref value);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// 解析可空枚举。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="text">待解析的文本。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private static bool ParseNullableEnum<TEnum>(string text, out T result) where TEnum : struct, System.Enum
    {
        if (System.Enum.TryParse<TEnum>(text, true, out var value))
        {
            TEnum? nullable = value;
            result = Unsafe.As<TEnum?, T>(ref nullable);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// 解析当前枚举类型文本。
    /// </summary>
    /// <param name="text">待解析的文本。</param>
    /// <param name="result">解析结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>。</returns>
    private delegate bool Parser(string text, out T result);
}
