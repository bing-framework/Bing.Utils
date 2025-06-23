// ReSharper disable once CheckNamespace
namespace Bing.Threading;

/// <summary>
/// 委托(<see cref="Delegate"/>) 扩展
/// </summary>
public static class DelegateExtensions
{
    /// <summary>
    /// 安全调用委托
    /// </summary>
    /// <param name="del">委托</param>
    /// <param name="parameters">参数</param>
    /// <remarks>
    /// 如果委托为 null，则不执行任何操作
    /// </remarks>
    public static void InvokeSafe(this Delegate del, params object[] parameters)
    {
        del?.DynamicInvoke(parameters);
    }

    /// <summary>
    /// 尝试执行委托
    /// </summary>
    /// <param name="del">委托</param>
    /// <param name="parameters">参数</param>
    /// <returns>执行成功返回 true，失败返回 false</returns>
    /// <remarks>
    /// 此方法会捕获执行过程中的异常，并尝试执行一次
    /// </remarks>
    public static bool Try(this Delegate del, params object[] parameters) => Try(del, 1, TimeSpan.Zero, parameters);

    /// <summary>
    /// 尝试执行委托
    /// </summary>
    /// <param name="del">委托</param>
    /// <param name="maxTries">最大尝试次数，如果设为0或负值则表示无限尝试</param>
    /// <param name="interval">尝试间隔时间</param>
    /// <param name="parameters">参数</param>
    /// <returns>执行成功返回 true，失败返回 false</returns>
    /// <remarks>
    /// 此方法会捕获执行过程中的异常，并根据指定的次数和间隔重试
    /// </remarks>
    public static bool Try(this Delegate del, int maxTries, TimeSpan interval, params object[] parameters)
    {
        var success = false;
        var triesCount = 0;
        while (!success)
        {
            try
            {
                del.DynamicInvoke(parameters);
                success = true;
            }
            catch
            {
                triesCount++;
                // 如果达到最大尝试次数，则退出循环
                if (maxTries > 0 && triesCount >= maxTries)
                    break;

                if (interval > TimeSpan.Zero)
                    Thread.Sleep(interval);
            }
        }
        return success;
    }

    /// <summary>
    /// 调用委托并返回结果，如果执行失败则返回默认值
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="del">委托</param>
    /// <param name="defaultValue">默认值，执行失败时返回</param>
    /// <param name="parameters">参数</param>
    /// <returns>执行成功返回结果，失败返回默认值</returns>
    /// <remarks>
    /// 此方法会捕获执行过程中的异常，并尝试执行一次
    /// </remarks>
    public static T InvokeOrDefault<T>(this Delegate del, T defaultValue = default, params object[] parameters) =>
        InvokeOrDefault(del, 1, TimeSpan.Zero, defaultValue, parameters);

    /// <summary>
    /// 调用委托并返回结果，如果执行失败则返回默认值
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="del">委托</param>
    /// <param name="maxTries">最大尝试次数，如果设为0或负值则表示无限尝试</param>
    /// <param name="interval">尝试间隔时间</param>
    /// <param name="defaultValue">默认值，执行失败时返回</param>
    /// <param name="parameters">参数</param>
    /// <returns>执行成功返回结果，失败返回默认值</returns>
    /// <remarks>
    /// 此方法会捕获执行过程中的异常，并根据指定的次数和间隔重试
    /// </remarks>
    public static T InvokeOrDefault<T>(this Delegate del, int maxTries, TimeSpan interval, T defaultValue = default, params object[] parameters)
    {
        var result = defaultValue;

        var triesCount = 0;
        while (true)
        {
            try
            {
                var obj = del.DynamicInvoke(parameters);
                if (obj is T o)
                    result = o;
                else
                    result = defaultValue;
                break;
            }
            catch
            {
                triesCount++;
                // 如果达到最大尝试次数，则返回默认值
                if (maxTries > 0 && triesCount >= maxTries)
                    break;

                if (interval > TimeSpan.Zero)
                    Thread.Sleep(interval);
            }
        }
        return result;
    }
}