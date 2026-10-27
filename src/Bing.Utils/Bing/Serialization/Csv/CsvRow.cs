using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using Bing.Diagnostics;
using Bing.Tabular;

namespace Bing.Serialization.Csv;

/// <summary>
/// 表示一条 CSV 记录及其可选表头映射。
/// </summary>
public sealed class CsvRow : IReadOnlyList<string>, ITabularRow
{
    private readonly ReadOnlyCollection<string> _values;
    private readonly ReadOnlyCollection<string> _headers;

    /// <summary>
    /// 使用字段值、可选表头和逻辑记录号创建记录。
    /// </summary>
    /// <param name="values">字段值；null 字段按空字段处理。</param>
    /// <param name="headers">可选列名，数量必须与字段值相同。</param>
    /// <param name="rowNumber">从一开始的逻辑记录号。</param>
    public CsvRow(IEnumerable<string> values, IEnumerable<string> headers = null, long rowNumber = 1)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (rowNumber <= 0) throw new ArgumentOutOfRangeException(nameof(rowNumber));

        var valueArray = values.Select(value => value ?? string.Empty).ToArray();
        if (valueArray.Length == 0) throw new ArgumentException("记录必须至少包含一个字段。", nameof(values));
        var headerArray = headers?.ToArray();
        if (headerArray != null)
        {
            CsvHeaderValidation.Validate(headerArray, nameof(headers));
            if (headerArray.Length != valueArray.Length)
            {
                throw new ArgumentException("表头数量必须与字段数量相同。", nameof(headers));
            }
        }

        _values = Array.AsReadOnly(valueArray);
        _headers = headerArray == null ? null : Array.AsReadOnly(headerArray);
        RowNumber = rowNumber;
    }

    /// <summary>
    /// 获取逻辑记录号。
    /// </summary>
    public long RowNumber { get; }

    /// <summary>获取原始 CSV 的逻辑记录位置，表头计入记录号。</summary>
    public CsvLocation Location => new(RowNumber);

    /// <inheritdoc />
    DataLocation ITabularRow.Location => Location;

    /// <summary>获取零基序号对应的列名；无表头时返回 null。</summary>
    /// <param name="ordinal">从零开始的列序号。</param>
    /// <returns>源列名或 null。</returns>
    public string GetName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= Count) throw new ArgumentOutOfRangeException(nameof(ordinal));
        return HeaderAt(ordinal);
    }

    /// <inheritdoc />
    object ITabularRow.GetValue(int ordinal) => _values[ordinal];

    /// <summary>
    /// 获取字段值列表。
    /// </summary>
    public IReadOnlyList<string> Values => _values;

    /// <summary>
    /// 获取可选表头；未配置时返回 null。
    /// </summary>
    public IReadOnlyList<string> Headers => _headers;

    /// <summary>
    /// 获取字段数量。
    /// </summary>
    public int Count => _values.Count;

    /// <summary>
    /// 获取指定位置的原始字符串字段。
    /// </summary>
    /// <param name="index">从零开始的字段位置。</param>
    public string this[int index] => _values[index];

    /// <summary>
    /// 按字段位置将字符串转换为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="index">从零开始的字段位置。</param>
    /// <param name="culture">解析文化；省略时使用不变文化。</param>
    /// <returns>转换后的值。</returns>
    public T Get<T>(int index, CultureInfo culture = null)
    {
        if (index < 0 || index >= _values.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return ConvertValue<T>(_values[index], index + 1, HeaderAt(index), culture);
    }

    /// <summary>
    /// 按不区分大小写的表头名称将字符串转换为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="name">列名。</param>
    /// <param name="culture">解析文化；省略时使用不变文化。</param>
    /// <returns>转换后的值。</returns>
    public T Get<T>(string name, CultureInfo culture = null)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("列名不能为空。", nameof(name));
        if (_headers == null) throw new InvalidOperationException("此记录没有表头映射，请按字段位置读取。");

        for (var index = 0; index < _headers.Count; index++)
        {
            if (string.Equals(_headers[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return ConvertValue<T>(_values[index], index + 1, _headers[index], culture);
            }
        }

        throw new CsvFormatException($"找不到列名“{name}”。", RowNumber);
    }

    /// <summary>
    /// 返回字段枚举器。
    /// </summary>
    /// <returns>字段值枚举器。</returns>
    public IEnumerator<string> GetEnumerator() => _values.GetEnumerator();

    /// <summary>
    /// 返回字段枚举器。
    /// </summary>
    /// <returns>字段值枚举器。</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string HeaderAt(int index) => _headers == null ? null : _headers[index];

    private T ConvertValue<T>(string text, int columnNumber, string columnName, CultureInfo culture)
    {
        var targetType = typeof(T);
        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (effectiveType == typeof(string)) return (T)(object)text;
        if (string.IsNullOrEmpty(text))
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null) return default;
            throw new CsvFormatException($"空字段不能转换为 {effectiveType.Name}。", RowNumber, columnNumber, columnName);
        }

        try
        {
            var provider = culture ?? CultureInfo.InvariantCulture;
            object converted;
            if (effectiveType.IsEnum)
            {
                converted = Enum.Parse(effectiveType, text, ignoreCase: true);
            }
            else if (effectiveType == typeof(Guid))
            {
                converted = Guid.Parse(text);
            }
            else if (effectiveType == typeof(DateTimeOffset))
            {
                converted = DateTimeOffset.Parse(text, provider);
            }
            else if (effectiveType == typeof(TimeSpan))
            {
                converted = TimeSpan.Parse(text, provider);
            }
            else
            {
                converted = Convert.ChangeType(text, effectiveType, provider);
            }

            return (T)converted;
        }
        catch (Exception exception) when (exception is FormatException || exception is InvalidCastException ||
                                           exception is OverflowException || exception is ArgumentException)
        {
            throw new CsvFormatException($"字段值无法转换为 {effectiveType.Name}。", RowNumber, columnNumber,
                columnName, exception);
        }
    }
}

internal static class CsvHeaderValidation
{
    internal static void Validate(IReadOnlyList<string> headers, string parameterName)
    {
        if (headers == null) throw new ArgumentNullException(parameterName);
        if (headers.Count == 0) throw new ArgumentException("表头不能是空列表。", parameterName);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            if (string.IsNullOrWhiteSpace(header))
                throw new ArgumentException($"第 {index + 1} 个表头不能为空。", parameterName);
            if (!seen.Add(header)) throw new ArgumentException($"表头“{header}”重复。", parameterName);
        }
    }
}
