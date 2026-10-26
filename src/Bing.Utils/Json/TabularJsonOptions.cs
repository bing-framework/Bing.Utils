using System.Data;

namespace Bing.Utils.Json;

/// <summary>
/// 指定表格转换器的写出格式。
/// </summary>
public enum TabularJsonFormat
{
    /// <summary>
    /// 保留历史 Rows/Tables 格式，读取时必须提供结构。
    /// </summary>
    Legacy = 0,
    /// <summary>
    /// 使用包含类型和结构的 bing.tabular v1 格式。
    /// </summary>
    Version1 = 1,
    /// <summary>
    /// 使用结构先行、保存行编辑版本的 bing.tabular v2 格式。
    /// </summary>
    Version2 = 2
}

/// <summary>
/// 配置表格 JSON 格式、历史结构和读取预算。
/// </summary>
public sealed class TabularJsonOptions
{
    /// <summary>
    /// 获取或设置写出格式；新入口默认使用版本一。
    /// </summary>
    public TabularJsonFormat Format { get; set; } = TabularJsonFormat.Version1;
    /// <summary>
    /// 获取或设置历史 Rows 格式的结构；读取只克隆结构，不修改原表。
    /// </summary>
    public DataTable TableSchema { get; set; }
    /// <summary>
    /// 获取或设置历史 Tables 格式的结构；按表顺序对应，不修改原数据集。
    /// </summary>
    public DataSet DataSetSchema { get; set; }
    /// <summary>
    /// 获取或设置文本及流入口的 UTF-8 输入字节上限；null 表示不限制，零只允许零字节输入。
    /// </summary>
    /// <remarks>已有 JsonReader 不提供原始字节位置；转换器拒绝此选项，请使用 TabularJson 入口。</remarks>
    public long? MaxInputBytes { get; set; }
    /// <summary>
    /// 获取或设置表数量上限；null 表示不限制。
    /// </summary>
    public int? MaxTables { get; set; }
    /// <summary>
    /// 获取或设置每张表的列数量上限；null 表示不限制。
    /// </summary>
    public int? MaxColumnsPerTable { get; set; }
    /// <summary>
    /// 获取或设置每张表的行数量上限；null 表示不限制。
    /// </summary>
    public int? MaxRowsPerTable { get; set; }
    /// <summary>
    /// 获取或设置 JSON 最大嵌套深度，默认与 Json.NET 一致为 64。
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// 获取或设置 v2 结构记录的 UTF-8 字节上限；null 不限制。
    /// </summary>
    public long? MaxSchemaBytes { get; set; }

    /// <summary>
    /// 获取或设置 v2 单行记录的 UTF-8 字节上限，包含所有版本及错误；读取在缓冲时检查。
    /// </summary>
    public long? MaxRowBytes { get; set; }

    /// <summary>
    /// 获取或设置 v2 单元格紧凑 JSON 值的 UTF-8 字节上限；解析后重新编码计数，null 不限制。
    /// </summary>
    /// <remarks>转义和空白按重新编码后的大小计算；原始输入缓冲必须另用 MaxRowBytes/MaxInputBytes 限制。</remarks>
    public long? MaxCellBytes { get; set; }

    /// <summary>
    /// 校验配置并固定本次调用的标量选项。
    /// </summary>
    internal TabularJsonOptions Snapshot()
    {
        if (!Enum.IsDefined(typeof(TabularJsonFormat), Format)) throw new ArgumentOutOfRangeException(nameof(Format));
        if (MaxInputBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxInputBytes));
        if (MaxTables < 0) throw new ArgumentOutOfRangeException(nameof(MaxTables));
        if (MaxColumnsPerTable < 0) throw new ArgumentOutOfRangeException(nameof(MaxColumnsPerTable));
        if (MaxRowsPerTable < 0) throw new ArgumentOutOfRangeException(nameof(MaxRowsPerTable));
        if (MaxDepth <= 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth));
        if (MaxSchemaBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxSchemaBytes));
        if (MaxRowBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxRowBytes));
        if (MaxCellBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxCellBytes));
        return (TabularJsonOptions)MemberwiseClone();
    }
}
