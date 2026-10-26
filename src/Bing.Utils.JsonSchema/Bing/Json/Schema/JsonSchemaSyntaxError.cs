using System.Text.Json;

namespace Bing.Json.Schema;

/// <summary>描述 JSON 文本解析失败及解析器提供的位置。</summary>
public sealed class JsonSchemaSyntaxError
{
    /// <summary>获取解析器的错误说明。</summary>
    public string Message { get; }

    /// <summary>获取从零开始的行号；解析器未提供时为 null。</summary>
    public long? LineNumber { get; }

    /// <summary>获取当前行中从零开始的 UTF-8 字节位置；解析器未提供时为 null。</summary>
    public long? BytePositionInLine { get; }

    internal JsonSchemaSyntaxError(JsonException exception)
    {
        Message = exception.Message;
        LineNumber = exception.LineNumber;
        BytePositionInLine = exception.BytePositionInLine;
    }
}
