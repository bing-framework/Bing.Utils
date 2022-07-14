using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Bing.Data
{
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
    }
}