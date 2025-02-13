﻿

// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 对象(<see cref="object"/>) 扩展
/// </summary>
public static partial class ObjectExtensions
{
    #region PropertyClone 对象值克隆

    /// <summary>
    /// 从源对象赋值到当前对象
    /// </summary>
    /// <param name="destination">当前对象</param>
    /// <param name="source">数据源对象</param>
    /// <returns>成功复制的值个数</returns>
    public static int ClonePropertyFrom(this object destination, object source) => destination.ClonePropertyFrom(source, null);

    /// <summary>
    /// 从源对象赋值到当前对象
    /// </summary>
    /// <param name="destination">当前对象</param>
    /// <param name="source">数据源对象</param>
    /// <param name="excludeName">排除下不要复制的属性名称</param>
    /// <returns>成功复制的值个数</returns>
    public static int ClonePropertyFrom(this object destination, object source, IEnumerable<string> excludeName)
    {
        if (source == null)
            return 0;
        return destination.ClonePropertyFrom(source, source.GetType(), excludeName);
    }

    /// <summary>
    /// 从源对象赋值到当前对象
    /// </summary>
    /// <param name="this">当前对象</param>
    /// <param name="source">数据源对象</param>
    /// <param name="type">复制的属性的类型</param>
    /// <param name="excludeName">排除不要复制属性名称</param>
    /// <returns>成功复制的值个数</returns>
    public static int ClonePropertyFrom(this object @this, object source, Type type, IEnumerable<string> excludeName)
    {
        if (@this == null || source == null)
            return 0;
        if (@this == source)
            return 0;
        if (excludeName == null)
            excludeName = new List<string>();
        var i = 0;
        var desType = @this.GetType();
        foreach (var mi in type.GetFields())
        {
            if (excludeName.Contains(mi.Name))
                continue;
            try
            {
                var des = desType.GetField(mi.Name);
                if (des != null && des.FieldType == mi.FieldType)
                {
                    des.SetValue(@this, mi.GetValue(source));
                    i++;
                }
            }
            catch
            {
            }
        }

        foreach (var pi in type.GetProperties())
        {
            if (excludeName.Contains(pi.Name))
                continue;
            try
            {
                var des = desType.GetProperty(pi.Name);
                if (des != null && des.PropertyType == pi.PropertyType && des.CanWrite && pi.CanRead)
                {
                    des.SetValue(@this, pi.GetValue(source, null), null);
                    i++;
                }
            }
            catch
            {
            }
        }
        return i;
    }

    /// <summary>
    /// 从当前对象赋值到目标对象
    /// </summary>
    /// <param name="source">当前对象</param>
    /// <param name="destination">目标对象</param>
    /// <returns>成功复制的值个数</returns>
    public static int ClonePropertyTo(this object source, object destination) => source.ClonePropertyTo(destination, null);

    /// <summary>
    /// 从当前对象赋值到目标对象
    /// </summary>
    /// <param name="source">当前对象</param>
    /// <param name="destination">目标对象</param>
    /// <param name="excludeName">排除下列名称的属性不要复制</param>
    /// <returns>成功复制的值个数</returns>
    public static int ClonePropertyTo(this object source, object destination, IEnumerable<string> excludeName)
    {
        if (destination == null)
            return 0;
        return destination.ClonePropertyFrom(source, source.GetType(), excludeName);
    }

    #endregion

    #region ToDynamic(将对象转换为dynamic)

    ///// <summary>
    ///// 将对象[主要是匿名对象]转换为dynamic
    ///// </summary>
    ///// <param name="value">值</param>
    ///// <returns></returns>
    //public static dynamic ToDynamic(this object value)
    //{
    //    IDictionary<string,object> expando=new ExpandoObject();
    //    Type type = value.GetType();
    //    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);
    //    foreach (PropertyDescriptor property in properties)
    //    {
    //        var val = property.GetValue(value);
    //        if (property.PropertyType.FullName != null &&
    //            property.PropertyType.FullName.StartsWith("<>f__AnonymousType"))
    //        {
    //            dynamic dval = val.ToDynamic();
    //            expando.Add(property.Name,dval);
    //        }
    //        else
    //        {
    //            expando.Add(property.Name, val);
    //        }
    //    }

    //    return (ExpandoObject) expando;
    //}

    #endregion

    #region ToNullable(将指定值转换为对应的可空类型)

    /// <summary>
    /// 将指定值转换为对应的可空类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="value">值</param>
    public static T? ToNullable<T>(this T value) where T : struct => value.IsNull() ? null : (T?)value;

    #endregion
}