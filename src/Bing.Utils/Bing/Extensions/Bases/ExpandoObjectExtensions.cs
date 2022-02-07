using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Bing.Collections;

// ReSharper disable once CheckNamespace
namespace Bing.Extensions
{
    /// <summary>
    /// 动态对象(<see cref="ExpandoObject"/>) 扩展
    /// </summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="eo">动态对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">属性值</param>
        /// <exception cref="ArgumentException"></exception>
        public static void AddProperty(this ExpandoObject eo, string propertyName, object value)
        {
            var obj = (IDictionary<string, object>)eo;
            if (obj.ContainsKey(propertyName))
                throw new ArgumentException($"属性 {propertyName} 已存在!");
            obj.Add(propertyName, value);
        }

        /// <summary>
        /// 设置属性
        /// </summary>
        /// <param name="eo">动态对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">属性值</param>
        public static void SetProperty(this ExpandoObject eo, string propertyName, object value)
        {
            var obj = (IDictionary<string, object>)eo;
            if (!obj.ContainsKey(propertyName))
                obj.Add(propertyName, value);
            else
                obj[propertyName] = value;
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="eo">动态对象</param>
        /// <param name="propertyName">属性名</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static object GetProperty(this ExpandoObject eo, string propertyName)
        {
            var obj = (IDictionary<string, object>)eo;
            if (!obj.ContainsKey(propertyName))
                throw new ArgumentNullException($"不存在 {propertyName} 属性");
            return obj[propertyName];
        }

        /// <summary>
        /// 获取全部属性
        /// </summary>
        /// <param name="eo">动态对象</param>
        public static List<string> GetProperties(this ExpandoObject eo)
        {
            var obj = (IDictionary<string, object>)eo;
            return obj.Keys.ToList();
        }

        /// <summary>
        /// 删除属性
        /// </summary>
        /// <param name="eo">动态对象</param>
        /// <param name="propertyName">属性名</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveProperty(this ExpandoObject eo, string propertyName)
        {
            var obj = (IDictionary<string, object>)eo;
            if (!obj.ContainsKey(propertyName))
                throw new ArgumentNullException($"不存在 {propertyName} 属性");
            obj.Remove(propertyName);
        }

        /// <summary>
        /// 将动态属性对象<see cref="ExpandoObject"/>列表转换为<see cref="DataTable"/>
        /// </summary>
        /// <param name="eos">动态属性对象列表</param>
        public static DataTable ToDataTable(this IEnumerable<ExpandoObject> eos)
        {
            var dt = new DataTable();
            if (eos == null)
                return null;
            if (!eos.Any())
                return dt;
            var entity = eos.FirstOrDefault();
            var properties = entity.GetProperties();
            properties.ForEach(prop =>
            {
                dt.Columns.Add(prop);
            });
            eos.ForEach((data, index) =>
            {
                dt.Rows.Add(dt.NewRow());
                properties.ForEach(prop =>
                {
                    dt.Rows[index][prop] = data.GetProperty(prop);
                });
            });
            return dt;
        }
    }
}
