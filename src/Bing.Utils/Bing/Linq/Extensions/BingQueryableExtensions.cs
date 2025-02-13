using System.Linq.Expressions;
using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Linq;

/// <summary>
/// 查询(<see cref="IQueryable{T}"/>) 扩展
/// </summary>
public static class BingQueryableExtensions
{
    #region PageBy(分页)

    /// <summary>
    /// 分页。<code>Skip(...).Take(...)</code>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="skipCount">跳过的行数</param>
    /// <param name="maxResultCount">每页显示行数</param>
    public static IQueryable<T> PageBy<T>(this IQueryable<T> query, int skipCount, int maxResultCount)
    {
        Check.NotNull(query, nameof(query));
        return query.Skip(skipCount).Take(maxResultCount);
    }

    /// <summary>
    /// 分页。<code>Skip(...).Take(...)</code>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TQueryable">查询源类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="skipCount">跳过的行数</param>
    /// <param name="maxResultCount">每页显示行数</param>
    public static TQueryable PageBy<T, TQueryable>(this TQueryable query, int skipCount, int maxResultCount)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));
        return (TQueryable)query.Skip(skipCount).Take(maxResultCount);
    }

    #endregion

    #region WhereIf(添加查询条件)

    /// <summary>
    /// 添加过滤条件。如果给定条件为真，则按照给定的条件进行过滤
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="condition">给定条件</param>
    /// <param name="predicate">过滤条件</param>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        Check.NotNull(query, nameof(query));
        return condition ? query.Where(predicate) : query;
    }

    /// <summary>
    /// 添加过滤条件。如果给定条件为真，则按照给定的条件进行过滤
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TQueryable">查询源类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="condition">给定条件</param>
    /// <param name="predicate">过滤条件</param>
    public static TQueryable WhereIf<T, TQueryable>(this TQueryable query, bool condition, Expression<Func<T, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));
        return condition ? (TQueryable)query.Where(predicate) : query;
    }

    /// <summary>
    /// 添加过滤条件。如果给定条件为真，则按照给定的条件进行过滤
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="condition">给定条件</param>
    /// <param name="predicate">过滤条件</param>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, int, bool>> predicate)
    {
        Check.NotNull(query, nameof(query));
        return condition ? query.Where(predicate) : query;
    }

    /// <summary>
    /// 添加过滤条件。如果给定条件为真，则按照给定的条件进行过滤
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <typeparam name="TQueryable">查询源类型</typeparam>
    /// <param name="query">数据源</param>
    /// <param name="condition">给定条件</param>
    /// <param name="predicate">过滤条件</param>
    public static TQueryable WhereIf<T, TQueryable>(this TQueryable query, bool condition, Expression<Func<T, int, bool>> predicate)
        where TQueryable : IQueryable<T>
    {
        Check.NotNull(query, nameof(query));
        return condition ? (TQueryable)query.Where(predicate) : query;
    }

    #endregion
}