using System.Data;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

/// <summary>
/// 表示逐行 JSON 的独立表结构快照。
/// </summary>
public sealed class TabularSchema
{
    /// <summary>
    /// 保存不含数据行的格式模型。
    /// </summary>
    internal JObject Model { get; }

    /// <summary>
    /// 保存内部使用的列结构，不向调用方暴露可变实例。
    /// </summary>
    internal DataTable[] Tables { get; }

    /// <summary>
    /// 获取结构是否描述数据集。
    /// </summary>
    public bool IsDataSet => (string)Model["kind"] == "dataset";

    /// <summary>
    /// 获取表数量。
    /// </summary>
    public int TableCount => Tables.Length;

    /// <summary>
    /// 从已校验的结构创建快照。
    /// </summary>
    internal TabularSchema(JObject model, DataTable[] tables) { Model = model; Tables = tables; }

    /// <summary>
    /// 捕获单表的结构，拒绝指向快照外部的关系。
    /// </summary>
    /// <param name="table">非 null 的表；数据行不会被复制或修改。</param>
    /// <returns>独立结构。</returns>
    public static TabularSchema FromTable(DataTable table) => TabularJson.CaptureSchema(table ?? throw new ArgumentNullException(nameof(table)));

    /// <summary>
    /// 捕获数据集的结构与关系。
    /// </summary>
    /// <param name="dataSet">非 null 的数据集。</param>
    /// <returns>独立结构。</returns>
    public static TabularSchema FromDataSet(DataSet dataSet) => TabularJson.CaptureSchema(dataSet ?? throw new ArgumentNullException(nameof(dataSet)));

    /// <summary>
    /// 获取指定表的独立列结构副本。
    /// </summary>
    /// <param name="tableIndex">从零开始的表序号。</param>
    /// <returns>调用方负责释放的空表；关系定义保留在整体结构中。</returns>
    public DataTable GetTableSchema(int tableIndex)
    {
        if (tableIndex < 0 || tableIndex >= Tables.Length) throw new ArgumentOutOfRangeException(nameof(tableIndex));
        return Tables[tableIndex].Clone();
    }
}
