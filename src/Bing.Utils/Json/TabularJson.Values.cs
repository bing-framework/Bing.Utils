using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

public static partial class TabularJson
{
    /// <summary>
    /// 固定类型白名单，禁止根据输入加载任意 CLR 类型。
    /// </summary>
    private static readonly Dictionary<string, Type> CellTypes = new(StringComparer.Ordinal)
    {
        ["string"] = typeof(string), ["boolean"] = typeof(bool), ["char"] = typeof(char),
        ["int8"] = typeof(sbyte), ["uint8"] = typeof(byte), ["int16"] = typeof(short), ["uint16"] = typeof(ushort),
        ["int32"] = typeof(int), ["uint32"] = typeof(uint), ["int64"] = typeof(long), ["uint64"] = typeof(ulong),
        ["single"] = typeof(float), ["double"] = typeof(double), ["decimal"] = typeof(decimal),
        ["datetime"] = typeof(DateTime), ["datetimeoffset"] = typeof(DateTimeOffset),
        ["timespan"] = typeof(TimeSpan), ["guid"] = typeof(Guid), ["bytes"] = typeof(byte[])
    };

    /// <summary>
    /// 获取受支持类型的稳定名称。
    /// </summary>
    private static string TypeName(Type type)
    {
        foreach (var entry in CellTypes) if (entry.Value == type) return entry.Key;
        throw new JsonSerializationException($"不支持表格列类型 {type}。");
    }

    /// <summary>
    /// 编码单元格，独立于调用方的 TypeNameHandling 和区域设置。
    /// </summary>
    private static JToken EncodeCell(object value, Type type)
    {
        if (value == null || value == DBNull.Value) return JValue.CreateNull();
        TypeName(type);
        if (type == typeof(byte[])) return new JValue(Convert.ToBase64String((byte[])value));
        if (type == typeof(DateTime)) return new JValue(((DateTime)value).ToString("O", CultureInfo.InvariantCulture));
        if (type == typeof(DateTimeOffset)) return new JValue(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture));
        if (type == typeof(TimeSpan)) return new JValue(((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture));
        if (type == typeof(Guid)) return new JValue(((Guid)value).ToString("D"));
        if (type == typeof(float)) return new JValue(((float)value).ToString("R", CultureInfo.InvariantCulture));
        if (type == typeof(double)) return new JValue(((double)value).ToString("R", CultureInfo.InvariantCulture));
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(decimal))
            return new JValue(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
        if (type == typeof(char))
        {
            if (char.IsSurrogate((char)value)) throw new JsonSerializationException("不支持孤立代理字符。");
            return new JValue(value.ToString());
        }
        return new JValue(value);
    }

    /// <summary>
    /// 根据声明类型解析单元格并拒绝隐式截断和未知类型。
    /// </summary>
    private static object DecodeCell(JToken token, Type type, bool legacy = false)
    {
        if (token == null) throw new JsonSerializationException("缺少单元格值。");
        if (token.Type == JTokenType.Null) return DBNull.Value;
        try
        {
            TypeName(type);
            if (legacy && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                && (type == typeof(long) || type == typeof(ulong) || type == typeof(decimal) || type == typeof(double) || type == typeof(float)))
                token = new JValue(token.ToString(Formatting.None));
            if (type == typeof(bool)) return Require<bool>(token, JTokenType.Boolean);
            if (type == typeof(string)) return Require<string>(token, JTokenType.String);
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint))
            {
                if (token.Type != JTokenType.Integer) throw new FormatException("需要整数。");
                return Convert.ChangeType(token.ToString(Formatting.None), type, CultureInfo.InvariantCulture);
            }
            var text = Require<string>(token, JTokenType.String);
            if (type == typeof(char))
            {
                if (text.Length != 1 || char.IsSurrogate(text[0])) throw new FormatException("需要一个非代理字符。");
                return text[0];
            }
            if (type == typeof(byte[])) return Convert.FromBase64String(text);
            if (type == typeof(Guid)) return Guid.ParseExact(text, "D");
            if (type == typeof(DateTime)) return legacy
                ? DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(DateTimeOffset)) return legacy
                ? DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None)
                : DateTimeOffset.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None);
            if (type == typeof(TimeSpan)) return TimeSpan.ParseExact(text, "c", CultureInfo.InvariantCulture);
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException || ex is ArgumentException)
        {
            throw new JsonSerializationException($"单元格 {token.Path} 不能解析为 {type.Name}。", ex);
        }
    }

    /// <summary>
    /// 获取类型严格匹配的 JSON 标量。
    /// </summary>
    private static T Require<T>(JToken token, JTokenType kind)
    {
        if (token == null || token.Type != kind) throw new JsonSerializationException($"字段 {token?.Path ?? "<缺失>"} 应为 {kind}。");
        return token.Value<T>();
    }

    /// <summary>
    /// 获取必需数组。
    /// </summary>
    private static JArray Array(JToken token) => token as JArray ?? throw new JsonSerializationException($"字段 {token?.Path} 应为数组。");

    /// <summary>
    /// 获取必需对象。
    /// </summary>
    private static JObject Object(JToken token) => token as JObject ?? throw new JsonSerializationException($"字段 {token?.Path} 应为对象。");

    /// <summary>
    /// 校验对象数量预算。
    /// </summary>
    private static void Limit(long count, long? maximum, string name)
    {
        if (maximum.HasValue && count > maximum.Value) throw new JsonSerializationException($"{name} 超过限额 {maximum.Value}。");
    }
}
