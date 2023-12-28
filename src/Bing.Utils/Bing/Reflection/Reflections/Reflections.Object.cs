using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

// 反射 - 对象
public static partial class Reflections
{
    #region GetField(获取指定对象的字段信息)

    /// <summary>
    /// 获取指定对象的字段信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">成员名</param>
    public static FieldInfo GetField<T>(string name)
    {
        return TypeReflections.TypeCacheManager.GetTypeFields(typeof(T)).FirstOrDefault(_ => _.Name == name);
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的字段信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">成员名</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static FieldInfo GetField<T>(string name, BindingFlags bindingFlags)
    {
        return typeof(T).GetField(name, bindingFlags);
    }

    #endregion

    #region GetFields(获取指定对象的字段信息数组)

    /// <summary>
    /// 获取指定对象的所有公共字段信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
    public static FieldInfo[] GetFields(object @this)
    {
        return TypeReflections.TypeCacheManager.GetTypeFields(Check.NotNull(@this, nameof(@this)).GetType());
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的字段信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static FieldInfo[] GetFields(object @this, BindingFlags bindingFlags)
    {
        return @this.GetType().GetFields(bindingFlags);
    }

    #endregion

    #region GetFieldValue(获取指定对象的字段值)

    /// <summary>
    /// 获取指定对象的字段值 (Public | NonPublic | Instance | Static)
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="fieldName">字段名</param>
    public static object GetFieldValue<T>(T @this, string fieldName)
    {
        var field = GetField<T>(fieldName);
        return field?.GetValue(@this);
    }

    #endregion

    #region GetMethod(获取指定对象的方法信息)

    /// <summary>
    /// 获取指定对象的方法信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">方法名</param>
    /// <returns></returns>
    public static MethodInfo GetMethod<T>(string name)
    {
        return TypeReflections.TypeCacheManager.TypeMethodCache.GetOrAdd(typeof(T), t => t.GetMethods())
            .FirstOrDefault(_ => _.Name == name);
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的方法信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">方法名</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static MethodInfo GetMethod<T>(string name, BindingFlags bindingFlags)
    {
        return typeof(T).GetMethod(name, bindingFlags);
    }

    #endregion

    #region GetMethods(获取指定对象的方法信息数组)

    /// <summary>
    /// 获取指定对象的所有公共方法信息数组
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static MethodInfo[] GetMethods<T>()
    {
        return TypeReflections.TypeCacheManager.TypeMethodCache.GetOrAdd(typeof(T), t => t.GetMethods());
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的方法信息数组
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="bindingFlags">绑定标记</param>
    public static MethodInfo[] GetMethods<T>( BindingFlags bindingFlags)
    {
        return typeof(T).GetMethods(bindingFlags);
    }

    #endregion

    #region GetProperty(获取指定对象的属性信息)

    /// <summary>
    /// 获取指定对象的属性信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">属性名</param>
    public static PropertyInfo GetProperty<T>(string name)
    {
        return TypeReflections.TypeCacheManager.GetTypeProperties(typeof(T)).FirstOrDefault(_ => _.Name == name);
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的属性信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">属性名</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static PropertyInfo GetProperty<T>(string name, BindingFlags bindingFlags)
    {
        return typeof(T).GetProperty(name, bindingFlags);
    }

    #endregion

    #region GetProperties(获取指定对象的属性信息数组)

    /// <summary>
    /// 获取指定对象的所有公共属性信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
    public static PropertyInfo[] GetProperties(object @this)
    {
        return TypeReflections.TypeCacheManager.GetTypeProperties(@this.GetType());
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的属性信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
    /// <param name="bindingFlags">绑定标记</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static PropertyInfo[] GetProperties(object @this, BindingFlags bindingFlags)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        return @this.GetType().GetProperties(bindingFlags);
    }

    #endregion

    #region GetProerptyOrField(获取指定对象的属性或字段信息)

    /// <summary>
    /// 获取指定对象的属性或字段信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">名称</param>
    public static MemberInfo GetPropertyOrField<T>(string name)
    {
        var property = GetProperty<T>(name);
        if (property != null)
            return property;
        var field = GetField<T>(name);
        return field;
    }

    #endregion

    #region GetPropertyValue(获取指定对象的属性值)

    /// <summary>
    /// 获取指定对象的属性值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="propertyName">属性名</param>
    public static object GetPropertyValue<T>(T @this, string propertyName)
    {
        var property = GetProperty<T>(propertyName);
        return property?.GetValueGetter<T>()?.Invoke(@this);
    }

    #endregion

    #region GetPropertyValueByPath(获取指定对象的属性值)

    /// <summary>
    /// 通过指定对象的属性路径获取属性值。
    /// </summary>
    /// <param name="obj">要获取属性值的对象。</param>
    /// <param name="objectType">对象的类型。</param>
    /// <param name="propertyPath">属性的路径，可以包括嵌套属性，如 "NestedObject.PropertyName"。</param>
    /// <returns>属性值，如果属性不存在或获取过程中发生错误，则返回 null。</returns>
    public static object GetPropertyValueByPath(object obj, Type objectType, string propertyPath)
    {
        var value = obj;
        var currentType = objectType;
        var objectPath = currentType.FullName;
        var absolutePropertyPath = propertyPath;
        if (objectPath != null && absolutePropertyPath.StartsWith(objectPath))
            absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");
        foreach (var propertyName in absolutePropertyPath.Split('.'))
        {
            var property = currentType.GetProperty(propertyName);
            if (property != null)
            {
                if (value != null)
                    value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }
            else
            {
                value = null;
                break;
            }
        }
        return value;
    }

    #endregion

    #region SetFieldValue(给指定对象设置字段值)

    /// <summary>
    /// 给指定对象设置字段值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="value">值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void SetFieldValue<T>(T @this, string fieldName, object value)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        var type = @this.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        field?.SetValue(@this, value);
    }

    #endregion

    #region SetPropertyValue(给指定对象设置属性值)

    /// <summary>
    /// 给指定对象设置属性值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    public static void SetPropertyValue<T>(T @this, string propertyName, object value) where T : class
    {
        var property = GetProperty<T>(propertyName);
        property?.GetValueSetter()?.Invoke(@this, value);
    }

    #endregion

    #region SetPropertyValueByPath(给指定对象设置属性值)

    /// <summary>
    /// 通过指定对象的属性路径设置属性值。
    /// </summary>
    /// <param name="obj">要设置属性值的对象。</param>
    /// <param name="objectType">对象的类型。</param>
    /// <param name="propertyPath">属性的路径，可以包括嵌套属性，如 "NestedObject.PropertyName"。</param>
    /// <param name="value">要设置的属性值。</param>
    public static void SetPropertyValueByPath(object obj, Type objectType, string propertyPath, object value)
    {
        var currentType = objectType;
        PropertyInfo property;
        var objectPath = currentType.FullName!;
        var absolutePropertyPath = propertyPath;
        if (absolutePropertyPath.StartsWith(objectPath))
            absolutePropertyPath = absolutePropertyPath.Replace(objectPath + ".", "");

        var properties = absolutePropertyPath.Split('.');
        if (properties.Length == 1)
        {
            property = objectType.GetProperty(properties.First())!;
            property.SetValue(obj, value);
            return;
        }

        for (var i = 0; i < properties.Length-1; i++)
        {
            property = currentType.GetProperty(properties[i])!;
            obj = property.GetValue(obj, null)!;
            currentType = property.PropertyType;
        }

        property = currentType.GetProperty(properties.Last())!;
        property.SetValue(obj, value);
    }

    #endregion
}