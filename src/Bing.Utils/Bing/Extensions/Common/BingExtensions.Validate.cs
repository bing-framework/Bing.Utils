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
    /// 判断字符串是否为 <c>null</c>、空或仅由空白字符组成。
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns>如果字符串为 <c>null</c>、空或仅由空白字符组成，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">GUID 值</param>
    /// <returns>如果 GUID 值为 <see cref="Guid.Empty"/>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsEmpty(this Guid value) => value == Guid.Empty;

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为 <c>null</c> 或默认值（<see cref="Guid.Empty"/>）
    /// </summary>
    /// <param name="value">GUID 值</param>
    /// <returns>如果 GUID 值为 <c>null</c> 或 <see cref="Guid.Empty"/>，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsEmpty(this Guid? value) => value == null || IsEmpty(value.Value);

    /// <summary>
    /// 判断 <see cref="StringBuilder"/> 是否为空
    /// </summary>
    /// <param name="sb"><see cref="StringBuilder"/> 对象</param>
    /// <returns>如果 <see cref="StringBuilder"/> 为 <c>null</c>，长度为 0，或转换为字符串后为空，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsEmpty(this StringBuilder sb) => sb == null || sb.Length == 0 || sb.ToString().IsEmpty();

    /// <summary>
    /// 判断集合是否为空
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="list">集合</param>
    /// <returns>如果集合为 <c>null</c> 或不包含任何元素，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsEmpty<T>(this IEnumerable<T> list) => null == list || !list.Any();

    /// <summary>
    /// 判断字典是否为空
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <returns>如果字典为 <c>null</c> 或不包含任何键值对，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool IsEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => null == dictionary || dictionary.Count == 0;

    /// <summary>
    /// 判断字典是否为空
    /// </summary>
    /// <param name="dictionary">字典</param>
    /// <returns>如果字典为 <c>null</c> 或不包含任何键值对，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
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