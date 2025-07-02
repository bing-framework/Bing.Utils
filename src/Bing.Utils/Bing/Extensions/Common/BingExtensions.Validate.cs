using System.Collections;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 系统扩展 - 验证
/// </summary>
public static partial class BingExtensions
{
    #region CheckNull(检查对象是否为null)

    /// <summary>
    /// 检查对象是否为 <c>null</c>，为 <c>null</c> 则抛出<see cref="ArgumentNullException"/>异常。
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="parameterName">参数名</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void CheckNull(this object obj, string parameterName)
    {
        if (obj == null)
            throw new ArgumentNullException(parameterName);
    }

    #endregion

    #region IsEmpty(是否为空)

    /// <summary>
    /// 判断布尔值是否为空（false）
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <returns>如果值为 false，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this bool value) => value == false;

    /// <summary>
    /// 判断可空布尔值是否为空（null 或 false）
    /// </summary>
    /// <param name="value">可空布尔值</param>
    /// <returns>如果值为 null 或 false，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this bool? value) => value is null or false;

    /// <summary>
    /// 判断整数值是否为空（0）
    /// </summary>
    /// <param name="value">整数值</param>
    /// <returns>如果值等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// int zero = 0;
    /// int nonZero = 42;
    /// bool result1 = zero.IsEmpty();     // 返回 true
    /// bool result2 = nonZero.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this int value) => value == 0;

    /// <summary>
    /// 判断可空整数值是否为空（null 或 0）
    /// </summary>
    /// <param name="value">可空整数值</param>
    /// <returns>如果值为 null 或等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// int? nullValue = null;
    /// int? zero = 0;
    /// int? nonZero = 42;
    /// bool result1 = nullValue.IsEmpty(); // 返回 true
    /// bool result2 = zero.IsEmpty();      // 返回 true
    /// bool result3 = nonZero.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this int? value) => value is null or 0;

    /// <summary>
    /// 判断长整数值是否为空（0）
    /// </summary>
    /// <param name="value">长整数值</param>
    /// <returns>如果值等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// long zero = 0L;
    /// long nonZero = 1000000000000L;
    /// bool result1 = zero.IsEmpty();     // 返回 true
    /// bool result2 = nonZero.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this long value) => value == 0;

    /// <summary>
    /// 判断可空长整数值是否为空（null 或 0）
    /// </summary>
    /// <param name="value">可空长整数值</param>
    /// <returns>如果值为 null 或等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// long? nullValue = null;
    /// long? zero = 0L;
    /// long? nonZero = 1000000000000L;
    /// bool result1 = nullValue.IsEmpty(); // 返回 true
    /// bool result2 = zero.IsEmpty();      // 返回 true
    /// bool result3 = nonZero.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this long? value) => value is null or 0;

    /// <summary>
    /// 判断单精度浮点数是否为空（0）
    /// </summary>
    /// <param name="value">单精度浮点数值</param>
    /// <returns>如果值等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// float zero = 0f;
    /// float nonZero = 3.14f;
    /// bool result1 = zero.IsEmpty();     // 返回 true
    /// bool result2 = nonZero.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this float value) => value == 0;

    /// <summary>
    /// 判断可空单精度浮点数是否为空（null 或 0）
    /// </summary>
    /// <param name="value">可空单精度浮点数值</param>
    /// <returns>如果值为 null 或等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// float? nullValue = null;
    /// float? zero = 0f;
    /// float? nonZero = 3.14f;
    /// bool result1 = nullValue.IsEmpty(); // 返回 true
    /// bool result2 = zero.IsEmpty();      // 返回 true
    /// bool result3 = nonZero.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this float? value) => value is null or 0;

    /// <summary>
    /// 判断双精度浮点数是否为空（0）
    /// </summary>
    /// <param name="value">双精度浮点数值</param>
    /// <returns>如果值等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// double zero = 0d;
    /// double nonZero = 3.14159d;
    /// bool result1 = zero.IsEmpty();     // 返回 true
    /// bool result2 = nonZero.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this double value) => value == 0;

    /// <summary>
    /// 判断可空双精度浮点数是否为空（null 或 0）
    /// </summary>
    /// <param name="value">可空双精度浮点数值</param>
    /// <returns>如果值为 null 或等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// double? nullValue = null;
    /// double? zero = 0d;
    /// double? nonZero = 3.14159d;
    /// bool result1 = nullValue.IsEmpty(); // 返回 true
    /// bool result2 = zero.IsEmpty();      // 返回 true
    /// bool result3 = nonZero.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this double? value) => value is null or 0;

    /// <summary>
    /// 判断十进制数是否为空（0）
    /// </summary>
    /// <param name="value">十进制数值</param>
    /// <returns>如果值等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// decimal zero = 0m;
    /// decimal nonZero = 123.456m;
    /// bool result1 = zero.IsEmpty();     // 返回 true
    /// bool result2 = nonZero.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this decimal value) => value == 0;

    /// <summary>
    /// 判断可空十进制数是否为空（null 或 0）
    /// </summary>
    /// <param name="value">可空十进制数值</param>
    /// <returns>如果值为 null 或等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// decimal? nullValue = null;
    /// decimal? zero = 0m;
    /// decimal? nonZero = 123.456m;
    /// bool result1 = nullValue.IsEmpty(); // 返回 true
    /// bool result2 = zero.IsEmpty();      // 返回 true
    /// bool result3 = nonZero.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this decimal? value) => value is null or 0;

    /// <summary>
    /// 判断字符串是否为 <c>null</c>、空或仅由空白字符组成。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns>如果字符串为 <c>null</c>、空或仅由空白字符组成，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <remarks>
    /// 该方法内部使用 <see cref="string.IsNullOrWhiteSpace(string)"/> 实现，可检测所有类型的空白字符，包括空格、制表符、换行符等。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 以下情况都将返回 true
    /// string nullString = null;
    /// bool result1 = nullString.IsEmpty();
    ///
    /// string emptyString = "";
    /// bool result2 = emptyString.IsEmpty();
    ///
    /// string whiteSpaceString = "   \t\n";
    /// bool result3 = whiteSpaceString.IsEmpty();
    ///
    /// // 以下情况将返回 false
    /// string nonEmptyString = "Hello World";
    /// bool result4 = nonEmptyString.IsEmpty();
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 判断日期时间是否为空（默认值或最小值）
    /// </summary>
    /// <param name="value">日期时间值</param>
    /// <returns>如果值等于默认值或最小值（0001-01-01 00:00:00），则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// DateTime defaultDate = default;
    /// DateTime nonDefaultDate = DateTime.Now;
    /// bool result1 = defaultDate.IsEmpty();     // 返回 true
    /// bool result2 = nonDefaultDate.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this DateTime value) => value == default || value == DateTime.MinValue;

    /// <summary>
    /// 判断可空日期时间是否为空（null 或默认值）
    /// </summary>
    /// <param name="value">可空日期时间值</param>
    /// <returns>如果值为 null 或等于最小值（0001-01-01 00:00:00），则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// DateTime? nullDate = null;
    /// DateTime? minDate = DateTime.MinValue;
    /// DateTime? normalDate = DateTime.Now;
    /// bool result1 = nullDate.IsEmpty();    // 返回 true
    /// bool result2 = minDate.IsEmpty();     // 返回 true
    /// bool result3 = normalDate.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this DateTime? value) => value is null || value == DateTime.MinValue;

    /// <summary>
    /// 判断日期时间偏移量是否为空（默认值）
    /// </summary>
    /// <param name="value">日期时间偏移量值</param>
    /// <returns>如果值等于默认值，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// DateTimeOffset defaultOffset = default;
    /// DateTimeOffset nonDefaultOffset = DateTimeOffset.Now;
    /// bool result1 = defaultOffset.IsEmpty();     // 返回 true
    /// bool result2 = nonDefaultOffset.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this DateTimeOffset value) => value == default || value == DateTimeOffset.MinValue;

    /// <summary>
    /// 判断可空日期时间偏移量是否为空（null 或最小值）
    /// </summary>
    /// <param name="value">可空日期时间偏移量值</param>
    /// <returns>如果值为 null 或等于最小值，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// DateTimeOffset? nullOffset = null;
    /// DateTimeOffset? minOffset = DateTimeOffset.MinValue;
    /// DateTimeOffset? normalOffset = DateTimeOffset.Now;
    /// bool result1 = nullOffset.IsEmpty();    // 返回 true
    /// bool result2 = minOffset.IsEmpty();     // 返回 true
    /// bool result3 = normalOffset.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this DateTimeOffset? value) => value is null || value == DateTimeOffset.MinValue;

    /// <summary>
    /// 判断时间间隔是否为空（0毫秒）
    /// </summary>
    /// <param name="value">时间间隔值</param>
    /// <returns>如果值的总毫秒数等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// TimeSpan zeroSpan = TimeSpan.Zero;
    /// TimeSpan nonZeroSpan = TimeSpan.FromHours(1);
    /// bool result1 = zeroSpan.IsEmpty();     // 返回 true
    /// bool result2 = nonZeroSpan.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this TimeSpan value) => value.TotalMilliseconds == 0;

    /// <summary>
    /// 判断可空时间间隔是否为空（null 或 0毫秒）
    /// </summary>
    /// <param name="value">可空时间间隔值</param>
    /// <returns>如果值为 null 或总毫秒数等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <example>
    /// <code>
    /// TimeSpan? nullSpan = null;
    /// TimeSpan? zeroSpan = TimeSpan.Zero;
    /// TimeSpan? nonZeroSpan = TimeSpan.FromHours(1);
    /// bool result1 = nullSpan.IsEmpty();     // 返回 true
    /// bool result2 = zeroSpan.IsEmpty();     // 返回 true
    /// bool result3 = nonZeroSpan.IsEmpty();  // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this TimeSpan? value) => value is null || value.Value.TotalMilliseconds == 0;

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">GUID 值</param>
    /// <returns>
    /// 如果 GUID 值等于 <see cref="Guid.Empty"/>（00000000-0000-0000-0000-000000000000），则返回 <c>true</c>；
    /// 否则返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// GUID 是全局唯一标识符，常用于作为对象的唯一标识。在数据库设计中，通常使用 GUID 作为主键。
    /// 空 GUID（<see cref="Guid.Empty"/>）是一个特殊值，表示没有有效的标识符。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 空 GUID
    /// Guid emptyGuid = Guid.Empty;
    /// bool result1 = emptyGuid.IsEmpty();  // 返回 true
    /// 
    /// // 新生成的 GUID
    /// Guid newGuid = Guid.NewGuid();
    /// bool result2 = newGuid.IsEmpty();    // 返回 false
    /// 
    /// // 从字符串创建的 GUID
    /// Guid parsedGuid = new Guid("12345678-1234-1234-1234-123456789012");
    /// bool result3 = parsedGuid.IsEmpty(); // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this Guid value) => value == Guid.Empty;

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为 <c>null</c> 或默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">可空 GUID 值</param>
    /// <returns>
    /// 如果 GUID 值为 <c>null</c> 或等于 <see cref="Guid.Empty"/>（00000000-0000-0000-0000-000000000000），
    /// 则返回 <c>true</c>；否则返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// 对于可空类型的 GUID，此方法将同时检查是否为 <c>null</c> 或是空 GUID（<see cref="Guid.Empty"/>）。
    /// </remarks>
    /// <example>
    /// <code>
    /// // null 值
    /// Guid? nullGuid = null;
    /// bool result1 = nullGuid.IsEmpty();  // 返回 true
    /// 
    /// // 空 GUID
    /// Guid? emptyGuid = Guid.Empty;
    /// bool result2 = emptyGuid.IsEmpty(); // 返回 true
    /// 
    /// // 新生成的 GUID
    /// Guid? newGuid = Guid.NewGuid();
    /// bool result3 = newGuid.IsEmpty();   // 返回 false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this Guid? value) => value == null || value == Guid.Empty;

    /// <summary>
    /// 判断 <see cref="StringBuilder"/> 是否为空
    /// </summary>
    /// <param name="sb"><see cref="StringBuilder"/> 对象</param>
    /// <returns>如果 <see cref="StringBuilder"/> 为 <c>null</c>，长度为 0，或转换为字符串后为空，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this StringBuilder sb) => sb == null || sb.Length == 0 || sb.ToString().IsEmpty();

    /// <summary>
    /// 判断集合是否为空
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="enumerable">集合</param>
    /// <returns>如果集合为 <c>null</c> 或不包含任何元素，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <remarks>
    /// 此方法检查集合是否为 null 或空集合。它适用于所有实现了 <see cref="IEnumerable{T}"/> 接口的类型，
    /// 包括数组、列表、集合、字典和自定义集合类型等。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 以下情况都将返回 true
    /// List&lt;int&gt; nullList = null;
    /// bool result1 = nullList.IsEmpty();  // 返回 true，因为集合为 null
    /// 
    /// List&lt;int&gt; emptyList = new List&lt;int&gt;();
    /// bool result2 = emptyList.IsEmpty(); // 返回 true，因为集合不包含任何元素
    /// 
    /// // 以下情况将返回 false
    /// List&lt;int&gt; nonEmptyList = new List&lt;int&gt; { 1, 2, 3 };
    /// bool result3 = nonEmptyList.IsEmpty(); // 返回 false，因为集合包含元素
    /// 
    /// int[] array = { 1, 2, 3 };
    /// bool result4 = array.IsEmpty();  // 返回 false，因为数组包含元素
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<T>(this IEnumerable<T> enumerable) => null == enumerable || !enumerable.Any();

    /// <summary>
    /// 判断字典是否为空
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <returns>如果字典为 <c>null</c> 或不包含任何键值对，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => null == dictionary || dictionary.Count == 0;

    /// <summary>
    /// 判断非泛型字典是否为空
    /// </summary>
    /// <param name="dictionary">非泛型字典</param>
    /// <returns>如果字典为 <c>null</c> 或不包含任何键值对，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this IDictionary dictionary) => null == dictionary || dictionary.Count == 0;

    #endregion

    #region IsDefault(是否默认值)

    /// <summary>
    /// 判断值是否为类型的默认值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">值</param>
    /// <returns>如果值是类型的默认值，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsDefault<T>(this T value) => EqualityComparer<T>.Default.Equals(value, default);

    #endregion

    #region IsNull(是否为空)

    /// <summary>
    /// 判断对象是否为 <c>null</c>
    /// </summary>
    /// <param name="target">对象</param>
    /// <returns>如果对象为 <c>null</c>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsNull(this object target) => target.IsNull<object>();

    /// <summary>
    /// 判断对象是否为 <c>null</c>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="target">对象</param>
    /// <returns>如果对象为 <c>null</c>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsNull<T>(this T target) => ReferenceEquals(target, null);

    #endregion

    #region NotEmpty(是否非空)

    /// <summary>
    /// 判断字符串是否非空
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns>如果字符串不为 <c>null</c>、空或仅由空白字符组成，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool NotEmpty(this string value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否非默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">GUID 值</param>
    /// <returns>如果 GUID 值不为 <see cref="Guid.Empty"/>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool NotEmpty(this Guid value) => value != Guid.Empty;

    /// <summary>
    /// 判断 <see cref="Guid"/>? 是否非 <c>null</c> 且不为默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">GUID 值</param>
    /// <returns>如果 GUID 值不为 <c>null</c> 且不为 <see cref="Guid.Empty"/>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool NotEmpty(this Guid? value) => value != null && value != Guid.Empty;

    /// <summary>
    /// 判断 <see cref="StringBuilder"/> 是否非空
    /// </summary>
    /// <param name="sb"><see cref="StringBuilder"/> 对象</param>
    /// <returns>如果 <see cref="StringBuilder"/> 不为 <c>null</c>，长度不为 0，且转换为字符串后不为空，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool NotEmpty(this StringBuilder sb) => sb != null && sb.Length != 0 && sb.ToString().NotEmpty();

    /// <summary>
    /// 判断集合是否非空
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="enumerable">集合</param>
    /// <returns>如果集合不为 <c>null</c> 且包含至少一个元素，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool NotEmpty<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
            return false;
        if (enumerable.Any())
            return true;
        return false;
    }

    #endregion

    #region IsZeroOrMinus(是否为0或负数)

    /// <summary>
    /// 判断 <see cref="short"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this short value) => value <= 0;

    /// <summary>
    /// 判断 <see cref="int"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this int value) => value <= 0;

    /// <summary>
    /// 判断 <see cref="long"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this long value) => value <= 0;

    /// <summary>
    /// 判断 <see cref="float"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this float value) => value <= 0;

    /// <summary>
    /// 判断 <see cref="double"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this double value) => value <= 0;

    /// <summary>
    /// 判断 <see cref="decimal"/> 是否为 0 或负数
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值小于等于 0，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrMinus(this decimal value) => value <= 0;

    #endregion

    #region IsPercentage(是否为百分数)

    /// <summary>
    /// 判断 <see cref="float"/> 是否为百分数（大于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值大于 0 且小于等于 1，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsPercentage(this float value) => value > 0 && value <= 1;

    /// <summary>
    /// 判断 <see cref="double"/> 是否为百分数（大于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值大于 0 且小于等于 1，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsPercentage(this double value) => value > 0 && value <= 1;

    /// <summary>
    /// 判断 <see cref="decimal"/> 是否为百分数（大于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值大于 0 且小于等于 1，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsPercentage(this decimal value) => value > 0 && value <= 1;

    #endregion

    #region IsZeroOrPercentage(是否为0或百分数)

    /// <summary>
    /// 判断 <see cref="float"/> 是否为 0 或百分数（大于等于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值等于 0 或为百分数，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrPercentage(this float value) => value.IsPercentage() || value.Equals(0f);

    /// <summary>
    /// 判断 <see cref="double"/> 是否为 0 或百分数（大于等于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值等于 0 或为百分数，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrPercentage(this double value) => value.IsPercentage() || value.Equals(0d);

    /// <summary>
    /// 判断 <see cref="decimal"/> 是否为 0 或百分数（大于等于 0 且小于等于 1）
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>如果值等于 0 或为百分数，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsZeroOrPercentage(this decimal value) => value.IsPercentage() || value.Equals(0m);

    #endregion

    #region IsBetween(检查一个值是否在最小值和最大值之间)

    /// <summary>
    /// 检查一个值是否在最小值和最大值之间（包括最小值和最大值）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">值</param>
    /// <param name="minInclusiveValue">最小值（包含）</param>
    /// <param name="maxInclusiveValue">最大值（包含）</param>
    /// <returns>如果值在最小值和最大值之间，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsBetween<T>(this T value, T minInclusiveValue, T maxInclusiveValue) where T : IComparable<T>
    {
        return value.CompareTo(minInclusiveValue) >= 0 && value.CompareTo(maxInclusiveValue) <= 0;
    }

    #endregion
}