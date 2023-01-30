

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 数组(<see cref="Array"/>) 扩展
/// </summary>
public static partial class ArrayExtensions
{
    /// <summary>
    /// 是否在数组索引范围内
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="index">索引</param>
    public static bool WithInIndex(this Array array, int index) => array != null && index >= 0 && index < array.Length;

    /// <summary>
    /// 是否在数组索引范围内
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="index">索引</param>
    /// <param name="dimension">数组维度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool WithInIndex(this Array array, int index, int dimension)
    {
        if (dimension <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimension));
        return array != null && index >= array.GetLowerBound(dimension) && index <= array.GetUpperBound(dimension);
    }
}