using System.Globalization;

namespace Bing.Tabular;

/// <summary>为成员创建一次性选择的标量转换委托，不进行嵌套对象或 JSON 自动映射。</summary>
internal static class TabularValueConverter
{
    internal static Func<object, CultureInfo, object> Create(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        var actual = nullable ?? type;
        var acceptsNull = !type.IsValueType || nullable != null;
        Func<object, CultureInfo, object> convert;
        if (actual == typeof(string)) convert = (value, culture) => Convert.ToString(value, culture);
        else if (actual.IsEnum)
            convert = (value, culture) => value is string text ? Enum.Parse(actual, text, true)
                : Enum.ToObject(actual, Convert.ChangeType(value, Enum.GetUnderlyingType(actual), culture));
        else if (actual == typeof(Guid)) convert = (value, culture) => Guid.Parse(Convert.ToString(value, culture));
        else if (actual == typeof(DateTime)) convert = (value, culture) => DateTime.Parse(Convert.ToString(value, culture), culture, DateTimeStyles.RoundtripKind);
        else if (actual == typeof(DateTimeOffset)) convert = (value, culture) => DateTimeOffset.Parse(Convert.ToString(value, culture), culture, DateTimeStyles.None);
        else if (actual == typeof(TimeSpan)) convert = (value, culture) => TimeSpan.Parse(Convert.ToString(value, culture), culture);
        else if (actual == typeof(Uri)) convert = (value, culture) => new Uri(Convert.ToString(value, culture), UriKind.RelativeOrAbsolute);
        else convert = (value, culture) => Convert.ChangeType(value, actual, culture);
        return (value, culture) =>
        {
            if (value == null || value == DBNull.Value || nullable != null && value is string empty && string.IsNullOrWhiteSpace(empty))
            {
                if (acceptsNull) return null;
                throw new InvalidCastException("空值不能转换为非空值类型。");
            }
            return actual.IsInstanceOfType(value) ? value : convert(value, culture);
        };
    }
}
