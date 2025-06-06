﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Bing.Helpers;

namespace Bing.Serialization.SystemTextJson;

/// <summary>
/// 可空日期格式Json转换器
/// </summary>
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 日期格式
    /// </summary>
    private readonly string _format;

    /// <summary>
    /// 初始化一个<see cref="NullableDateTimeJsonConverter"/>类型的实例
    /// </summary>
    public NullableDateTimeJsonConverter() : this("yyyy-MM-dd HH:mm:ss")
    {
    }

    /// <summary>
    /// 初始化一个<see cref="NullableDateTimeJsonConverter"/>类型的实例
    /// </summary>
    /// <param name="format">日期格式。默认值：yyyy-MM-dd HH:mm:ss</param>
    public NullableDateTimeJsonConverter(string format) => _format = format;

    /// <summary>
    /// 读取数据
    /// </summary>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return Time.ToLocalTime(Conv.ToDate(reader.GetString()));
        if (reader.TryGetDateTime(out var date))
            return Time.ToLocalTime(date);
        return DateTime.MinValue;
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        var date = Time.ToLocalTime(value.Value).ToString(_format);
        writer.WriteStringValue(date);
    }
}