﻿using Bing.Extensions;
using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射 操作
/// </summary>
public static partial class Reflections
{
    #region FindTypes(查找类型列表)

    /// <summary>
    /// 查找类型列表
    /// </summary>
    /// <typeparam name="TFind">查找类型</typeparam>
    /// <param name="assemblies">待查找的程序集列表</param>
    public static List<Type> FindTypes<TFind>(params Assembly[] assemblies)
    {
        var findType = typeof(TFind);
        return FindTypes(findType, assemblies);
    }

    /// <summary>
    /// 查找类型列表
    /// </summary>
    /// <param name="findType">查找类型</param>
    /// <param name="assemblies">待查找的程序集列表</param>
    public static List<Type> FindTypes(Type findType, params Assembly[] assemblies)
    {
        var result = new List<Type>();
        foreach (var assembly in assemblies)
            result.AddRange(GetTypes(findType, assembly));
        return result.Distinct().ToList();
    }

    /// <summary>
    /// 获取类型列表
    /// </summary>
    /// <param name="findType">查找类型</param>
    /// <param name="assembly">待查找的程序集</param>
    private static List<Type> GetTypes(Type findType, Assembly assembly)
    {
        var result = new List<Type>();
        if (assembly == null)
            return result;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            return result;
        }

        foreach (var type in types)
            AddType(result, findType, type);
        return result;
    }

    /// <summary>
    /// 添加类型
    /// </summary>
    /// <param name="result">类型列表</param>
    /// <param name="findType">查找类型</param>
    /// <param name="type">类型</param>
    private static void AddType(List<Type> result, Type findType, Type type)
    {
        if (type.IsInterface || type.IsAbstract)
            return;
        if (findType.IsAssignableFrom(type) == false && MatchGeneric(findType, type) == false)
            return;
        result.Add(type);
    }

    /// <summary>
    /// 泛型匹配
    /// </summary>
    /// <param name="findType">查找类型</param>
    /// <param name="type">类型</param>
    private static bool MatchGeneric(Type findType, Type type)
    {
        if (findType.IsGenericTypeDefinition == false)
            return false;
        var definition = findType.GetGenericTypeDefinition();
        foreach (var implementedInterface in type.FindInterfaces((_, criteria) => true, null))
        {
            if (implementedInterface.IsGenericType == false)
                continue;
            return definition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition());
        }
        return false;
    }

    #endregion

    #region GetInstancesByInterface(获取实现了接口的所有实例)

    /// <summary>
    /// 获取实现了接口的所有实例
    /// </summary>
    /// <typeparam name="TInterface">接口类型</typeparam>
    /// <param name="assembly">在该程序集中查找</param>
    public static List<TInterface> GetInstancesByInterface<TInterface>(Assembly assembly)
    {
        var typeInterface = typeof(TInterface);
        return
            assembly.GetTypes()
                .Where(
                    t =>
                        typeInterface.GetTypeInfo().IsAssignableFrom(t) && t != typeInterface &&
                        t.GetTypeInfo().IsAbstract == false)
                .Select(t => CreateInstance<TInterface>(t))
                .ToList();
    }

    #endregion

    #region CreateInstance(动态创建实例)

    /// <summary>
    /// 动态创建实例
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="type">类型</param>
    /// <param name="parameters">传递给构造函数的参数</param>
    public static T CreateInstance<T>(Type type, params object[] parameters) => Conv.To<T>(Activator.CreateInstance(type, parameters));

    /// <summary>
    /// 动态创建实例
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="className">类名，包括命名空间,如果类型不处于当前执行程序集中，需要包含程序集名，范例：Test.Core.Test2,Test.Core</param>
    /// <param name="parameters">传递给构造函数的参数</param>
    public static T CreateInstance<T>(string className, params object[] parameters)
    {
        var type = Type.GetType(className) ?? Assembly.GetCallingAssembly().GetType(className);
        return CreateInstance<T>(type, parameters);
    }

    #endregion

    #region GetPropertyInfo(获取属性信息)

    /// <summary>
    /// 获取属性信息
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="propertyName">属性名</param>
    public static PropertyInfo GetPropertyInfo(Type type, string propertyName) => type.GetProperties().FirstOrDefault(p => p.Name.Equals(propertyName));

    #endregion

    #region IsBool(是否布尔类型)

    /// <summary>
    /// 是否布尔类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsBool(MemberInfo member)
    {
        if (member == null)
            return false;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return member.ToString() == "System.Boolean";

            case MemberTypes.Property:
                return IsBool((PropertyInfo)member);
        }
        return false;
    }

    /// <summary>
    /// 是否布尔类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsBool(PropertyInfo property) => property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?);

    #endregion

    #region IsEnum(是否枚举类型)

    /// <summary>
    /// 是否枚举类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsEnum(MemberInfo member)
    {
        if (member == null)
            return false;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return ((TypeInfo)member).IsEnum;

            case MemberTypes.Property:
                return IsEnum((PropertyInfo)member);
        }
        return false;
    }

    /// <summary>
    /// 是否枚举类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsEnum(PropertyInfo property)
    {
        if (property.PropertyType.GetTypeInfo().IsEnum)
            return true;
        var value = Nullable.GetUnderlyingType(property.PropertyType);
        if (value == null)
            return false;
        return value.GetTypeInfo().IsEnum;
    }

    #endregion

    #region IsDate(是否日期类型)

    /// <summary>
    /// 是否日期类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsDate(MemberInfo member)
    {
        if (member == null)
            return false;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return member.ToString() == "System.DateTime";

            case MemberTypes.Property:
                return IsDate((PropertyInfo)member);
        }
        return false;
    }

    /// <summary>
    /// 是否日期类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsDate(PropertyInfo property)
    {
        if (property.PropertyType == typeof(DateTime))
            return true;
        if (property.PropertyType == typeof(DateTime?))
            return true;
        return false;
    }

    #endregion

    #region IsInt(是否整型)

    /// <summary>
    /// 是否整型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsInt(MemberInfo member)
    {
        if (member == null)
            return false;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return member.ToString() == "System.Int32" || member.ToString() == "System.Int16" ||
                       member.ToString() == "System.Int64";

            case MemberTypes.Property:
                return IsInt((PropertyInfo)member);
        }
        return false;
    }

    /// <summary>
    /// 是否整型
    /// </summary>
    /// <param name="property">成员</param>
    public static bool IsInt(PropertyInfo property)
    {
        if (property.PropertyType == typeof(int))
            return true;
        if (property.PropertyType == typeof(int?))
            return true;
        if (property.PropertyType == typeof(short))
            return true;
        if (property.PropertyType == typeof(short?))
            return true;
        if (property.PropertyType == typeof(long))
            return true;
        if (property.PropertyType == typeof(long?))
            return true;
        return false;
    }

    #endregion

    #region IsNumber(是否数值类型)

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsNumber(MemberInfo member)
    {
        if (member == null)
            return false;
        if (IsInt(member))
            return true;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return member.ToString() == "System.Double" || member.ToString() == "System.Decimal" ||
                       member.ToString() == "System.Single";

            case MemberTypes.Property:
                return IsNumber((PropertyInfo)member);
        }
        return false;
    }

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsNumber(PropertyInfo property)
    {
        if (property.PropertyType == typeof(double))
            return true;
        if (property.PropertyType == typeof(double?))
            return true;
        if (property.PropertyType == typeof(decimal))
            return true;
        if (property.PropertyType == typeof(decimal?))
            return true;
        if (property.PropertyType == typeof(float))
            return true;
        if (property.PropertyType == typeof(float?))
            return true;
        return false;
    }

    #endregion

    #region IsCollection(是否集合)

    /// <summary>
    /// 是否集合
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsCollection(Type type) => type.IsArray || IsGenericCollection(type);

    #endregion

    #region IsGenericCollection(是否泛型集合)

    /// <summary>
    /// 是否泛型集合
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsGenericCollection(Type type)
    {
        if (!type.IsGenericType)
            return false;
        var typeDefinition = type.GetGenericTypeDefinition();
        return typeDefinition == typeof(IEnumerable<>)
               || typeDefinition == typeof(IReadOnlyCollection<>)
               || typeDefinition == typeof(IReadOnlyList<>)
               || typeDefinition == typeof(ICollection<>)
               || typeDefinition == typeof(IList<>)
               || typeDefinition == typeof(List<>);
    }

    #endregion

    #region GetPublicProperties(获取公共属性列表)

    /// <summary>
    /// 获取公共属性列表
    /// </summary>
    /// <param name="instance">实例</param>
    public static List<Item> GetPublicProperties(object instance)
    {
        var properties = instance.GetType().GetProperties();
        return properties.ToList().Select(t => new Item(t.Name, t.GetValue(instance))).ToList();
    }

    #endregion

    #region GetTopBaseType(获取顶级基类)

    /// <summary>
    /// 获取顶级基类
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Type GetTopBaseType<T>() => GetTopBaseType(typeof(T));

    /// <summary>
    /// 获取顶级基类
    /// </summary>
    /// <param name="type">类型</param>
    public static Type GetTopBaseType(Type type)
    {
        if (type == null)
            return null;
        if (type.IsInterface)
            return type;
        if (type.BaseType == typeof(object))
            return type;
        return GetTopBaseType(type.BaseType);
    }

    #endregion

    #region IsDeriveClassFrom(判断当前类型是否可由指定类型派生)

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <typeparam name="TBaseType">基类型</typeparam>
    /// <param name="type">当前类型</param>
    /// <param name="canAbstract">能否是抽象类</param>
    public static bool IsDeriveClassFrom<TBaseType>(Type type, bool canAbstract = false) => IsDeriveClassFrom(type, typeof(TBaseType), canAbstract);

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <param name="type">当前类型</param>
    /// <param name="baseType">基类型</param>
    /// <param name="canAbstract">能否是抽象类</param>
    public static bool IsDeriveClassFrom(Type type, Type baseType, bool canAbstract = false)
    {
        Check.NotNull(type, nameof(type));
        Check.NotNull(baseType, nameof(baseType));
        return type.IsClass && (!canAbstract && !type.IsAbstract) && type.IsBaseOn(baseType);
    }

    #endregion

    #region IsBaseOn(返回当前类型是否是指定基类的派生类)

    /// <summary>
    /// 返回当前类型是否是指定基类的派生类
    /// </summary>
    /// <typeparam name="TBaseType">基类型</typeparam>
    /// <param name="type">类型</param>
    public static bool IsBaseOn<TBaseType>(Type type) => IsBaseOn(type, typeof(TBaseType));

    /// <summary>
    /// 返回当前类型是否是指定基类的派生类
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="baseType">基类类型</param>
    public static bool IsBaseOn(Type type, Type baseType) => baseType.IsGenericTypeDefinition
        ? baseType.IsGenericAssignableFrom(type)
        : baseType.IsAssignableFrom(type);

    #endregion

    #region IsGenericAssignableFrom(判断当前泛型类型是否可由指定类型的实例填充)

    /// <summary>
    /// 判断当前泛型类型是否可由指定类型的实例填充
    /// </summary>
    /// <param name="genericType">泛型类型</param>
    /// <param name="type">指定类型</param>
    public static bool IsGenericAssignableFrom(Type genericType, Type type)
    {
        Check.NotNull(genericType, nameof(genericType));
        Check.NotNull(type, nameof(type));
        if (!genericType.IsGenericType)
            throw new ArgumentException("该功能只支持泛型类型的调用，非泛型类型可使用 IsAssignableFrom 方法。");
        var allOthers = new List<Type>() { type };
        if (genericType.IsInterface) allOthers.AddRange(type.GetInterfaces());

        foreach (var other in allOthers)
        {
            var cur = other;
            while (cur != null)
            {
                if (cur.IsGenericType)
                    cur = cur.GetGenericTypeDefinition();
                if (cur.IsSubclassOf(genericType) || cur == genericType)
                    return true;
                cur = cur.BaseType;
            }
        }
        return false;
    }

    #endregion

    #region GetElementType(获取元素类型)

    /// <summary>
    /// 获取元素类型。如果是集合，返回集合的元素类型
    /// </summary>
    /// <param name="type">类型</param>
    public static Type GetElementType(Type type)
    {
        if (IsCollection(type) == false)
            return type;
        if (type.IsArray)
            return type.GetElementType();
        var genericArgumentsTypes = type.GetTypeInfo().GetGenericArguments();
        if (genericArgumentsTypes == null || genericArgumentsTypes.Length == 0)
            throw new ArgumentException("泛型类型参数不能为空");
        return genericArgumentsTypes[0];
    }

    #endregion

    #region IsAssignableTo(检查类型是否可分配给目标类型)

    /// <summary>
    /// 检查类型是否可分配给目标类型 <typeparamref name="TTarget"/>。<br />
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> 方法。
    /// </summary>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="type">要检查的类型。</param>
    /// <returns>如果类型可分配给目标类型，则为 true；否则为 false。</returns>
    public static bool IsAssignableTo<TTarget>(Type type)
    {
        Check.NotNull(type, nameof(type));
        return IsAssignableTo(type, typeof(TTarget));
    }

    /// <summary>
    /// 检查类型是否可分配给目标类型。<br />
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> 方法。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>如果类型可分配给目标类型，则为 true；否则为 false。</returns>
    public static bool IsAssignableTo(Type type, Type targetType)
    {
        Check.NotNull(type, nameof(type));
        Check.NotNull(targetType, nameof(targetType));
        return targetType.IsAssignableFrom(type);
    }

    #endregion

    #region GetBaseClasses(获取指定类型的所有基类型)

    /// <summary>
    /// 获取指定类型及其基类型的数组。
    /// </summary>
    /// <param name="type">要获取基类型的类型。</param>
    /// <param name="includeObject">是否包含 <see cref="object"/> 类型（默认为 true）。</param>
    /// <returns>包含类型及其基类型的数组。</returns>
    public static Type[] GetBaseClasses(Type type, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));
        var types = new List<Type>();
        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject);
        return types.ToArray();
    }

    /// <summary>
    /// 获取指定类型的所有基类型
    /// </summary>
    /// <param name="type">要获取基类型的类型。</param>
    /// <param name="stoppingType">停止添加的类型。</param>
    /// <param name="includeObject">是否包含 <see cref="object"/> 类型（默认为 true）。</param>
    /// <returns>包含类型及其基类型的数组。</returns>
    public static Type[] GetBaseClasses(Type type, Type stoppingType, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));
        var types = new List<Type>();
        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        return types.ToArray();
    }

    /// <summary>
    /// 递归地添加类型及其基类型到类型列表。
    /// </summary>
    /// <param name="types">要添加到的类型列表。</param>
    /// <param name="type">要添加的类型。</param>
    /// <param name="includeObject">是否包含 <see cref="object"/> 类型。</param>
    /// <param name="stoppingType">停止添加的类型（可选）。</param>
    private static void AddTypeAndBaseTypesRecursively(List<Type> types, Type type, bool includeObject, Type stoppingType = null)
    {
        if (type == null || type == stoppingType)
            return;
        if (!includeObject && type == typeof(object))
            return;
        AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        types.Add(type);
    }

    #endregion
}