using System.ComponentModel;
using System.Data;
using Bing.Collections;

namespace Bing.Data;

/// <summary>
/// 数据表帮助类
/// </summary>
public static class DataTableHelper
{
    /// <summary>
    /// 将<see cref="DataTable"/>转换为泛型集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="dt">数据表</param>
    public static IList<T> ToList<T>(DataTable dt) where T : class, new()
    {
        var list = new List<T>();
        if (dt == null || dt.Rows.Count == 0)
            return list;
        list.AddRange(dt.Rows.Cast<DataRow>().Select(info => DataTableBuilder<T>.CreateBuilder(dt.Rows[0]).Build(info)));
        return list;
    }

    /// <summary>
    /// 将泛型集合类转换成 <see cref="DataTable"/>
    /// </summary>
    /// <typeparam name="T">集合项类型</typeparam>
    /// <param name="list">集合</param>
    /// <param name="tableName">表名</param>
    /// <returns>数据集(表)</returns>
    public static DataTable ToDataTable<T>(IEnumerable<T> list, string tableName = null) =>
        ToDataTable(list.ToList(), tableName);

    /// <summary>
    /// 将泛型集合类转换成 <see cref="DataTable"/>
    /// </summary>
    /// <typeparam name="T">集合项类型</typeparam>
    /// <param name="list">集合</param>
    /// <param name="tableName">表名</param>
    /// <returns>数据集(表)</returns>
    public static DataTable ToDataTable<T>(IList<T> list, string tableName = null)
    {
        var result = new DataTable(tableName);
        if (list.Count == 0)
        {
            // 添加表头列，列名为属性名
            foreach (var property in typeof(T).GetProperties()) 
                result.Columns.Add(property.Name, property.PropertyType);
            return result;
        }

        var properties=list[0].GetType().GetProperties();
        result.Columns.AddRange(properties.Select(p =>
        {
            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return new DataColumn(p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name, Nullable.GetUnderlyingType(p.PropertyType));
            return new DataColumn(p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name, p.PropertyType);
        }).ToArray());
        list.ForEach(item => result.LoadDataRow(properties.Select(p => p.GetValue(item)).ToArray(), true));
        return result;
    }

    /// <summary>
    /// 创建数据表，根据 nameList 里面的字段创建一个表格，返回该表格的 <see cref="DataTable"/>
    /// </summary>
    /// <param name="nameList">包含字段信息的列表</param>
    /// <returns>数据集(表)</returns>
    public static DataTable CreateTable(List<string> nameList)
    {
        if (nameList.Count <= 0)
            return null;
        var table = new DataTable();
        foreach (var columnName in nameList) 
            table.Columns.Add(columnName, typeof(string));
        return table;
    }

    /// <summary>
    /// 创建数据表，通过字符列表创建表字段，字段格式可以是：<br />
    /// 1) a,b,c,d,e<br/>
    /// 2) a|int,b|string,c|bool,d|decimal<br/>
    /// </summary>
    /// <param name="dt">数据表</param>
    /// <param name="nameString">字符列表</param>
    /// <returns>数据集(表)</returns>
    public static DataTable CreateDataTable(DataTable dt, string nameString)
    {
        var nameArray = nameString.Split(',', ';');
        foreach (var item in nameArray)
        {
            if(string.IsNullOrWhiteSpace(item))
                continue;
            var subItems = item.Split('|');
            if (subItems.Length == 2)
                dt.Columns.Add(subItems[0], ConvertType(subItems[1]));
            else
                dt.Columns.Add(subItems[0]);
        }

        return dt;
    }

    /// <summary>
    /// 根据类型名返回一个Type对象
    /// </summary>
    /// <param name="typeName">类型的名称</param>
    /// <returns>Type对象</returns>
    private static Type ConvertType(string typeName) => typeName.ToLower().Replace("system.", "") switch
    {
        "boolean" => typeof(bool),
        "bool" => typeof(bool),
        "int16" => typeof(short),
        "short" => typeof(short),
        "int32" => typeof(int),
        "int" => typeof(int),
        "long" => typeof(long),
        "int64" => typeof(long),
        "uint16" => typeof(ushort),
        "ushort" => typeof(ushort),
        "uint32" => typeof(uint),
        "uint" => typeof(uint),
        "uint64" => typeof(ulong),
        "ulong" => typeof(ulong),
        "single" => typeof(float),
        "float" => typeof(float),
        "string" => typeof(string),
        "guid" => typeof(Guid),
        "decimal" => typeof(decimal),
        "double" => typeof(double),
        "datetime" => typeof(DateTime),
        "byte" => typeof(byte),
        "char" => typeof(char),
        _ => typeof(string)
    };
}