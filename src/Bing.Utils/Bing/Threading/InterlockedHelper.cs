namespace Bing.Threading;

/// <summary>
/// 提供线程安全的读取操作的帮助类。
/// </summary>
public static class InterlockedHelper
{
    /// <summary>
    /// 线程安全地读取整数值。
    /// </summary>
    /// <param name="location">要读取的整数值</param>
    /// <returns>读取的整数值</returns>
    public static int Read(ref int location) => Interlocked.CompareExchange(ref location, default, default);

    /// <summary>
    /// 线程安全地读取浮点数值。
    /// </summary>
    /// <param name="location">要读取的浮点数值</param>
    /// <returns>读取的浮点数值</returns>
    public static float Read(ref float location) => Interlocked.CompareExchange(ref location, default, default);

    /// <summary>
    /// 线程安全地读取双精度浮点数值。
    /// </summary>
    /// <param name="location">要读取的双精度浮点数值</param>
    /// <returns>读取的双精度浮点数值</returns>
    public static double Read(ref double location) => Interlocked.CompareExchange(ref location, default, default);

    /// <summary>
    /// 线程安全地读取引用类型的值。
    /// </summary>
    /// <typeparam name="T">引用类型</typeparam>
    /// <param name="location">要读取的引用类型的值</param>
    /// <returns>读取的引用类型的值</returns>
    public static T Read<T>(ref T location) where T : class => Interlocked.CompareExchange(ref location, default, default);
}