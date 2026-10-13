using Bing.Extensions;
using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射 操作
/// </summary>
public static partial class Reflections
{
    #region CreateInstance(动态创建实例)

    /// <summary>
    /// 动态创建实例
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="type">类型</param>
    /// <param name="parameters">传递给构造函数的参数</param>
    /// <remarks>只匹配兼容的公共构造函数，不执行数值转换或返回值转换。</remarks>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("构造函数按运行时类型和参数匹配，裁剪时必须保留目标类型的公共构造函数。")]
#endif
    public static T CreateInstance<T>(Type type, params object[] parameters)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (!typeof(T).IsAssignableFrom(type))
            throw new ArgumentException($"类型“{type.FullName}”不可赋值给“{typeof(T).FullName}”。", nameof(type));
        return (T)StrictConstructorBinder.CreateInstance(type, parameters);
    }

    /// <summary>
    /// 动态创建实例
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="className">类名，包括命名空间,如果类型不处于当前执行程序集中，需要包含程序集名，范例：Test.Core.Test2,Test.Core</param>
    /// <param name="parameters">传递给构造函数的参数</param>
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("构造函数委托通过 Expression.Compile 生成，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("按名称解析类型并匹配构造函数，裁剪时必须保留目标类型和公共构造函数。")]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static T CreateInstance<T>(string className, params object[] parameters)
    {
        if (className is null)
            throw new ArgumentNullException(nameof(className));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("类型名称不能为空。", nameof(className));
        var type = Type.GetType(className) ?? Assembly.GetCallingAssembly().GetType(className);
        if (type is null)
            throw new ArgumentException($"无法解析指定类型名称“{className}”。", nameof(className));
        return CreateInstance<T>(type, parameters);
    }

    #endregion

    #region FindImplementTypes(查找实现类型列表)

    /// <summary>
    /// 在指定的程序集中查找实现类型列表
    /// </summary>
    /// <typeparam name="TFind">查找类型</typeparam>
    /// <param name="assemblies">待查找的程序集列表</param>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("程序集类型扫描依赖运行时元数据，裁剪时必须保留目标程序集中的类型。")]
#endif
    public static List<Type> FindImplementTypes<TFind>(params Assembly[] assemblies) => FindImplementTypes(typeof(TFind), assemblies);

    /// <summary>
    /// 在指定的程序集中查找实现类型列表
    /// </summary>
    /// <param name="findType">查找类型</param>
    /// <param name="assemblies">待查找的程序集列表</param>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("程序集类型扫描依赖运行时元数据，裁剪时必须保留目标程序集中的类型。")]
#endif
    public static List<Type> FindImplementTypes(Type findType, params Assembly[] assemblies)
    {
        Check.NotNull(findType, nameof(findType));
        Check.NotNull(assemblies, nameof(assemblies));
        if (assemblies.Any(assembly => assembly is null))
            throw new ArgumentException("程序集列表不能包含 null。", nameof(assemblies));
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
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("程序集类型扫描依赖运行时元数据，裁剪时必须保留目标程序集中的类型。")]
#endif
    private static List<Type> GetTypes(Type findType, Assembly assembly)
    {
        var result = new List<Type>();
        if (assembly == null)
            return result;
        var types = GetLoadableTypes(assembly);
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
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "调用方 FindImplementTypes 已声明程序集扫描的裁剪风险；此处处理的是扫描得到的运行时类型。")]
#endif
    private static bool MatchGeneric(Type findType, Type type)
    {
        if (findType.IsGenericTypeDefinition == false)
            return false;
        var definition = findType.GetGenericTypeDefinition();
        foreach (var implementedInterface in type.FindInterfaces((_, criteria) => true, null))
        {
            if (implementedInterface.IsGenericType == false)
                continue;
            if (definition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition()))
                return true;
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
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("程序集中的实现类型通过运行时构造函数委托实例化，要求运行时支持动态代码。")]
#endif
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("程序集类型扫描和构造函数匹配依赖运行时元数据，裁剪时必须保留实现类型及其公共构造函数。")]
#endif
    public static List<TInterface> GetInstancesByInterface<TInterface>(Assembly assembly)
    {
        Check.NotNull(assembly, nameof(assembly));
        var typeInterface = typeof(TInterface);
        return
            GetLoadableTypes(assembly)
                .Where(
                    t =>
                        typeInterface.GetTypeInfo().IsAssignableFrom(t) && t != typeInterface &&
                        t.GetTypeInfo().IsAbstract == false)
                .Select(t => CreateInstance<TInterface>(t))
                .ToList();
    }

    /// <summary>
    /// 获取程序集当前能够加载的类型；程序集存在部分类型加载失败时保留可用类型。
    /// </summary>
    /// <param name="assembly">程序集。</param>
    /// <returns>可加载类型数组。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Assembly.GetTypes 依赖运行时类型元数据，裁剪后可能缺少未静态引用的类型。")]
#endif
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null);
        }
    }

    #endregion

    #region GetDirectInterfaceTypes(获取直接接口类型列表)

    /// <summary>
    /// 获取直接接口类型列表，排除基接口类型。
    /// </summary>
    /// <typeparam name="T">在该类型上查找接口</typeparam>
    /// <param name="baseInterfaceTypes">基接口类型列表，只返回继承了基接口的直接接口。</param>
    /// <returns>直接实现的接口类型列表。如果提供了基接口类型，只返回同时满足直接实现和基接口条件的类型。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("接口图通过运行时元数据遍历；裁剪时必须保留目标类型的接口元数据。")]
#endif
    public static List<Type> GetDirectInterfaceTypes<T>(params Type[] baseInterfaceTypes) => GetDirectInterfaceTypes(typeof(T), baseInterfaceTypes);

    /// <summary>
    /// 获取直接接口类型列表，排除基接口类型。
    /// </summary>
    /// <param name="type">在该类型上查找接口</param>
    /// <param name="baseInterfaceTypes">基接口类型列表，只返回继承了基接口的直接接口。</param>
    /// <returns>直接实现的接口类型列表。如果提供了基接口类型，只返回同时满足直接实现和基接口条件的类型。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("接口图通过运行时元数据遍历；裁剪时必须保留目标类型的接口元数据。")]
#endif
    public static List<Type> GetDirectInterfaceTypes(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, params Type[] baseInterfaceTypes)
    {
        Check.NotNull(type, nameof(type));
        ValidateBaseInterfaceTypes(baseInterfaceTypes);
        var interfaceTypes = type.GetInterfaces();
        var directInterfaceTypes = interfaceTypes.Except(interfaceTypes.SelectMany(t => t.GetInterfaces())).ToList();
        if (baseInterfaceTypes == null || baseInterfaceTypes.Length == 0)
            return directInterfaceTypes;
        return GetInterfaceTypes(directInterfaceTypes, baseInterfaceTypes);
    }

    /// <summary>
    /// 从给定的接口类型集合中筛选出实现了指定基接口的类型。
    /// </summary>
    /// <param name="interfaceTypes">要检查的接口类型集合。</param>
    /// <param name="baseInterfaceTypes">检查实现的基接口类型数组。</param>
    /// <returns>实现了指定基接口的接口类型列表。如果接口类型为泛型且不是泛型定义本身，则添加其泛型定义。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "接口集合来自运行时 Assembly/Type 扫描，IEnumerable<Type> 无法表达元素级接口元数据要求。")]
#endif
    private static List<Type> GetInterfaceTypes(IEnumerable<Type> interfaceTypes, Type[] baseInterfaceTypes)
    {
        var result = new List<Type>();
        foreach (var interfaceType in interfaceTypes)
        {
            // 检查当前接口是否实现了任一基接口
            if (interfaceType.GetInterfaces().Any(baseInterfaceTypes.Contains) == false)
                continue;
            // 如果接口是具体化的泛型且没有完整名称，则添加其泛型定义
            if (interfaceType.IsGenericType && !interfaceType.IsGenericTypeDefinition && interfaceType.FullName == null)
            {
                result.Add(interfaceType.GetGenericTypeDefinition());
                continue;
            }
            result.Add(interfaceType);
        }
        return result;
    }

    #endregion

    #region GetInterfaceTypes(获取接口类型列表)

    /// <summary>
    /// 获取接口类型列表，排除基接口类型。
    /// </summary>
    /// <typeparam name="T">在该类型上查找接口</typeparam>
    /// <param name="baseInterfaceTypes">用于筛选的基接口类型数组。只有实现了这些接口的类型才会被包含在结果中。</param>
    /// <returns>满足条件的接口类型列表。如果没有提供基接口类型或没有找到满足条件的接口，则返回该类型实现的所有接口。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("接口图通过运行时元数据遍历；裁剪时必须保留目标类型的接口元数据。")]
#endif
    public static List<Type> GetInterfaceTypes<T>(params Type[] baseInterfaceTypes) => GetInterfaceTypes(typeof(T), baseInterfaceTypes);

    /// <summary>
    /// 获取接口类型列表，排除基接口类型。
    /// </summary>
    /// <param name="type">在该类型上查找接口</param>
    /// <param name="baseInterfaceTypes">用于筛选的基接口类型数组。只有实现了这些接口的类型才会被包含在结果中。</param>
    /// <returns>满足条件的接口类型列表。如果没有提供基接口类型或没有找到满足条件的接口，则返回该类型实现的所有接口。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("接口图通过运行时元数据遍历；裁剪时必须保留目标类型的接口元数据。")]
#endif
    public static List<Type> GetInterfaceTypes(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, params Type[] baseInterfaceTypes)
    {
        Check.NotNull(type, nameof(type));
        ValidateBaseInterfaceTypes(baseInterfaceTypes);
        var interfaceTypes = type.GetInterfaces();
        if (baseInterfaceTypes == null || baseInterfaceTypes.Length == 0)
            return interfaceTypes.ToList();
        return GetInterfaceTypes(interfaceTypes, baseInterfaceTypes);
    }

    private static void ValidateBaseInterfaceTypes(Type[] baseInterfaceTypes)
    {
        // null 数组表示未指定筛选条件；数组中的 null 不是合法类型。
        if (baseInterfaceTypes?.Any(type => type is null) == true)
            throw new ArgumentException("基接口类型列表不能包含 null。", nameof(baseInterfaceTypes));
    }

    #endregion

    #region IsBool(是否布尔类型)

    /// <summary>
    /// 是否布尔类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsBool(MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return ((TypeInfo)member).AsType() == typeof(bool);

            case MemberTypes.Property:
                return IsBool((PropertyInfo)member);

            case MemberTypes.Field:
            {
                var fieldType = ((FieldInfo)member).FieldType;
                return fieldType == typeof(bool) || fieldType == typeof(bool?);
            }
        }
        return false;
    }

    /// <summary>
    /// 是否布尔类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsBool(PropertyInfo property)
    {
        Check.NotNull(property, nameof(property));
        return property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?);
    }

    #endregion

    #region IsEnum(是否枚举类型)

    /// <summary>
    /// 是否枚举类型
    /// </summary>
    /// <param name="member">成员</param>
    public static bool IsEnum(MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return ((TypeInfo)member).AsType().IsEnum;

            case MemberTypes.Property:
                return IsEnum((PropertyInfo)member);

            case MemberTypes.Field:
            {
                var fieldType = ((FieldInfo)member).FieldType;
                if (fieldType.GetTypeInfo().IsEnum)
                    return true;
                var underlyingType = Nullable.GetUnderlyingType(fieldType);
                return underlyingType?.GetTypeInfo().IsEnum == true;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否枚举类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsEnum(PropertyInfo property)
    {
        Check.NotNull(property, nameof(property));
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
            throw new ArgumentNullException(nameof(member));
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
                return ((TypeInfo)member).AsType() == typeof(DateTime);

            case MemberTypes.Property:
                return IsDate((PropertyInfo)member);

            case MemberTypes.Field:
            {
                var fieldType = ((FieldInfo)member).FieldType;
                return fieldType == typeof(DateTime) || fieldType == typeof(DateTime?);
            }
        }
        return false;
    }

    /// <summary>
    /// 是否日期类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsDate(PropertyInfo property)
    {
        Check.NotNull(property, nameof(property));
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
            throw new ArgumentNullException(nameof(member));
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
            {
                var type = ((TypeInfo)member).AsType();
                return type == typeof(int) || type == typeof(short) || type == typeof(long);
            }

            case MemberTypes.Property:
                return IsInt((PropertyInfo)member);

            case MemberTypes.Field:
            {
                var fieldType = ((FieldInfo)member).FieldType;
                return fieldType == typeof(int) || fieldType == typeof(int?)
                    || fieldType == typeof(short) || fieldType == typeof(short?)
                    || fieldType == typeof(long) || fieldType == typeof(long?);
            }
        }
        return false;
    }

    /// <summary>
    /// 是否整型
    /// </summary>
    /// <param name="property">成员</param>
    public static bool IsInt(PropertyInfo property)
    {
        Check.NotNull(property, nameof(property));
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
            throw new ArgumentNullException(nameof(member));
        if (IsInt(member))
            return true;
        switch (member.MemberType)
        {
            case MemberTypes.TypeInfo:
            {
                var type = ((TypeInfo)member).AsType();
                return type == typeof(double) || type == typeof(decimal) || type == typeof(float);
            }

            case MemberTypes.Property:
                return IsNumber((PropertyInfo)member);

            case MemberTypes.Field:
            {
                var fieldType = ((FieldInfo)member).FieldType;
                return fieldType == typeof(double) || fieldType == typeof(double?)
                    || fieldType == typeof(decimal) || fieldType == typeof(decimal?)
                    || fieldType == typeof(float) || fieldType == typeof(float?);
            }
        }
        return false;
    }

    /// <summary>
    /// 是否数值类型
    /// </summary>
    /// <param name="property">属性</param>
    public static bool IsNumber(PropertyInfo property)
    {
        Check.NotNull(property, nameof(property));
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
    public static bool IsCollection(Type type)
    {
        Check.NotNull(type, nameof(type));
        return type.IsArray || IsGenericCollection(type);
    }

    #endregion

    #region IsGenericCollection(是否泛型集合)

    /// <summary>
    /// 是否泛型集合
    /// </summary>
    /// <param name="type">类型</param>
    public static bool IsGenericCollection(Type type)
    {
        Check.NotNull(type, nameof(type));
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
    /// 获取公共可读实例属性列表。
    /// </summary>
    /// <param name="instance">实例</param>
    /// <remarks>静态属性、索引器和只有 setter 的属性不会进入结果。</remarks>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("对象的运行时类型由 object.GetType() 决定，裁剪时必须保留目标类型的公共属性。")]
#endif
    public static List<Item> GetPublicProperties(object instance)
    {
        Check.NotNull(instance, nameof(instance));
        var properties = TypeReflections.GetPublicReadableInstanceProperties(instance.GetType());
        return properties.Select(property => new Item(property.Name, property.GetValue(instance))).ToList();
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
        Check.NotNull(type, nameof(type));
        if (type.IsInterface)
            return type;
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.BaseType == typeof(object))
                return current;
        }
        return null;
    }

    #endregion

    #region IsDeriveClassFrom(判断当前类型是否可由指定类型派生)

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <typeparam name="TBaseType">基类型</typeparam>
    /// <param name="type">当前类型</param>
    /// <param name="canAbstract">能否是抽象类</param>
    public static bool IsDeriveClassFrom<TBaseType>(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, bool canAbstract = false) => IsDeriveClassFrom(type, typeof(TBaseType), canAbstract);

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <param name="type">当前类型</param>
    /// <param name="baseType">基类型</param>
    /// <param name="canAbstract">能否是抽象类</param>
    public static bool IsDeriveClassFrom(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, Type baseType, bool canAbstract = false)
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
    public static bool IsBaseOn<TBaseType>(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type) => IsBaseOn(type, typeof(TBaseType));

    /// <summary>
    /// 返回当前类型是否是指定基类的派生类
    /// </summary>
    /// <param name="type">类型</param>
    /// <param name="baseType">基类类型</param>
    public static bool IsBaseOn(
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type, Type baseType)
    {
        Check.NotNull(type, nameof(type));
        Check.NotNull(baseType, nameof(baseType));
        return baseType.IsGenericTypeDefinition
            ? baseType.IsGenericAssignableFrom(type)
            : baseType.IsAssignableFrom(type);
    }

    #endregion

    #region IsGenericAssignableFrom(判断当前泛型类型是否可由指定类型的实例填充)

    /// <summary>
    /// 判断当前泛型类型是否可由指定类型的实例填充
    /// </summary>
    /// <param name="genericType">泛型类型</param>
    /// <param name="type">指定类型</param>
    public static bool IsGenericAssignableFrom(Type genericType,
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        Check.NotNull(genericType, nameof(genericType));
        Check.NotNull(type, nameof(type));
        if (!genericType.IsGenericType)
            throw new ArgumentException("该功能只支持泛型类型的调用，非泛型类型可使用 IsAssignableFrom 方法。");

        // 闭合泛型应遵循 CLR 的真实赋值规则，包括泛型协变/逆变和闭合泛型基类。
        // 只有开放泛型定义需要继续按泛型定义遍历继承链。
        if (!genericType.IsGenericTypeDefinition)
            return genericType.IsAssignableFrom(type);

        var allOthers = new List<Type>() { type };
        if (genericType.IsInterface) allOthers.AddRange(type.GetInterfaces());

        foreach (var other in allOthers)
        {
            var cur = other;
            while (cur != null)
            {
                if (cur.IsGenericType && cur.GetGenericTypeDefinition() == genericType)
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
