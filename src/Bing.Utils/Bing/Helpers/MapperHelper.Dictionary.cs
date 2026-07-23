using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Bing.Reflection;

namespace Bing.Helpers;

public static partial class MapperHelper
{
    /// <summary>
    /// 将只读字典映射为指定类型的新对象。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="values">键和值组成的源字典。</param>
    /// <param name="options">字典映射选项。</param>
    /// <returns>映射后的新对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="values"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当字典键、映射配置或值转换无效且未忽略时抛出。</exception>
    public static T MapDictionary<T>(IReadOnlyDictionary<string, object> values, DictionaryMapOptions options = null) where T : new()
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return MapDictionaryCore<T>(values.Select(item => new KeyValuePair<string, object>(item.Key, item.Value)), options);
    }

    /// <summary>
    /// 将非泛型字典映射为指定类型的新对象。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="values">键和值组成的源字典。</param>
    /// <param name="options">字典映射选项。</param>
    /// <returns>映射后的新对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="values"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当字典包含非字符串键、映射配置或值转换无效且未忽略时抛出。</exception>
    public static T MapDictionary<T>(IDictionary values, DictionaryMapOptions options = null) where T : new()
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var entries = new List<KeyValuePair<string, object>>();
        foreach (DictionaryEntry entry in values)
        {
            if (!(entry.Key is string key))
                throw new ArgumentException("字典映射仅支持字符串键", nameof(values));
            entries.Add(new KeyValuePair<string, object>(key, entry.Value));
        }

        return MapDictionaryCore<T>(entries, options);
    }

    /// <summary>
    /// 将只读字典转换为指定类型的新对象。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="values">键和值组成的源字典。</param>
    /// <param name="options">字典映射选项。</param>
    /// <returns>映射后的新对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="values"/> 为 null 时抛出。</exception>
    public static T ToObject<T>(this IReadOnlyDictionary<string, object> values, DictionaryMapOptions options = null) where T : new() =>
        MapDictionary<T>(values, options);

    /// <summary>
    /// 执行字典到对象的核心映射。
    /// </summary>
    /// <typeparam name="T">目标对象类型。</typeparam>
    /// <param name="values">已验证的源字典项。</param>
    /// <param name="options">字典映射选项。</param>
    /// <returns>映射后的新对象。</returns>
    private static T MapDictionaryCore<T>(IEnumerable<KeyValuePair<string, object>> values, DictionaryMapOptions options) where T : new()
    {
        options ??= new DictionaryMapOptions();
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(T))
            .Where(property => property.SetMethod != null && property.SetMethod.IsPublic)
            .ToArray();
        var assignedProperties = new HashSet<string>(options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var result = new T();

        foreach (var item in values)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                if (options.IgnoreUnknownKeys)
                    continue;
                throw new ArgumentException("字典键不能为空", nameof(values));
            }

            var propertyName = GetMappedPropertyName(item.Key, options.PropertyMappings, comparison);
            var property = FindMatchingMember(properties, propertyName, options.IgnoreCase) as PropertyInfo;
            if (property == null)
            {
                if (options.IgnoreUnknownKeys)
                    continue;
                throw new ArgumentException($"字典键“{item.Key}”未匹配目标类型“{typeof(T).FullName}”的公共可写属性。", nameof(values));
            }

            if (!assignedProperties.Add(property.Name))
                continue;

            if (!TryConvertValue(item.Value, property.PropertyType, out var convertedValue))
            {
                assignedProperties.Remove(property.Name);
                if (options.IgnoreConversionErrors)
                    continue;
                throw CreateMapConversionException(typeof(T), item.Key, item.Value, typeof(T), property.Name, property.PropertyType, null);
            }

            try
            {
                property.SetValue(result, convertedValue);
            }
            catch (TargetInvocationException exception)
            {
                assignedProperties.Remove(property.Name);
                throw new InvalidOperationException($"写入目标类型“{typeof(T).FullName}”的成员“{property.Name}”时发生异常。", exception.InnerException ?? exception);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取字典键对应的目标属性名称。
    /// </summary>
    /// <param name="key">字典键。</param>
    /// <param name="mappings">字典键到目标属性名的映射表。</param>
    /// <param name="comparison">键比较方式。</param>
    /// <returns>目标属性名称；不存在显式映射时返回原字典键。</returns>
    private static string GetMappedPropertyName(string key, IReadOnlyDictionary<string, string> mappings, StringComparison comparison)
    {
        if (mappings == null)
            return key;

        foreach (var mapping in mappings)
        {
            if (string.Equals(mapping.Key, key, comparison))
                return mapping.Value;
        }

        return key;
    }

    /// <summary>
    /// 尝试将值转换为目标成员类型。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="destinationType">目标类型。</param>
    /// <param name="result">转换后的值。</param>
    /// <returns>转换成功时返回 true；否则返回 false。</returns>
    internal static bool TryConvertValue(object value, Type destinationType, out object result)
    {
        if (destinationType == null)
            throw new ArgumentNullException(nameof(destinationType));

        result = null;
        var nullableUnderlyingType = Nullable.GetUnderlyingType(destinationType);
        var actualDestinationType = nullableUnderlyingType ?? destinationType;
        if (value == null || value == DBNull.Value)
        {
            if (!actualDestinationType.IsValueType || nullableUnderlyingType != null)
                return true;
            return false;
        }

        if (destinationType.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        try
        {
            if (actualDestinationType == typeof(Guid))
            {
                if (Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var guid))
                {
                    result = guid;
                    return true;
                }
                return false;
            }

            if (actualDestinationType.IsEnum)
            {
                if (value is string text)
                {
                    result = System.Enum.Parse(actualDestinationType, text, true);
                    return true;
                }

                var enumUnderlyingType = System.Enum.GetUnderlyingType(actualDestinationType);
                var enumNumber = Convert.ChangeType(value, enumUnderlyingType, CultureInfo.InvariantCulture);
                result = System.Enum.ToObject(actualDestinationType, enumNumber);
                return true;
            }

            if (actualDestinationType == typeof(DateTime))
            {
                if (!DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dateTime))
                    return false;
                result = dateTime;
                return true;
            }

            if (actualDestinationType == typeof(DateTimeOffset))
            {
                if (!DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dateTimeOffset))
                    return false;
                result = dateTimeOffset;
                return true;
            }

            if (actualDestinationType == typeof(TimeSpan))
            {
                if (!TimeSpan.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, out var timeSpan))
                    return false;
                result = timeSpan;
                return true;
            }

            if (value is string json && IsComplexJsonTarget(actualDestinationType))
            {
                result = Json.ToObject(json, actualDestinationType);
                return result != null;
            }

            var converter = TypeDescriptor.GetConverter(actualDestinationType);
            if (converter.CanConvertFrom(value.GetType()))
            {
                result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                return result != null;
            }

            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(actualDestinationType))
            {
                result = Convert.ChangeType(value, actualDestinationType, CultureInfo.InvariantCulture);
                return true;
            }

            if (value is JsonElement element)
            {
                result = Json.ToObject(element.GetRawText(), actualDestinationType);
                return result != null;
            }
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
        catch (InvalidCastException)
        {
            result = null;
            return false;
        }
        catch (OverflowException)
        {
            result = null;
            return false;
        }
        catch (ArgumentException)
        {
            result = null;
            return false;
        }
        catch (NotSupportedException)
        {
            result = null;
            return false;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }

        return false;
    }

    /// <summary>
    /// 判断目标类型是否应从 JSON 字符串读取复杂对象。
    /// </summary>
    /// <param name="type">目标类型。</param>
    /// <returns>目标类型为复杂对象或集合时返回 true；否则返回 false。</returns>
    private static bool IsComplexJsonTarget(Type type)
    {
        return type != typeof(string)
               && !type.IsPrimitive
               && !type.IsEnum
               && type != typeof(decimal)
               && type != typeof(Guid)
               && type != typeof(DateTime)
               && type != typeof(DateTimeOffset)
               && type != typeof(TimeSpan)
               && type != typeof(Uri);
    }

    /// <summary>
    /// 创建包含映射上下文的转换异常。
    /// </summary>
    /// <param name="key">源字典键或源成员名称。</param>
    /// <param name="memberName">目标成员名称。</param>
    /// <param name="value">源值。</param>
    /// <param name="destinationType">目标类型。</param>
    /// <param name="innerException">底层异常。</param>
    /// <returns>包含转换上下文的异常。</returns>
    internal static ArgumentException CreateConversionException(string key, string memberName, object value, Type destinationType, Exception innerException)
    {
        return CreateMapConversionException(null, key, value, null, memberName, destinationType, innerException);
    }

    /// <summary>
    /// 创建包含源和目标成员上下文的转换异常。
    /// </summary>
    /// <param name="sourceObjectType">源对象类型；字典映射时可为 null。</param>
    /// <param name="sourceMemberName">源成员名称或字典键。</param>
    /// <param name="value">源值。</param>
    /// <param name="destinationObjectType">目标对象类型；未知时可为 null。</param>
    /// <param name="destinationMemberName">目标成员名称。</param>
    /// <param name="destinationType">目标成员类型。</param>
    /// <param name="innerException">底层异常。</param>
    /// <returns>包含安全转换上下文的异常。</returns>
    internal static ArgumentException CreateMapConversionException(Type sourceObjectType, string sourceMemberName, object value,
        Type destinationObjectType, string destinationMemberName, Type destinationType, Exception innerException)
    {
        var sourceValueType = value?.GetType().FullName ?? "null";
        var sourceTypeName = sourceObjectType?.FullName ?? "字典";
        var destinationTypeName = destinationObjectType?.FullName ?? "未知目标类型";
        var message = $"无法将源类型“{sourceTypeName}”的成员“{sourceMemberName}”的值“{GetSafeValueText(value)}”（值类型“{sourceValueType}”）转换为目标类型“{destinationTypeName}”的成员“{destinationMemberName}”（目标类型“{destinationType.FullName}”）。";
        return new ArgumentException(message, nameof(value), innerException);
    }

    /// <summary>
    /// 获取用于异常信息的安全值文本。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <returns>长度受限的源值文本。</returns>
    private static string GetSafeValueText(object value)
    {
        if (value == null || value == DBNull.Value)
            return "null";

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.GetType().Name;
        return text.Length <= 128 ? text : text.Substring(0, 128) + "...";
    }
}
