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
    public static FieldInfo GetField<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>(string name)
    {
        ValidateMemberName(name);
        return FindPublicField(typeof(T), name);
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的字段信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">成员名</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static FieldInfo GetField<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
        T>(string name, BindingFlags bindingFlags)
    {
        ValidateMemberName(name);
        return FindFieldWithFlags(typeof(T), name, bindingFlags);
    }

    private static FieldInfo FindPublicField(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        Type type, string name)
    {
        var fields = TypeReflections.TypeCacheManager.GetTypeFields(type);
        return SelectClosestField(type, name, fields, StringComparison.Ordinal)
               ?? throw new MissingFieldException(type.FullName, name);
    }

    private static FieldInfo FindFieldWithFlags(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields)]
#endif
        Type type, string name, BindingFlags? bindingFlags)
    {
        var comparison = (bindingFlags.GetValueOrDefault() & BindingFlags.IgnoreCase) != 0
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var fields = TypeReflections.TypeCacheManager.GetTypeFields(type, bindingFlags.Value);
        return SelectClosestField(type, name, fields, comparison)
               ?? throw new MissingFieldException(type.FullName, name);
    }

    private static FieldInfo SelectClosestField(Type type, string name, IEnumerable<FieldInfo> fields,
        StringComparison comparison)
    {
        FieldInfo selected = null;
        var selectedDepth = int.MaxValue;
        var ambiguous = false;
        foreach (var field in fields)
        {
            if (!string.Equals(field.Name, name, comparison))
                continue;
            var depth = 0;
            for (var declaringType = type; declaringType != null && declaringType != field.DeclaringType;
                 declaringType = declaringType.BaseType)
                depth++;
            if (depth < selectedDepth)
            {
                selected = field;
                selectedDepth = depth;
                ambiguous = false;
            }
            else if (depth == selectedDepth)
                ambiguous = true;
        }
        if (ambiguous)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个名为“{name}”的字段。");
        return selected;
    }

    #endregion

    #region GetFields(获取指定对象的字段信息数组)

    /// <summary>
    /// 获取指定对象的所有公共字段信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
 #if NET5_0_OR_GREATER
     [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("对象的运行时类型由 object.GetType() 决定，裁剪时必须保留目标类型的字段元数据。")]
 #endif
    public static FieldInfo[] GetFields(object @this)
    {
        return TypeReflections.TypeCacheManager.GetTypeFields(Check.NotNull(@this, nameof(@this)).GetType());
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的字段信息数组
    /// </summary>
    /// <param name="this">当前对象</param>
    /// <param name="bindingFlags">绑定标记</param>
 #if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("对象的运行时类型由 object.GetType() 决定，裁剪时必须保留目标类型的字段元数据。")]
 #endif
    public static FieldInfo[] GetFields(object @this, BindingFlags bindingFlags)
    {
        return TypeReflections.TypeCacheManager.GetTypeFields(Check.NotNull(@this, nameof(@this)).GetType(), bindingFlags);
    }

    #endregion

    #region GetFieldValue(获取指定对象的字段值)

    /// <summary>
    /// 获取指定对象的公共字段值。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="fieldName">字段名</param>
    public static object GetFieldValue<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>(T @this, string fieldName)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("字段名不能为空。", nameof(fieldName));
        var field = GetField<T>(fieldName);
        if (field == null)
            throw new MissingFieldException(typeof(T).FullName, fieldName);
        return field.GetValue(@this);
    }

    #endregion

    #region GetMethod(获取指定对象的方法信息)

    /// <summary>
    /// 获取指定对象的方法信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">方法名</param>
    /// <returns></returns>
    public static MethodInfo GetMethod<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        T>(string name)
    {
        ValidateMemberName(name);
        var methods = TypeReflections.TypeCacheManager.GetTypeMethods(typeof(T))
            .Where(method => method.Name == name).Take(2).ToArray();
        if (methods.Length > 1)
            throw new AmbiguousMatchException($"类型“{typeof(T).FullName}”中存在多个名为“{name}”的公共方法。");
        return methods.Length == 1 ? methods[0] : throw new MissingMethodException(typeof(T).FullName, name);
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的方法信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">方法名</param>
    /// <param name="bindingFlags">绑定标记</param>
    public static MethodInfo GetMethod<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
        T>(string name, BindingFlags bindingFlags)
    {
        ValidateMemberName(name);
        var comparison = (bindingFlags & BindingFlags.IgnoreCase) != 0
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var methods = TypeReflections.TypeCacheManager.GetTypeMethods(typeof(T), bindingFlags)
            .Where(method => string.Equals(method.Name, name, comparison)).Take(2).ToArray();
        if (methods.Length > 1)
            throw new AmbiguousMatchException($"类型“{typeof(T).FullName}”中存在多个名为“{name}”的方法。");
        return methods.Length == 1 ? methods[0] : throw new MissingMethodException(typeof(T).FullName, name);
    }

    #endregion

    #region GetMethods(获取指定对象的方法信息数组)

    /// <summary>
    /// 获取指定对象的所有公共方法信息数组
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static MethodInfo[] GetMethods<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        T>()
    {
        return TypeReflections.TypeCacheManager.GetTypeMethods(typeof(T));
    }

    /// <summary>
    /// 获取指定对象的指定 <see cref="BindingFlags"/> 的方法信息数组
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="bindingFlags">绑定标记</param>
    public static MethodInfo[] GetMethods<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
        T>(BindingFlags bindingFlags)
    {
        return TypeReflections.TypeCacheManager.GetTypeMethods(typeof(T), bindingFlags);
    }

    #endregion

    #region GetProperty(获取指定对象的属性信息)

    /// <summary>
    /// 获取指定类型中具有给定名称的公共属性。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">要查找的属性的名称。</param>
    /// <returns>匹配给定名称的公共属性；不存在时抛出 <see cref="MissingMemberException"/>。</returns>
    public static PropertyInfo GetProperty<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(string name) => GetProperty(typeof(T), name);

    /// <summary>
    /// 获取指定类型中具有给定名称的公共属性。
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="name">要查找的属性的名称。</param>
    /// <returns>匹配给定名称的公共属性；不存在时抛出 <see cref="MissingMemberException"/>。</returns>
    public static PropertyInfo GetProperty(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        Type type, string name)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        ValidateMemberName(name);
        var properties = TypeReflections.TypeCacheManager.GetTypeProperties(type)
            .Where(property => property.Name == name).Take(2).ToArray();
        if (properties.Length > 1)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个名为“{name}”的公共属性。");
        return properties.Length == 1 ? properties[0] : throw new MissingMemberException(type.FullName, name);
    }

    /// <summary>
    /// 获取指定类型具有指定名称和绑定标志的属性信息。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">要查找的属性的名称。</param>
    /// <param name="bindingFlags">用于控制搜索的绑定约束（如公共或非公共、静态或实例等）。</param>
    /// <returns>匹配指定名称和绑定标志的属性；不存在时抛出 <see cref="MissingMemberException"/>。</returns>
    public static PropertyInfo GetProperty<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        T>(string name, BindingFlags bindingFlags) => GetProperty(typeof(T), name, bindingFlags);

    /// <summary>
    /// 获取指定类型具有指定名称和绑定标志的属性信息
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="name">要查找的属性的名称。</param>
    /// <param name="bindingFlags">用于控制搜索的绑定约束（如公共或非公共、静态或实例等）。</param>
    /// <returns>匹配指定名称和绑定标志的属性；不存在时抛出 <see cref="MissingMemberException"/>。</returns>
    public static PropertyInfo GetProperty(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type, string name, BindingFlags bindingFlags)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        ValidateMemberName(name);
        var comparison = (bindingFlags & BindingFlags.IgnoreCase) != 0
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var properties = TypeReflections.TypeCacheManager.GetTypeProperties(type, bindingFlags)
            .Where(property => string.Equals(property.Name, name, comparison)).Take(2).ToArray();
        if (properties.Length > 1)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个名为“{name}”的属性。");
        return properties.Length == 1 ? properties[0] : throw new MissingMemberException(type.FullName, name);
    }

    #endregion

    #region GetProperties(获取指定对象的属性信息数组)

    /// <summary>
    /// 获取指定类型的所有公共属性信息。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>包含指定类型所有公共属性的PropertyInfo数组。如果指定的类型没有公共属性，则返回空数组。</returns>
    public static PropertyInfo[] GetProperties<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => GetProperties(typeof(T));

    /// <summary>
    /// 获取指定类型的所有公共属性信息。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>包含指定类型所有公共属性的PropertyInfo数组。如果指定的类型没有公共属性，则返回空数组。</returns>
    public static PropertyInfo[] GetProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        Type type) => TypeReflections.TypeCacheManager.GetTypeProperties(type);

    /// <summary>
    /// 获取指定类型的属性信息，根据提供的绑定标志筛选结果。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="bindingFlags">用于控制搜索的绑定约束（如公共或非公共、静态或实例等）。</param>
    /// <returns>根据指定绑定标志找到的PropertyInfo对象数组。如果没有找到匹配的属性，则返回空数组。</returns>
    public static PropertyInfo[] GetProperties<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        T>(BindingFlags bindingFlags) => GetProperties(typeof(T), bindingFlags);

    /// <summary>
    /// 获取指定类型的属性信息，根据提供的绑定标志筛选结果。
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="bindingFlags">用于控制搜索的绑定约束（如公共或非公共、静态或实例等）。</param>
    /// <returns>根据指定绑定标志找到的PropertyInfo对象数组。如果没有找到匹配的属性，则返回空数组。</returns>
    public static PropertyInfo[] GetProperties(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
        Type type, BindingFlags bindingFlags) =>
        TypeReflections.TypeCacheManager.GetTypeProperties(type, bindingFlags);

    #endregion

    #region GetProerptyOrField(获取指定对象的属性或字段信息)

    /// <summary>
    /// 获取指定对象的属性或字段信息
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="name">名称</param>
    public static MemberInfo GetPropertyOrField<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>(string name)
    {
        ValidateMemberName(name);
        var properties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(T))
            .Where(candidate => candidate.Name == name).Take(2).ToArray();
        if (properties.Length > 1)
            throw new AmbiguousMatchException($"类型“{typeof(T).FullName}”中存在多个名为“{name}”的公共属性。");
        if (properties.Length == 1)
            return properties[0];
        return GetField<T>(name);
    }

    private static void ValidateMemberName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("成员名称不能为空。", nameof(name));
    }

    #endregion

    #region GetPropertyValue(获取指定对象的属性值)

    /// <summary>
    /// 获取指定对象的属性值
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">当前对象</param>
    /// <param name="propertyName">属性名</param>
 #if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("属性访问器通过 Expression.Compile 生成，要求运行时支持动态代码。")]
 #endif
 #if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
 #endif
    public static object GetPropertyValue<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(T @this, string propertyName)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("属性名不能为空。", nameof(propertyName));
        var property = GetProperty<T>(propertyName);
        var getter = property?.GetValueGetter<T>();
        if (getter == null)
            throw new MissingMemberException(typeof(T).FullName, propertyName);
        return getter(@this);
    }

    #endregion

    #region GetPropertyValueByPath(获取指定对象的属性值)

    /// <summary>
    /// 通过指定对象的属性路径获取属性值。
    /// </summary>
    /// <param name="obj">要获取属性值的对象。</param>
    /// <param name="objectType">对象的类型。</param>
    /// <param name="propertyPath">属性的路径，可以包括嵌套属性，如 "NestedObject.PropertyName"。</param>
    /// <returns>路径对应的属性值；路径不存在、不可读取或中间值为空时抛出异常。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性元数据，裁剪时必须保留路径涉及的成员。")]
#endif
    public static object GetPropertyValueByPath(object obj, Type objectType, string propertyPath)
    {
        ValidatePathTarget(obj, objectType);
        return GetMemberValueByPath(obj, objectType, propertyPath, new MemberPathOptions { IncludeFields = false });
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
 #if NET5_0_OR_GREATER
     [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("字段按泛型声明类型解析，裁剪时必须保留该类型的公共字段。")]
 #endif
    public static void SetFieldValue<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>(T @this, string fieldName, object value)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (typeof(T).IsValueType)
            throw new NotSupportedException("值类型按值传递只能修改副本，请使用 ref 参数重载。");
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("字段名不能为空。", nameof(fieldName));
        var type = typeof(T);
        var field = GetField<T>(fieldName);
        if (field == null)
            throw new MissingFieldException(type.FullName, fieldName);
        if (field.IsInitOnly || field.IsLiteral)
            throw new InvalidOperationException($"字段“{fieldName}”不可写。");
        field.SetValue(@this, value);
    }

    /// <summary>设置结构体字段并将修改回写到原变量。</summary>
    public static void SetFieldValue<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        T>(ref T instance, string fieldName, object value) where T : struct
    {
        ValidateMemberName(fieldName);
        var field = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new MissingFieldException(typeof(T).FullName, fieldName);
        if (field.IsInitOnly || field.IsLiteral)
            throw new InvalidOperationException($"字段“{fieldName}”不可写。");
        object boxed = instance;
        field.SetValue(boxed, value);
        instance = (T)boxed;
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
 #if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("属性访问器通过 Expression.Compile 生成，要求运行时支持动态代码。")]
 #endif
 #if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("属性访问器依赖运行时属性元数据，裁剪时必须保留目标属性及其访问器。")]
 #endif
    public static void SetPropertyValue<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(T @this, string propertyName, object value) where T : class
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("属性名不能为空。", nameof(propertyName));
        var property = GetProperty<T>(propertyName);
        var setter = property?.GetValueSetter();
        if (setter == null)
            throw new MissingMemberException(typeof(T).FullName, propertyName);
        setter(@this, value);
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
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性元数据，裁剪时必须保留路径涉及的成员。")]
#endif
    public static void SetPropertyValueByPath(object obj, Type objectType, string propertyPath, object value)
    {
        ValidatePathTarget(obj, objectType);
        SetMemberValueByPath(obj, objectType, propertyPath, value, new MemberPathOptions { IncludeFields = false });
    }

    private static void ValidatePathTarget(object instance, Type objectType)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (objectType == null)
            throw new ArgumentNullException(nameof(objectType));
        if (!objectType.IsInstanceOfType(instance))
            throw new ArgumentException("目标对象不属于指定类型。", nameof(objectType));
    }

    #endregion

    #region GetPublicConstantsRecursively(获取指定类型的所有公共常量值)

    /// <summary>
    /// 递归获取指定类型（包括基类型）及其嵌套类型中的公共常量值。
    /// </summary>
    /// <param name="type">要获取常量的类型。</param>
    /// <returns>包含公共常量值的字符串数组。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("公共常量和嵌套类型通过运行时元数据递归扫描，裁剪时必须保留相关字段和嵌套类型。")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "递归扫描的子类型来自运行时 Type.GetNestedTypes，保留策略由调用方承担。")]
#endif
    public static string[] GetPublicConstantsRecursively(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        const int maxRecursiveParameterValidationDepth = 8;
        var publicConstants = new List<string>();

        void Recursively(List<string> constants, Type targetType, int currentDepth)
        {
            if (currentDepth > maxRecursiveParameterValidationDepth)
                return;
            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                         .Where(x => x.IsLiteral && !x.IsInitOnly))
            {
                var value = field.GetValue(null);
                if (value is not null)
                    constants.Add(value.ToString()!);
            }
            var nestedTypes = targetType.GetNestedTypes(BindingFlags.Public);
            foreach (var nestedType in nestedTypes)
                Recursively(constants, nestedType, currentDepth + 1);
        }

        Recursively(publicConstants, type, 1);
        return publicConstants.ToArray();
    }


    #endregion
}
