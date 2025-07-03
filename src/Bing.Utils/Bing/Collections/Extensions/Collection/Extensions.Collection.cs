using System.Collections.ObjectModel;
using Bing.Extensions;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 集合(<see cref="ICollection{T}"/>) 扩展
/// </summary>
public static partial class BingCollectionExtensions
{
    #region Sort(排序)

    /// <summary>
    /// 排序
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="collection">集合</param>
    /// <param name="comparer">比较器</param>
    public static void Sort<T>(this ICollection<T> collection, IComparer<T> comparer = null)
    {
        comparer = comparer ?? Comparer<T>.Default;
        var list = new List<T>(collection);
        list.Sort(comparer);
        collection.ReplaceItems(list);
    }

    #endregion

    #region ReplaceItems(替换项)

    /// <summary>
    /// 替换项
    /// </summary>
    /// <typeparam name="TItem">项类型</typeparam>
    /// <typeparam name="TNewItem">新项类型</typeparam>
    /// <param name="collection">集合</param>
    /// <param name="newItems">新项集合</param>
    /// <param name="createItemAction">创建项操作</param>
    public static void ReplaceItems<TItem, TNewItem>(this ICollection<TItem> collection,
        IEnumerable<TNewItem> newItems, Func<TNewItem, TItem> createItemAction)
    {
        collection.CheckNotNull(nameof(collection));
        newItems.CheckNotNull(nameof(newItems));
        createItemAction.CheckNotNull(nameof(createItemAction));

        collection.Clear();
        var convertedNewItems = newItems.Select(createItemAction);
        collection.AddRange(convertedNewItems);
    }

    /// <summary>
    /// 替换项
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="collection">集合</param>
    /// <param name="newItems">新项集合</param>
    public static void ReplaceItems<T>(this ICollection<T> collection, IEnumerable<T> newItems)
    {
        collection.CheckNotNull(nameof(collection));
        newItems.CheckNotNull(nameof(newItems));

        collection.ReplaceItems(newItems, x => x);
    }

    #endregion

    /// <summary>
    /// 将集合转换为指定的可观察集合
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="collection">要转换的源集合</param>
    /// <param name="observableCollection">目标可观察集合</param>
    /// <returns>填充了源集合元素的可观察集合</returns>
    /// <remarks>
    /// 此方法会清空目标可观察集合，然后将源集合中的所有元素添加到目标集合中。
    /// 适用于需要复用现有可观察集合实例的场景。
    /// </remarks>
    /// <exception cref="ArgumentNullException">源集合或目标可观察集合为 null</exception>
    public static ObservableCollection<T> ToObservableCollection<T>(this ICollection<T> collection, ObservableCollection<T> observableCollection)
    {
        collection.CheckNotNull(nameof(collection));
        observableCollection.CheckNotNull(nameof(observableCollection));

        observableCollection.Clear();
        foreach (var item in collection) 
            observableCollection.Add(item);
        return observableCollection;
    }

    /// <summary>
    /// 将集合转换为新的可观察集合
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="collection">要转换的源集合</param>
    /// <returns>包含源集合所有元素的新可观察集合</returns>
    /// <remarks>
    /// 此方法创建一个新的可观察集合实例，并将源集合中的所有元素添加到新集合中。
    /// </remarks>
    /// <exception cref="ArgumentNullException">源集合为 null</exception>
    public static ObservableCollection<T> ToObservableCollection<T>(this ICollection<T> collection)
    {
        collection.CheckNotNull(nameof(collection));

        var observableCollection = new ObservableCollection<T>();
        foreach (var item in collection)
            observableCollection.Add(item);
        return observableCollection;
    }
}