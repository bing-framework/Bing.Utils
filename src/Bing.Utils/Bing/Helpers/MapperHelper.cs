using Bing.Reflection;
using Bing.Text;

namespace Bing.Helpers;

/// <summary>
/// 映射器帮助类
/// </summary>
public static class MapperHelper
{
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
        var sourceType = typeof(TSource);
        var destinationType = typeof(TDestination);

        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(destinationType);
        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(sourceType)
            .Where(x => destinationProperties.Any(_ => _.Name.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null)
                    continue;

                var propGetter = sourceProperty.GetValueGetter();
                if (propGetter != null)
                    destinationProperty.GetValueSetter()?.Invoke(result, propGetter.Invoke(source));
            }
        }

        return result;
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
        var sourceType = typeof(TSource);
        var destinationType = typeof(TDestination);

        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(destinationType)
            .Where(x => propertiesToMap.Any(_ => string.Equals(_, x.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(sourceType)
            .Where(x => propertiesToMap.Any(_ => _.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null || !sourceProperty.CanRead || !destinationProperty.CanWrite)
                    continue;

                var propGetter = sourceProperty.GetValueGetter();
                if (propGetter != null)
                    destinationProperty.GetValueSetter()?.Invoke(result, propGetter.Invoke(source));
            }
        }

        return result;
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
        var sourceType = typeof(TSource);
        var destinationType = typeof(TDestination);

        var destinationProperties = TypeReflections.TypeCacheManager.GetTypeProperties(destinationType)
            .Where(x => !propertiesNoMap.Any(_ => string.Equals(_, x.Name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var sourceProperties = TypeReflections.TypeCacheManager.GetTypeProperties(sourceType)
            .Where(x => !destinationProperties.Any(_ => _.Name.EqualsIgnoreCase(x.Name)))
            .ToArray();

        var result = new TDestination();

        if (destinationProperties.Length > 0)
        {
            foreach (var destinationProperty in destinationProperties)
            {
                var sourceProperty = sourceProperties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(destinationProperty.Name));
                if (sourceProperty == null || !sourceProperty.CanRead || !destinationProperty.CanWrite)
                    continue;

                var propGetter = sourceProperty.GetValueGetter();
                if (propGetter != null)
                    destinationProperty.GetValueSetter()?.Invoke(result, propGetter.Invoke(source));
            }
        }

        return result;
    }
}