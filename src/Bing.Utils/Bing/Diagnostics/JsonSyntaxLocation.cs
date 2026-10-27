namespace Bing.Diagnostics;

/// <summary>
/// 表示 JSON 语法错误的行号和 UTF-8 字节偏移。
/// </summary>
public sealed class JsonSyntaxLocation : DataLocation
{
    /// <summary>
    /// 初始化 JSON 语法位置。
    /// </summary>
    /// <param name="lineNumber">从 0 开始的行号。</param>
    /// <param name="bytePositionInLine">行内从 0 开始的 UTF-8 字节偏移。</param>
    public JsonSyntaxLocation(long lineNumber, long bytePositionInLine)
    {
        if (lineNumber < 0) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        if (bytePositionInLine < 0) throw new ArgumentOutOfRangeException(nameof(bytePositionInLine));

        LineNumber = lineNumber;
        BytePositionInLine = bytePositionInLine;
    }

    /// <inheritdoc />
    public override string Kind => "json-syntax";

    /// <summary>
    /// 获取从 0 开始的行号。
    /// </summary>
    public long LineNumber { get; }

    /// <summary>
    /// 获取行内从 0 开始的 UTF-8 字节偏移。
    /// </summary>
    public long BytePositionInLine { get; }

    /// <inheritdoc />
    public override string ToString() => $"JSON 第 {LineNumber} 行字节偏移 {BytePositionInLine}";
}
