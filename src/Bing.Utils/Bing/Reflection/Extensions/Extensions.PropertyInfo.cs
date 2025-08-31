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

            // 跳过索引器属性
            if (IsIndexerProperty(prop))
                return null;

            // 跳过静态属性
            if (IsStatic(prop))
                return null;

            var instance = Expression.Parameter(typeof(T), "i");
            var property = Expression.Property(instance, prop);
            var convert = Expression.Convert(property, typeof(object)); // 使用 Convert 而不是 TypeAs 以支持值类型
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

            // 跳过索引器属性
            if (IsIndexerProperty(prop))
                return null;

            var getMethod = prop.GetGetMethod();
            if (getMethod == null)
                return null;

            // 处理静态属性
            if (IsStatic(prop))
            {
                // 静态属性不需要实例，直接调用静态方法
                var staticCall = Expression.Call(null, getMethod);
                var castToObject = Expression.Convert(staticCall, typeof(object));
                return (Func<object, object>)Expression.Lambda(castToObject, Expression.Parameter(typeof(object), "obj")).Compile();
            }

            var instance = Expression.Parameter(typeof(object), "obj");
            var getterCall =
                Expression.Call(propertyInfo.DeclaringType!.IsValueType
                    ? Expression.Unbox(instance, propertyInfo.DeclaringType)
                    : Expression.Convert(instance, propertyInfo.DeclaringType), getMethod);
            var castToObjectResult = Expression.Convert(getterCall, typeof(object));
            return (Func<object, object>) Expression.Lambda(castToObjectResult, instance).Compile();
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

            // 跳过索引器属性
            if (IsIndexerProperty(prop))
                return null;

            // 跳过静态属性
            if (IsStatic(prop))
                return null;

            var setMethod = prop.GetSetMethod();
            if (setMethod == null)
                return null;

            var instance = Expression.Parameter(typeof(T), "i");
            var argument = Expression.Parameter(typeof(object), "a");

            if (typeof(T).IsValueType)
            {
                // 值类型需要通过引用传递来修改
                var instanceVar = Expression.Variable(typeof(T), "temp");
                var assign = Expression.Assign(instanceVar, instance);
                var setterCall = Expression.Call(instanceVar, setMethod, Expression.Convert(argument, prop.PropertyType));
                var reassign = Expression.Assign(instance, instanceVar);
                var block = Expression.Block(new[] { instanceVar }, assign, setterCall, reassign);
                return Expression.Lambda<Action<T, object>>(block, instance, argument).Compile();
            }
            else
            {
                // 引用类型的正常处理
                var setterCall = Expression.Call(instance, setMethod, Expression.Convert(argument, prop.PropertyType));
                return Expression.Lambda<Action<T, object>>(setterCall, instance, argument).Compile();
            }

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

            // 跳过索引属性
            if (IsIndexerProperty(prop))
                return null;

            var setMethod = prop.GetSetMethod();
            if (setMethod == null)
                return null;

            var obj = Expression.Parameter(typeof(object), "o");
            var value = Expression.Parameter(typeof(object));

            // 处理静态属性
            if (IsStatic(prop))
            {
                // 静态属性不需要实例，直接调用静态方法
                var staticCall = Expression.Call(null, setMethod, Expression.Convert(value, prop.PropertyType));
                return Expression.Lambda<Action<object, object>>(staticCall, obj, value).Compile();
            }

            // 备注：对值类型使用 Expression.Unbox，对引用类型使用 Expression.Convert
            // 处理实例属性
            var setterCall =
                Expression.Call(
                    propertyInfo.DeclaringType!.IsValueType
                        ? Expression.Unbox(obj, propertyInfo.DeclaringType)
                        : Expression.Convert(obj, propertyInfo.DeclaringType), setMethod, Expression.Convert(value, propertyInfo.PropertyType));
            return Expression.Lambda<Action<object, object>>(setterCall, obj, value).Compile();
        });
    }

    /// <summary>
    /// 判断属性是否静态
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsStatic(this PropertyInfo property) => (property.GetMethod ?? property.SetMethod).IsStatic;

    /// <summary>
    /// 判断属性是否为索引器属性
    /// </summary>
    /// <param name="propertyInfo">属性信息</param>
    /// <returns>如果是索引器属性返回 true，否则返回 false</returns>
    private static bool IsIndexerProperty(PropertyInfo propertyInfo)
    {
        // 索引器属性的特征：
        // 1. 属性名为 "Item"
        // 2. 有参数（索引参数）
        return propertyInfo.Name == "Item" && propertyInfo.GetIndexParameters().Length > 0;
    }
}