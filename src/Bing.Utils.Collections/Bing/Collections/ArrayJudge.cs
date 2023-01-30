namespace Bing.Collections;

/// <summary>
/// 数据检查器
/// </summary>
public static class ArrayJudge
{
    /// <summary>
    /// 在一维数组中，判断给定的索引值是否会超出数组上下边界。如果超出则返回 false。
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="index">索引值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIndexInRange(Array array, int index)
    {
        return index >= 0 && index < array.Length;
    }

    /// <summary>
    /// 在多维数组中，判断给定的索引值是否会超出数组指定维度上的上下边界。如果超出则返回 false。<br />
    /// 如果维度值超界，则抛出异常。
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="index">索引值</param>
    /// <param name="dimension">数组维度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIndexInRange(Array array, int index, int dimension)
    {
        if (dimension <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimension));
        return index >= array.GetLowerBound(dimension) && index <= array.GetUpperBound(dimension);
    }
}