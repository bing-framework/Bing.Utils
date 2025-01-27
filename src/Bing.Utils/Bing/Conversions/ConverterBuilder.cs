namespace Bing.Conversions;

/// <summary>
/// 常用类型转换器构建器
/// </summary>
public static class ConverterBuilder
{
    /// <summary>
    /// 获取Guid类型的转换器
    /// </summary>
    /// <param name="format">
    /// Guid 格式字符串（可选）。如果提供，必须为标准 Guid 格式说明符之一（如 "N", "D", "B", "P", "X"）。
    /// 如果未提供，将使用默认解析方式。
    /// </param>
    /// <returns>用于将对象转换为 Guid 的转换器</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FormatException">当提供的字符串格式不正确时抛出</exception>
    /// <exception cref="InvalidCastException">当无法将输入转换为 Guid 时抛出</exception>
    internal static Converter<object, Guid> GetGuidConverter(string format = null)
    {
        return (value) =>
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value is Guid guid)
                return guid;
            var s = Convert.ToString(value);
            if (string.IsNullOrEmpty(s))
                throw new InvalidCastException("The provided value cannot be converted to a valid GUID.");
            return string.IsNullOrEmpty(format) ? Guid.Parse(s) : Guid.ParseExact(s, format);
        };
    }

    /// <summary>
    /// 获取枚举类型的转换器。
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns>转换器</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    internal static Converter<object, T> GetEnumConverter<T>() where T : struct, Enum =>
        value => (T)GetEnumConverter(typeof(T)).Invoke(value);

    /// <summary>
    /// 获取枚举类型的转换器。
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>转换器</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidCastException"></exception>
    internal static Converter<object, object> GetEnumConverter(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));
        if (!enumType.IsEnum)
            throw new ArgumentException($"{enumType.Name} 必须是一个枚举类型", nameof(enumType));

        return value =>
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), $"转换的值不能为 null: {enumType.Name}");
            try
            {
                return value switch
                {
                    var v when v.GetType() == enumType => v,
                    Enum e => ConvertToEnumFromEnum(e, enumType),
                    string s => Enum.Parse(enumType, s, true),
                    IConvertible c => Enum.ToObject(enumType, c.ToType(Enum.GetUnderlyingType(enumType), null)),
                    _ => throw new InvalidCastException($"无法将类型 {value.GetType()} 转换为枚举类型 {enumType.Name}")
                };
            }
            catch (Exception e)
            {
                throw new InvalidCastException($"转换为枚举类型 {enumType.Name} 失败", e);
            }
        };

        // 转换为枚举类型
        static object ConvertToEnumFromEnum(Enum enumValue, Type targetEnumType)
        {
            var underlyingValue = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()));
            return Enum.ToObject(targetEnumType, underlyingValue);
        }
    }
}