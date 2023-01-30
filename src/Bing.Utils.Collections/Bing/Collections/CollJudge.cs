using System.Collections;

namespace Bing.Collections;

/// <summary>
/// 集合判断器
/// </summary>
public static class CollJudge
{
    /// <summary>
    /// 判断 <see cref="IEnumerable"/> 是否为 null
    /// </summary>
    /// <param name="enumerable">集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull(IEnumerable enumerable) => enumerable is null;

    /// <summary>
    /// 判断 <see cref="IEnumerable"/> 是否为空、null
    /// </summary>
    /// <param name="enumerable">集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(IEnumerable enumerable)
    {
        return enumerable is null || !enumerable.Cast<object>().Any();
    }

    /// <summary>
    /// 判断 <see cref="IEnumerable{T}"/> 是否为空、null
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>(IEnumerable<T> enumerable) => enumerable is null || !enumerable.Any();

    /// <summary>
    /// 检查两个集合是否拥有相等数量的成员
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="that">集合</param>
    public static bool IsSameCount<T>(ICollection<T> source, ICollection<T> that)
    {
        if (source is null && that is null)
            return true;
        if (source is null || that is null)
            return false;
        return source.Count.Equals(that.Count);
    }

    /// <summary>
    /// 检查两个集合是否拥有相等数量的成员
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="that">集合</param>
    public static bool IsSameCount<T>(IQueryable<T> source, IQueryable<T> that)
    {
        if (source is null && that is null)
            return true;
        if (source is null || that is null)
            return false;
        return source.Count().Equals(that.Count());
    }

    /// <summary>
    /// 判断 一个集合 是否至少包含指定数量的元素
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="collection">集合</param>
    /// <param name="count">数量</param>
    public static bool ContainsAtLeast<T>(ICollection<T> collection, int count) => collection?.Count >= count;

    /// <summary>
    /// 判断 一个可查询集合 是否至少包含指定数量的元素
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="queryable">可查询集合</param>
    /// <param name="count">数量</param>
    public static bool ContainsAtLeast<T>(IQueryable<T> queryable, int count)
    {
        if (queryable is null)
            return false;
        return (from t in queryable.Take(count) select t).Count() >= count;
    }

    /// <summary>
    /// 判断 两个可查询集合 是否包含相同数量的元素
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="queryable">可查询集合</param>
    /// <param name="targetQueryable">可查询集合</param>
    public static bool ContainsEqualCount<T>(IQueryable<T> queryable, IQueryable<T> targetQueryable)
    {
        if (queryable is null && targetQueryable is null)
            return true;
        if (queryable is null || targetQueryable is null)
            return false;
        return queryable.Count().Equals(targetQueryable.Count());
    }
}

/// <summary>
/// 集合判断器扩展
/// </summary>
public static class CollJudgeExtensions
{
    /// <summary>
    /// 检查两个集合是否拥有相等数量的成员
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="that">集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameCount<T>(this ICollection<T> source, ICollection<T> that)
    {
        return CollJudge.IsSameCount(source, that);
    }

    /// <summary>
    /// 检查两个集合是否拥有相等数量的成员
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">集合</param>
    /// <param name="that">集合</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameCount<T>(this IQueryable<T> source, IQueryable<T> that)
    {
        return CollJudge.IsSameCount(source, that);
    }
}