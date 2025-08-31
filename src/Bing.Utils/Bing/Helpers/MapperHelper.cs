using Bing.Reflection;
using Bing.Text;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Bing.Helpers;

/// <summary>
/// 映射器帮助类
/// </summary>
public static class MapperHelper
{
    /// <summary>
    /// Struct 映射委托缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, object> _structMapCache = new();

    /// <summary>
    /// 将源对象映射到目标对象
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static TDestination Map<TSource, TDestination>(TSource source) where TDestination : new()
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        // 检查是否为值类型到值类型的映射，使用优化的 Struct 映射
        if (typeof(TSource).IsValueType && typeof(TDestination).IsValueType)
            return MapStructDynamic<TSource, TDestination>(source);

        // 原有的引用类型映射逻辑
        return MapReference<TSource, TDestination>(source);
    }

    /// <summary>
    /// 将源对象映射到目标对象
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesToMap">属性映射数组，可指定映射部分属性</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static TDestination MapWith<TSource, TDestination>(TSource source, params string[] propertiesToMap) where TDestination : new()
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (propertiesToMap == null || propertiesToMap.Length == 0)
            return new TDestination();

        // 检查是否为值类型到值类型的映射
        if (typeof(TSource).IsValueType && typeof(TDestination).IsValueType)
            return MapStructWithDynamic<TSource, TDestination>(source, propertiesToMap);

        // 原有的引用类型映射逻辑
        return MapReferenceWith<TSource, TDestination>(source, propertiesToMap);
    }

    /// <summary>
    /// 将源对象映射到目标对象
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesNoMap">忽略属性映射数组，忽略指定映射部分属性</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static TDestination MapWithout<TSource, TDestination>(TSource source, params string[] propertiesNoMap) where TDestination : new()
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        
        propertiesNoMap ??= [];

        // 检查是否为值类型到值类型的映射
        if (typeof(TSource).IsValueType && typeof(TDestination).IsValueType)
            return MapStructWithoutDynamic<TSource, TDestination>(source, propertiesNoMap);

        // 原有的引用类型映射逻辑
        return MapReferenceWithout<TSource, TDestination>(source, propertiesNoMap);
    }

    #region Struct 映射实现

    /// <summary>
    /// 动态调用结构体映射（解决泛型约束问题）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStructDynamic<TSource, TDestination>(TSource source)
    {
        var key = $"Map_{typeof(TSource).FullName}_{typeof(TDestination).FullName}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapFuncDynamic<TSource, TDestination>();
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 动态调用结构体映射（指定属性）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesToMap">要映射的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStructWithDynamic<TSource, TDestination>(TSource source, string[] propertiesToMap)
    {
        var key = $"MapWith_{typeof(TSource).FullName}_{typeof(TDestination).FullName}_{string.Join(",", propertiesToMap.OrderBy(p => p))}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapWithFuncDynamic<TSource, TDestination>(propertiesToMap);
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 动态调用结构体映射（排除指定属性）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesNoMap">要排除的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStructWithoutDynamic<TSource, TDestination>(TSource source, string[] propertiesNoMap)
    {
        var key = $"MapWithout_{typeof(TSource).FullName}_{typeof(TDestination).FullName}_{string.Join(",", propertiesNoMap.OrderBy(p => p))}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapWithoutFuncDynamic<TSource, TDestination>(propertiesNoMap);
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 创建结构体映射委托（动态版本）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapFuncDynamic<TSource, TDestination>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的所有可写属性
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !IsIndexerProperty(p) && !IsStatic(p))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            // 查找源类型中同名的可读属性
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            // 检查属性类型是否兼容
            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 创建结构体映射委托（指定属性，动态版本）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="propertiesToMap">要映射的属性名称</param>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapWithFuncDynamic<TSource, TDestination>(string[] propertiesToMap)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的指定属性
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                       !IsIndexerProperty(p) &&
                       !IsStatic(p) &&
                       propertiesToMap.Any(name => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 创建结构体映射委托（排除指定属性，动态版本）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="propertiesNoMap">要排除的属性名称</param>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapWithoutFuncDynamic<TSource, TDestination>(string[] propertiesNoMap)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的属性（排除指定属性）
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                       !IsIndexerProperty(p) &&
                       !IsStatic(p) &&
                       !propertiesNoMap.Any(name => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 结构体映射（高性能版本）
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStruct<TSource, TDestination>(TSource source)
        where TSource : struct
        where TDestination : struct
    {
        var key = $"Map_{typeof(TSource).FullName}_{typeof(TDestination).FullName}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapFunc<TSource, TDestination>();
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 结构体映射（指定属性）
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesToMap">要映射的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStructWith<TSource, TDestination>(TSource source, string[] propertiesToMap)
        where TSource : struct
        where TDestination : struct
    {
        var key = $"MapWith_{typeof(TSource).FullName}_{typeof(TDestination).FullName}_{string.Join(",", propertiesToMap.OrderBy(p => p))}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapWithFunc<TSource, TDestination>(propertiesToMap);
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 结构体映射（排除指定属性）
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesNoMap">要排除的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapStructWithout<TSource, TDestination>(TSource source, string[] propertiesNoMap)
        where TSource : struct
        where TDestination : struct
    {
        var key = $"MapWithout_{typeof(TSource).FullName}_{typeof(TDestination).FullName}_{string.Join(",", propertiesNoMap.OrderBy(p => p))}";
        if (!_structMapCache.TryGetValue(key, out var mapFuncObj))
        {
            var mapFunc = CreateStructMapWithoutFunc<TSource, TDestination>(propertiesNoMap);
            _structMapCache[key] = mapFunc;
            mapFuncObj = mapFunc;
        }

        var func = (Func<TSource, TDestination>)mapFuncObj;
        return func(source);
    }

    /// <summary>
    /// 创建结构体映射委托
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapFunc<TSource, TDestination>()
        where TSource : struct
        where TDestination : struct
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的所有可写属性
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !IsIndexerProperty(p) && !IsStatic(p))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            // 查找源类型中同名的可读属性
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            // 检查属性类型是否兼容
            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 创建结构体映射委托（指定属性）
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <param name="propertiesToMap">要映射的属性名称</param>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapWithFunc<TSource, TDestination>(string[] propertiesToMap)
        where TSource : struct
        where TDestination : struct
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的指定属性
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                       !IsIndexerProperty(p) &&
                       !IsStatic(p) &&
                       propertiesToMap.Any(name => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 创建结构体映射委托（排除指定属性）
    /// </summary>
    /// <typeparam name="TSource">源结构体类型</typeparam>
    /// <typeparam name="TDestination">目标结构体类型</typeparam>
    /// <param name="propertiesNoMap">要排除的属性名称</param>
    /// <returns>映射委托</returns>
    private static Func<TSource, TDestination> CreateStructMapWithoutFunc<TSource, TDestination>(string[] propertiesNoMap)
        where TSource : struct
        where TDestination : struct
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TDestination);

        var sourceParam = Expression.Parameter(sourceType, "source");
        var bindings = new List<MemberBinding>();

        // 获取目标类型的属性（排除指定属性）
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                       !IsIndexerProperty(p) &&
                       !IsStatic(p) &&
                       !propertiesNoMap.Any(name => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var targetProp in targetProperties)
        {
            var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                continue;

            if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
            {
                var sourcePropertyAccess = Expression.Property(sourceParam, sourceProp);
                var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                bindings.Add(Expression.Bind(targetProp, convertedValue));
            }
        }

        var body = Expression.MemberInit(Expression.New(targetType), bindings);
        return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParam).Compile();
    }

    /// <summary>
    /// 检查属性类型是否兼容
    /// </summary>
    /// <param name="sourceType">源属性类型</param>
    /// <param name="targetType">目标属性类型</param>
    /// <returns>是否兼容</returns>
    private static bool IsPropertyTypeCompatible(Type sourceType, Type targetType)
    {
        // 相同类型
        if (sourceType == targetType)
            return true;

        // 可空类型兼容性
        var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
        var targetUnderlyingType = Nullable.GetUnderlyingType(targetType);

        if (sourceUnderlyingType != null && targetUnderlyingType != null)
            return IsPropertyTypeCompatible(sourceUnderlyingType, targetUnderlyingType);

        if (sourceUnderlyingType != null)
            return IsPropertyTypeCompatible(sourceUnderlyingType, targetType);

        if (targetUnderlyingType != null)
            return IsPropertyTypeCompatible(sourceType, targetUnderlyingType);

        // 结构体到结构体的兼容性检查
        if (sourceType.IsValueType && targetType.IsValueType)
            return CanMapStruct(sourceType, targetType);

        // 可赋值性检查
        return targetType.IsAssignableFrom(sourceType);
    }

    /// <summary>
    /// 检查是否可以进行结构体映射
    /// </summary>
    /// <param name="sourceType">源结构体类型</param>
    /// <param name="targetType">目标结构体类型</param>
    /// <returns>是否可以映射</returns>
    private static bool CanMapStruct(Type sourceType, Type targetType)
    {
        // 获取两个结构体的可映射属性
        var sourceProps = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsIndexerProperty(p) && !IsStatic(p))
            .ToArray();

        var targetProps = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !IsIndexerProperty(p) && !IsStatic(p))
            .ToArray();

        // 检查是否有任何匹配的属性
        return targetProps.Any(targetProp =>
            sourceProps.Any(sourceProp =>
                string.Equals(sourceProp.Name, targetProp.Name, StringComparison.OrdinalIgnoreCase) &&
                IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType)));
    }

    /// <summary>
    /// 转换属性表达式
    /// </summary>
    /// <param name="sourceExpression">源属性表达式</param>
    /// <param name="sourceType">源属性类型</param>
    /// <param name="targetType">目标属性类型</param>
    /// <returns>转换后的表达式</returns>
    private static Expression ConvertProperty(Expression sourceExpression, Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return sourceExpression;

        // 处理可空类型转换
        var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
        var targetUnderlyingType = Nullable.GetUnderlyingType(targetType);

        if (sourceUnderlyingType != null && targetUnderlyingType != null)
        {
            // 可空类型到可空类型
            if (sourceUnderlyingType == targetUnderlyingType)
                return sourceExpression;
            // 嵌套结构体可空类型转换
            if (sourceUnderlyingType.IsValueType && targetUnderlyingType.IsValueType)
                return CreateNullableToNullableStructConversion(sourceExpression, sourceUnderlyingType, targetUnderlyingType);
        }
        else if (sourceUnderlyingType != null && targetUnderlyingType == null)
        {
            // 可空类型到非可空类型
            if (sourceUnderlyingType == targetType)
            {
                return Expression.Condition(
                    Expression.Property(sourceExpression, "HasValue"),
                    Expression.Property(sourceExpression, "Value"),
                    Expression.Default(targetType));
            }

            // 可空结构体到非可空结构体
            if (sourceUnderlyingType.IsValueType && targetType.IsValueType)
                return CreateNullableToStructConversion(sourceExpression, sourceUnderlyingType, targetType);
        }
        else if (sourceUnderlyingType == null && targetUnderlyingType != null)
        {
            // 非可空类型到可空类型
            if (sourceType == targetUnderlyingType)
                return Expression.Convert(sourceExpression, targetType);

            // 非可空结构体到可空结构体
            if (sourceType.IsValueType && targetUnderlyingType.IsValueType)
                return CreateStructToNullableConversion(sourceExpression, sourceType, targetUnderlyingType);
        }

        // 结构体到结构体的直接转换
        if (sourceType.IsValueType && targetType.IsValueType && sourceType != targetType)
            return CreateStructToStructConversion(sourceExpression, sourceType, targetType);

        // 尝试直接转换
        try
        {
            return Expression.Convert(sourceExpression, targetType);
        }
        catch
        {
            // 转换失败，返回默认值
            return Expression.Default(targetType);
        }
    }

    /// <summary>
    /// 创建结构体到结构体的转换表达式
    /// </summary>
    /// <param name="sourceExpression">源表达式</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换表达式</returns>
    private static Expression CreateStructToStructConversion(Expression sourceExpression, Type sourceType, Type targetType)
    {
        try
        {
            // 创建一个临时变量存储源值
            var sourceVar = Expression.Variable(sourceType, "sourceStruct");
            var targetVar = Expression.Variable(targetType, "targetStruct");

            var bindings = new List<MemberBinding>();

            // 获取目标类型的可写属性
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && !IsIndexerProperty(p) && !IsStatic(p))
                .ToArray();

            foreach (var targetProp in targetProperties)
            {
                // 查找源类型中同名的可读属性
                var sourceProp = sourceType.GetProperty(targetProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceProp == null || !sourceProp.CanRead || IsIndexerProperty(sourceProp) || IsStatic(sourceProp))
                    continue;

                // 检查属性类型是否兼容
                if (IsPropertyTypeCompatible(sourceProp.PropertyType, targetProp.PropertyType))
                {
                    var sourcePropertyAccess = Expression.Property(sourceVar, sourceProp);
                    var convertedValue = ConvertProperty(sourcePropertyAccess, sourceProp.PropertyType, targetProp.PropertyType);
                    bindings.Add(Expression.Bind(targetProp, convertedValue));
                }
            }

            if (bindings.Count == 0)
            {
                return Expression.Default(targetType);
            }

            // 创建表达式块
            var block = Expression.Block(
                new[] { sourceVar, targetVar },
                Expression.Assign(sourceVar, sourceExpression),
                Expression.Assign(targetVar, Expression.MemberInit(Expression.New(targetType), bindings)),
                targetVar
            );

            return block;
        }
        catch
        {
            return Expression.Default(targetType);
        }
    }

    /// <summary>
    /// 创建可空结构体到可空结构体的转换表达式
    /// </summary>
    /// <param name="sourceExpression">源表达式</param>
    /// <param name="sourceUnderlyingType">源底层类型</param>
    /// <param name="targetUnderlyingType">目标底层类型</param>
    /// <returns>转换表达式</returns>
    private static Expression CreateNullableToNullableStructConversion(Expression sourceExpression, Type sourceUnderlyingType, Type targetUnderlyingType)
    {
        var targetNullableType = typeof(Nullable<>).MakeGenericType(targetUnderlyingType);

        return Expression.Condition(
            Expression.Property(sourceExpression, "HasValue"),
            Expression.Convert(
                CreateStructToStructConversion(
                    Expression.Property(sourceExpression, "Value"),
                    sourceUnderlyingType,
                    targetUnderlyingType
                ),
                targetNullableType
            ),
            Expression.Default(targetNullableType)
        );
    }

    /// <summary>
    /// 创建可空结构体到非可空结构体的转换表达式
    /// </summary>
    /// <param name="sourceExpression">源表达式</param>
    /// <param name="sourceUnderlyingType">源底层类型</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换表达式</returns>
    private static Expression CreateNullableToStructConversion(Expression sourceExpression, Type sourceUnderlyingType, Type targetType)
    {
        return Expression.Condition(
            Expression.Property(sourceExpression, "HasValue"),
            CreateStructToStructConversion(
                Expression.Property(sourceExpression, "Value"),
                sourceUnderlyingType,
                targetType
            ),
            Expression.Default(targetType)
        );
    }

    /// <summary>
    /// 创建非可空结构体到可空结构体的转换表达式
    /// </summary>
    /// <param name="sourceExpression">源表达式</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetUnderlyingType">目标底层类型</param>
    /// <returns>转换表达式</returns>
    private static Expression CreateStructToNullableConversion(Expression sourceExpression, Type sourceType, Type targetUnderlyingType)
    {
        var targetNullableType = typeof(Nullable<>).MakeGenericType(targetUnderlyingType);

        return Expression.Convert(
            CreateStructToStructConversion(sourceExpression, sourceType, targetUnderlyingType),
            targetNullableType
        );
    }

    /// <summary>
    /// 判断属性是否为索引器属性
    /// </summary>
    /// <param name="propertyInfo">属性信息</param>
    /// <returns>是否为索引器属性</returns>
    private static bool IsIndexerProperty(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name == "Item" && propertyInfo.GetIndexParameters().Length > 0;
    }

    /// <summary>
    /// 判断属性是否为静态属性
    /// </summary>
    /// <param name="propertyInfo">属性信息</param>
    /// <returns>是否为静态属性</returns>
    private static bool IsStatic(PropertyInfo propertyInfo)
    {
        return (propertyInfo.GetMethod ?? propertyInfo.SetMethod)?.IsStatic == true;
    }

    #endregion

    #region 引用类型映射实现

    /// <summary>
    /// 引用类型映射
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapReference<TSource, TDestination>(TSource source) where TDestination : new()
    {
        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TDestination))
            .Where(p => !p.IsStatic())
            .ToArray();
        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TSource))
            .Where(x => !x.IsStatic() && destinationProperties.Any(_ => _.Name.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite)
                    continue;

                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null || !sourceProperty.CanRead)
                    continue;

                try
                {
                    var propGetter = sourceProperty.GetValueGetter<TSource>();
                    var propSetter = destinationProperty.GetValueSetter<TDestination>();

                    if (propGetter != null && propSetter != null)
                    {
                        var value = propGetter.Invoke(source);
                        propSetter.Invoke(result, value);
                    }
                }
                catch (Exception)
                {
                    // 忽略属性映射异常，继续处理其他属性
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 引用类型映射（指定属性）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesToMap">要映射的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapReferenceWith<TSource, TDestination>(TSource source, string[] propertiesToMap) where TDestination : new()
    {
        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TDestination))
            .Where(x => !x.IsStatic() && propertiesToMap.Any(_ => string.Equals(_, x.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TSource))
            .Where(x => !x.IsStatic() && propertiesToMap.Any(_ => _.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite)
                    continue;

                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null || !sourceProperty.CanRead)
                    continue;

                try
                {
                    var propGetter = sourceProperty.GetValueGetter<TSource>();
                    var propSetter = destinationProperty.GetValueSetter<TDestination>();

                    if (propGetter != null && propSetter != null)
                    {
                        var value = propGetter.Invoke(source);
                        propSetter.Invoke(result, value);
                    }
                }
                catch (Exception)
                {
                    // 忽略属性映射异常，继续处理其他属性
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 引用类型映射（排除指定属性）
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="propertiesNoMap">要排除的属性名称</param>
    /// <returns>映射后的目标对象</returns>
    private static TDestination MapReferenceWithout<TSource, TDestination>(TSource source, string[] propertiesNoMap) where TDestination : new()
    {
        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TDestination))
            .Where(x => !x.IsStatic() && !propertiesNoMap.Any(_ => string.Equals(_, x.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(typeof(TSource))
            .Where(x => !x.IsStatic() && destinationProperties.Any(_ => _.Name.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite)
                    continue;

                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null || !sourceProperty.CanRead)
                    continue;

                try
                {
                    var propGetter = sourceProperty.GetValueGetter<TSource>();
                    var propSetter = destinationProperty.GetValueSetter<TDestination>();

                    if (propGetter != null && propSetter != null)
                    {
                        var value = propGetter.Invoke(source);
                        propSetter.Invoke(result, value);
                    }
                }
                catch (Exception)
                {
                    // 忽略属性映射异常，继续处理其他属性
                }
            }
        }

        return result;
    }

    #endregion
}