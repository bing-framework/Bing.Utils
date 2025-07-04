// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 数组(<see cref="Array"/>) 扩展
/// </summary>
public static partial class ArrayExtensions
{
    #region WithInIndex(判断索引是否在数组范围内)

    /// <summary>
    /// 判断索引是否在一维数组范围内
    /// </summary>
    /// <param name="array">待检查的数组</param>
    /// <param name="index">索引值</param>
    /// <returns>
    /// 如果索引在数组有效范围内，则返回 true；
    /// 如果数组为空或索引超出范围，则返回 false
    /// </returns>
    public static bool WithInIndex(this Array array, int index)
    {
        if (array == null)
            return false;
        return index >= 0 && index < array.Length;
    }

    /// <summary>
    /// 判断索引是否在多维数组指定维度的范围内
    /// </summary>
    /// <param name="array">待检查的数组</param>
    /// <param name="index">索引值</param>
    /// <param name="dimension">数组维度，从0开始计数</param>
    /// <returns>
    /// 如果索引在指定维度的有效范围内，则返回 true；
    /// 如果数组为空或索引超出范围，则返回 false
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">当维度小于0或大于等于数组的维度数时抛出</exception>
    public static bool WithInIndex(this Array array, int index, int dimension)
    {
        if (array == null)
            return false;
        if (dimension < 0)
            throw new ArgumentOutOfRangeException(nameof(dimension), "维度不能为负数");
        if (dimension >= array.Rank)
            throw new ArgumentOutOfRangeException(nameof(dimension), $"维度超出范围，数组维度数为 {array.Rank}");
        return index >= array.GetLowerBound(dimension) && index <= array.GetUpperBound(dimension);
    }

    #endregion
}