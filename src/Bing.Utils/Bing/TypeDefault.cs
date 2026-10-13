namespace Bing;

/// <summary>
/// 类型默认值
/// </summary>
public static class TypeDefault
{
    /// <summary>
    /// 获取指定类型默认值
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    public static TValue Of<TValue>() => default;

    /// <summary>
    /// 获取int
    /// </summary>
    public static int Int => default;

    /// <summary>
    /// 获取long
    /// </summary>
    public static long Long => default;

    /// <summary>
    /// 获取float
    /// </summary>
    public static float Float => default;

    /// <summary>
    /// 获取double
    /// </summary>
    public static double Double => default;

    /// <summary>
    /// 获取decimal
    /// </summary>
    public static decimal Decimal => default;

    /// <summary>
    /// 获取char
    /// </summary>
    public static char Char => default;

    /// <summary>
    /// 获取string
    /// </summary>
    public static string String => default;

    /// <summary>
    /// 获取empty
    /// </summary>
    public static string StringEmpty => string.Empty;

    /// <summary>
    /// 获取DateTime
    /// </summary>
    public static DateTime DateTime => default;

    /// <summary>
    /// 获取TimeSpan
    /// </summary>
    public static TimeSpan TimeSpan => default;
}
