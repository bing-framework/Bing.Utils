// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 可枚举类型(<see cref="IEnumerable{T}"/>) 扩展
/// </summary>
public static partial class EnumerableExtensions
{
    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");
        foreach (var item in enumerable)
            action(item);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");

        var index = 0;
        foreach (var item in enumerable)
            action(item, index++);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="enumerable">值</param>
    /// <param name="action">操作</param>
    /// <param name="maxDegreeOfParallelism">最大并行度，默认为无限制</param>
    /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
    public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action, int? maxDegreeOfParallelism = null)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
        if (action == null)
            throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");
        // 如果未指定并行度，直接并行执行所有任务
        if (!maxDegreeOfParallelism.HasValue)
            return Task.WhenAll(enumerable.Select(action));
        // 有并行度限制时，使用 SemaphoreSlim 控制并行度
        return ParallelForEachAsync(enumerable, action, maxDegreeOfParallelism.Value);
    }

    /// <summary>
    /// 对指定集合中的每个元素执行指定操作
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">源</param>
    /// <param name="action">操作</param>
    /// <param name="maxDegreeOfParallelism">最大并行度</param>
    /// <returns></returns>
    private static async Task ParallelForEachAsync<T>(IEnumerable<T> source, Func<T, Task> action, int maxDegreeOfParallelism)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task>();
        foreach (var item in source)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await action(item);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        await Task.WhenAll(tasks);
    }
}