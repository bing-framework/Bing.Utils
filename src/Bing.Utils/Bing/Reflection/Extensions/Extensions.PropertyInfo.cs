using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 属性信息(<see cref="PropertyInfo"/>) 扩展
/// </summary>
public static partial class PropertyInfoExtensions
{
    /// <summary>
    /// 获取 值获取器
    /// </summary>
    /// <param name="propertyInfo">属性</param>
    public static Func<object, object> GetValueGetter(this PropertyInfo propertyInfo)
    {
        return TypeReflections.TypeCacheManager.PropertyValueGetters.GetOrAdd(propertyInfo, prop =>
        {
            if (!prop.CanRead)
                return null;

            Debug.Assert(propertyInfo.DeclaringType != null);

            var instance = Expression.Parameter(typeof(object), "obj");
            var getterCall =
                Expression.Call(propertyInfo.DeclaringType!.IsValueType
                    ? Expression.Unbox(instance, propertyInfo.DeclaringType)
                    : Expression.Convert(instance, propertyInfo.DeclaringType), prop.GetGetMethod());
            var castToObject = Expression.Convert(getterCall, typeof(object));
            return (Func<object, object>) Expression.Lambda(castToObject, instance).Compile();
        });
    }

    /// <summary>
    /// 获取 值设置器
    /// </summary>
    /// <param name="propertyInfo">属性</param>
    public static Action<object, object> GetValueSetter(this PropertyInfo propertyInfo)
    {
        return TypeReflections.TypeCacheManager.PropertyValueSetters.GetOrAdd(propertyInfo, prop =>
        {
            if (!prop.CanWrite)
                return null;

            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));
            var setterCall =
                Expression.Call(
                    propertyInfo.DeclaringType!.IsValueType
                        ? Expression.Unbox(obj, propertyInfo.DeclaringType)
                        : Expression.Convert(obj, propertyInfo.DeclaringType), propertyInfo.GetSetMethod());
            return Expression.Lambda<Action<object, object>>(setterCall, obj, value).Compile();
        });
    }

}