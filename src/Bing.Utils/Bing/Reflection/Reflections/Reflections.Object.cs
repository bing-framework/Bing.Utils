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
}