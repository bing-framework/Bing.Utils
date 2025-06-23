// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 确定字符串是否与指定集合中的任一字符串相等
    /// </summary>
    /// <param name="value">要比较的字符串</param>
    /// <param name="comparisonType">字符串比较的方式</param>
    /// <param name="values">要与之比较的字符串集合</param>
    /// <returns>
    /// 如果 <paramref name="value"/> 与 <paramref name="values"/> 中的任一字符串相等，则返回 true；否则返回 false。
    /// 如果 <paramref name="values"/> 为 null 或空集合，则返回 false。
    /// 比较使用指定的 <paramref name="comparisonType"/> 进行。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "hello", "world") => true
    /// "Hello".EqualsAnyOf(StringComparison.Ordinal, "hello", "world") => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAnyOf(this string value, StringComparison comparisonType, params string[] values) =>
        values != null && values.Length > 0 && values.Any(v => string.Equals(value, v, comparisonType));

    /// <summary>
    /// 确定字符串是否与指定集合中的任一字符串相等，忽略大小写
    /// </summary>
    /// <param name="text">要比较的字符串</param>
    /// <param name="values">要与之比较的字符串集合</param>
    /// <returns>
    /// 如果 <paramref name="text"/> 与 <paramref name="values"/> 中的任一字符串相等（忽略大小写），则返回 true；否则返回 false。
    /// 如果 <paramref name="values"/> 为 null 或空集合，则返回 false。
    /// </returns>
    /// <example>
    /// <code>
    /// "Hello".EqualsAnyOfIgnoreCase("hello", "world") => true
    /// "Hello".EqualsAnyOfIgnoreCase("HELLO", "WORLD") => true
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsAnyOfIgnoreCase(this string text, params string[] values) =>
        values != null && values.Length > 0 && values.Any(v => string.Equals(text, v, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 判断两个字符串是否相等，忽略大小写
    /// </summary>
    /// <param name="text">当前字符串</param>
    /// <param name="targetText">目标比较字符串</param>
    /// <returns>如果两个字符串内容相等（忽略大小写），则返回true；否则返回false</returns>
    /// <remarks>
    /// 此方法使用 <see cref="StringComparison.OrdinalIgnoreCase"/> 比较方式。
    /// 不会对字符串进行Trim处理，也不会特殊处理null值。
    /// </remarks>
    /// <example>
    /// <code>
    /// "Hello".EqualsIgnoreCase("hello") => true
    /// "Hello".EqualsIgnoreCase("HELLO") => true
    /// "Hello".EqualsIgnoreCase("world") => false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase(this string text, string targetText) =>
        string.Equals(text, targetText, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 使用固定区域性比较字符串是否相等（忽略大小写、移除首尾空白、处理null值）
    /// </summary>
    /// <param name="text">当前字符串</param>
    /// <param name="targetText">目标字符串</param>
    /// <returns>
    /// 如果字符串相等则返回 true，否则返回 false。
    /// </returns>
    /// <remarks>
    /// 此方法执行下列操作：
    /// 1. 使用 <see cref="StringComparison.InvariantCultureIgnoreCase"/> 忽略大小写和区域性
    /// 2. 对两个字符串进行 Trim 操作去除首尾空白字符
    /// 3. 特殊处理 null 值：当两个参数均为 null 时返回 true；仅有一个为 null 时返回 false
    /// 
    /// 适用于需要忽略格式和区域性进行内容比较的场景，如配置键名、常量值等。
    /// </remarks>
    /// <example>
    /// <code>
    /// "Hello".EqualsInvariant("hello") => true
    /// "  Hello  ".EqualsInvariant("hello") => true
    /// "Hello".EqualsInvariant("world") => false
    /// (null).EqualsInvariant(null) => true
    /// "Hello".EqualsInvariant(null) => false
    /// </code>
    /// </example>
    public static bool EqualsInvariant(this string text, string targetText) =>
        text.EqualsWithOptions(targetText, StringComparison.InvariantCultureIgnoreCase, trim: true, handleNull: true);

    /// <summary>
    /// 确定两个指定的字符串是否具有相同的值，使用指定的字符串比较规则（兼容性方法）
    /// </summary>
    /// <param name="text">当前字符串</param>
    /// <param name="targetText">目标比较字符串</param>
    /// <param name="comparison">比较规则，默认使用不区分区域性且忽略大小写的比较方式</param>
    /// <returns>如果两个字符串相等，则返回 true，否则返回 false</returns>
    /// <remarks>
    /// 此方法为兼容旧版本的API而保留。新代码建议使用功能更强大的 <see cref="EqualsWithOptions"/> 方法。
    /// 当比较 null 值时，只有当两个值都为 null 时才返回 true。
    /// </remarks>
    /// <seealso cref="EqualsWithOptions"/>
    public static bool EqualsTo(this string text, string targetText, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) =>
        EqualsWithOptions(text, targetText, comparison);

    /// <summary>
    /// 使用可定制选项比较两个字符串是否相等
    /// </summary>
    /// <param name="text">当前字符串</param>
    /// <param name="targetText">目标比较字符串</param>
    /// <param name="comparison">字符串比较规则，默认使用不区分区域性且忽略大小写的比较方式</param>
    /// <param name="trim">是否在比较前进行 Trim 操作，移除首尾空白</param>
    /// <param name="handleNull">是否特殊处理 null 值（两个都是 null 返回 true）</param>
    /// <returns>如果两个字符串相等，则返回 true，否则返回 false</returns>
    /// <remarks>
    /// 此方法提供了完整的字符串比较定制选项：
    /// <list type="bullet">
    /// <item><description>可选择任意 <see cref="StringComparison"/> 比较方式</description></item>
    /// <item><description>可选是否进行空白修剪 <paramref name="trim"/></description></item>
    /// <item><description>可选是否特殊处理 null 值 <paramref name="handleNull"/></description></item>
    /// </list>
    /// 
    /// <para>适合需要精确控制比较条件的场景。对于常见比较场景，请考虑使用更具体的方法：</para>
    /// <list type="bullet">
    /// <item><description>简单忽略大小写比较：<see cref="EqualsIgnoreCase"/></description></item>
    /// <item><description>标准化比较（忽略大小写、区域性、空白）：<see cref="EqualsInvariant"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本用法 - 默认使用 InvariantCultureIgnoreCase
    /// "Hello".EqualsWithOptions("hello") => true
    /// 
    /// // 区分大小写比较
    /// "Hello".EqualsWithOptions("hello", StringComparison.Ordinal) => false
    /// "Hello".EqualsWithOptions("Hello", StringComparison.Ordinal) => true
    /// 
    /// // 处理空白
    /// "  Hello  ".EqualsWithOptions("hello", trim: true) => true
    /// "  Hello  ".EqualsWithOptions("hello", trim: false) => false
    /// 
    /// // 处理null值
    /// (null).EqualsWithOptions(null, handleNull: true) => true
    /// (null).EqualsWithOptions("", handleNull: true) => false
    /// "".EqualsWithOptions(null, handleNull: true) => false
    /// 
    /// // 组合使用选项
    /// "  Hello  ".EqualsWithOptions("hello", StringComparison.OrdinalIgnoreCase, trim: true, handleNull: true) => true
    /// </code>
    /// </example>
    public static bool EqualsWithOptions(this string text, string targetText, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase, bool trim = false, bool handleNull = false)
    {
        // 快速路径：如果引用相同，直接返回true
        if (ReferenceEquals(text, targetText))
            return true;

        // 处理 null 值的特殊情况
        if (handleNull)
        {
            if (text == null && targetText == null)
                return true;
            if (text == null || targetText == null)
                return false;
        }
        else if (text == null || targetText == null)
        {
            // 当不特殊处理null时，任一为null都使用普通string.Equals行为
            return string.Equals(text, targetText);
        }

        // 进行 Trim 操作
        if (trim)
        {
            text = text?.Trim();
            targetText = targetText?.Trim();

            // Trim后可能相等，再次尝试快速路径
            if (ReferenceEquals(text, targetText))
                return true;
        }

        // 比较字符串
        return string.Equals(text, targetText, comparison);
    }
}