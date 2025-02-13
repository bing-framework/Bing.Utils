using System.Linq.Expressions;

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
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="propertyInfo">属性</param>
    public static Func<T, object> GetValueGetter<T>(this PropertyInfo propertyInfo)
    {
        return TypeReflections.TypeCacheManager<T>.PropertyValueGetters.GetOrAdd(propertyInfo, prop =>
        {
            if (!prop.CanRead)
                return null;
            var instance = Expression.Parameter(typeof(T), "i");
            var property = Expression.Property(instance, prop);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
        });
    }

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
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="propertyInfo">属性</param>
    public static Action<T, object> GetValueSetter<T>(this PropertyInfo propertyInfo)
    {
        return TypeReflections.TypeCacheManager<T>.PropertyValueSetters.GetOrAdd(propertyInfo, prop =>
        {
            if (!prop.CanWrite)
                return null;
            var instance = Expression.Parameter(typeof(T), "i");
            var argument = Expression.Parameter(typeof(object), "a");
            var setterCall = Expression.Call(instance, prop.GetSetMethod(), Expression.Convert(argument, prop.PropertyType));
            return Expression.Lambda<Action<T, object>>(setterCall, instance, argument).Compile();
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
            // 备注：对值类型使用 Expression.Unbox，对引用类型使用 Expression.Convert
            var setterCall =
                Expression.Call(
                    propertyInfo.DeclaringType!.IsValueType
                        ? Expression.Unbox(obj, propertyInfo.DeclaringType)
                        : Expression.Convert(obj, propertyInfo.DeclaringType), propertyInfo.GetSetMethod());
            return Expression.Lambda<Action<object, object>>(setterCall, obj, value).Compile();
        });
    }

    /// <summary>
    /// 判断属性是否静态
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsStatic(this PropertyInfo property) => (property.GetMethod ?? property.SetMethod).IsStatic;

}