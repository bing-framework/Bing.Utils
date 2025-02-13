using Bing.Collections;

namespace Bing.Text;

/// <summary>
/// 额外的字符串扩展
/// </summary>
public static partial class StringProwessExtensions
{
    #region Join

    /// <summary>
    /// 将给定的集合以特定的字符串为分隔符进行合并。
    /// </summary>
    /// <typeparam name="T">集合元素的类型</typeparam>
    /// <param name="separator">分隔符</param>
    /// <param name="list">要合并的集合</param>
    /// <returns>合并后的字符串</returns>
    public static string JoinStringFor<T>(this string separator, IEnumerable<T> list) => list.JoinToString(separator);

    #endregion

    #region Split

    /// <summary>
    /// 将指定文本根据给定的索引位置进行分割，并将分割所得的两部分字符以元组形式返回。
    /// </summary>
    /// <param name="that">要分割的字符串</param>
    /// <param name="index">分割位置的索引</param>
    /// <returns>包含分割后两部分字符的元组</returns>
    public static Tuple<string, string> SplitByIndex(this string that, int index)
    {
        if (that.IsNullOrEmpty())
            return Tuple.Create("", "");
        if (index >= that.Length)
            return Tuple.Create(that, "");
        if (index <= 0)
            return Tuple.Create("", that);
        return Tuple.Create(that.Substring(0, index - 1), that.Substring(index - 1));
    }

    /// <summary>
    /// 将指定文本根据给定的分隔符进行分割，并将分割所得的字符数组转换为指定类型的实例数据。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="that">要分割的字符串</param>
    /// <param name="delimiter">分隔符</param>
    /// <returns>转换后的指定类型的实例数组</returns>
    public static T[] SplitTyped<T>(this string that, char delimiter) where T : IComparable
    {
        if (that.IsNullOrWhiteSpace())
            return Array.Empty<T>();
        return that
            .Trim()
            .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => (T)Convert.ChangeType(s, typeof(T)))
            .ToArray();
    }

    /// <summary>
    /// 将指定文本根据给定的分隔符进行分割，并将分割所得的字符数组转换为指定类型的实例数据。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="that">要分割的字符串</param>
    /// <param name="delimiter">分隔符</param>
    /// <returns>转换后的指定类型的实例数组</returns>
    public static T[] SplitTyped<T>(this string that, string delimiter) where T : IComparable
    {
        if (that.IsNullOrWhiteSpace())
            return Array.Empty<T>();
        return that
            .Trim()
            .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => (T)Convert.ChangeType(s, typeof(T)))
            .ToArray();
    }

    #endregion
}