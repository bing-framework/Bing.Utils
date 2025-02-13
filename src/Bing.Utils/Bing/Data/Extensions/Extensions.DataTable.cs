using System.Data;

// ReSharper disable once CheckNamespace
namespace Bing.Data;

/// <summary>
/// 数据表(<see cref="DataTable"/>) 扩展
/// </summary>
public static partial class DataTableExtensions
{
    /// <summary>
    /// 将<see cref="DataTable"/>转换为泛型集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="dataTable">数据表</param>
    public static IList<T> ToList<T>(this DataTable dataTable) where T : class, new() => DataTableHelper.ToList<T>(dataTable);

    /// <summary>
    /// 检查<see cref="DataTable"/>是否有数据行
    /// </summary>
    /// <param name="dataTable">数据表</param>
    /// <returns>是否有数据行</returns>
    public static bool HasRows(this DataTable dataTable) => dataTable.Rows.Count > 0;
}