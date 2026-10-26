namespace Bing.Serialization.Csv;

/// <summary>
/// 配置 CSV/TSV 流的格式、映射和资源预算。
/// </summary>
public sealed class CsvOptions
{
    /// <summary>
    /// 获取或设置字段分隔符，默认是逗号；制表符可用于 TSV。
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// 获取或设置读取时是否把首条记录作为表头。
    /// </summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// 获取或设置写入时是否输出表头。
    /// </summary>
    public bool WriteHeader { get; set; } = true;

    /// <summary>
    /// 获取或设置固定列名；读取时用于无表头映射或校验表头，写入时作为输出表头。
    /// </summary>
    public IReadOnlyList<string> Headers { get; set; }

    /// <summary>
    /// 获取或设置单条记录的 UTF-8 原始字节上限，包含分隔符和引用语法但不含行结束符；null 表示不限制。
    /// </summary>
    public long? MaxRowBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// 获取或设置单个字段的 UTF-8 原始字节上限，包含引用语法；null 表示不限制。
    /// </summary>
    public long? MaxFieldBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// 获取或设置读取时允许消耗的 UTF-8 输入总字节数；null 表示不限制，默认不限制。
    /// </summary>
    /// <remarks>
    /// 统计范围包含 UTF-8 BOM、表头、分隔符和记录结束符；该选项不限制写入。
    /// </remarks>
    public long? MaxInputBytes { get; set; }

    /// <summary>
    /// 获取或设置允许读取或写出的数据行数；null 表示不限制，默认不限制。
    /// </summary>
    /// <remarks>
    /// 表头不计入行数；零允许空数据以及单独的表头。
    /// </remarks>
    public long? MaxRows { get; set; }

    /// <summary>
    /// 获取或设置每条记录允许的最大字段数。
    /// </summary>
    public int MaxFieldsPerRow { get; set; } = 4096;

    /// <summary>
    /// 获取或设置写入记录的行结束符，只接受 LF 或 CRLF。
    /// </summary>
    public string NewLine { get; set; } = "\r\n";

    internal CsvOptions Snapshot()
    {
        if (Delimiter > 0x7f || Delimiter == '\0' || Delimiter == '"' || Delimiter == '\r' || Delimiter == '\n')
        {
            throw new ArgumentOutOfRangeException(nameof(Delimiter), "分隔符必须是非零 ASCII 字符，且不能是引号或换行符。");
        }

        if (MaxRowBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRowBytes));
        }

        if (MaxFieldBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFieldBytes));
        }

        if (MaxInputBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxInputBytes));
        }

        if (MaxRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRows));
        }

        if (MaxFieldsPerRow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFieldsPerRow));
        }

        if (NewLine != "\n" && NewLine != "\r\n")
        {
            throw new ArgumentException("行结束符只能是 LF 或 CRLF。", nameof(NewLine));
        }

        string[] headers = null;
        if (Headers != null)
        {
            headers = Headers.ToArray();
            if (headers.Length == 0 || headers.Length > MaxFieldsPerRow)
            {
                throw new ArgumentException("表头字段数必须大于零且不能超过每行字段数上限。", nameof(Headers));
            }

            CsvHeaderValidation.Validate(headers, nameof(Headers));
        }

        return new CsvOptions
        {
            Delimiter = Delimiter,
            HasHeader = HasHeader,
            WriteHeader = WriteHeader,
            Headers = headers,
            MaxRowBytes = MaxRowBytes,
            MaxFieldBytes = MaxFieldBytes,
            MaxInputBytes = MaxInputBytes,
            MaxRows = MaxRows,
            MaxFieldsPerRow = MaxFieldsPerRow,
            NewLine = NewLine
        };
    }
}
