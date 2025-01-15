namespace Bing.Text.Joiners;

/// <summary>
/// 公共合并工具
/// </summary>
public static class CommonJoinUtils
{
    /// <summary>
    /// 将集合合并为字符串
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TContainer">容器类型</typeparam>
    /// <param name="container">容器</param>
    /// <param name="containerUpdateFunc">容器更新函数</param>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="to">转换函数</param>
    /// <param name="replaceFunc">替换函数</param>
    public static void JoinToString<T, TContainer>(
        TContainer container, Action<TContainer, string> containerUpdateFunc,
        IEnumerable<T> list, string delimiter,
        Func<T, bool> predicate, Func<T, string> to, Func<T, T> replaceFunc = null)
    {
        if (list is null)
            return;

        var head = true;

        foreach (var item in list)
        {
            var checker = item;
            if (!(predicate?.Invoke(checker) ?? true))
            {
                if (replaceFunc == null)
                    continue;
                checker = replaceFunc(item);
            }

            if (head)
            {
                head = false;
            }
            else
            {
                containerUpdateFunc.Invoke(container, delimiter);
            }

            containerUpdateFunc.Invoke(container, to(checker));
        }
    }

    /// <summary>
    /// 将集合合并为字符串
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TContainer">容器类型</typeparam>
    /// <param name="container">容器</param>
    /// <param name="containerUpdateFunc">容器更新函数</param>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="to">转换函数</param>
    /// <param name="replaceFunc">替换函数</param>
    public static void JoinToString<T, TContainer>(
        TContainer container, Action<TContainer, string> containerUpdateFunc,
        IEnumerable<T> list, string delimiter,
        Func<T, int, bool> predicate, Func<T, int, string> to, Func<T, int, T> replaceFunc = null)
    {
        if (list is null)
            return;

        var head = true;
        var index = -1;

        foreach (var item in list)
        {
            var checker = item;
            index++;

            if (!(predicate?.Invoke(checker, index) ?? true))
            {
                if (replaceFunc == null)
                    continue;
                checker = replaceFunc(item, index);
            }

            if (head)
            {
                head = false;
            }
            else
            {
                containerUpdateFunc.Invoke(container, delimiter);
            }

            containerUpdateFunc.Invoke(container, to(checker, index));
        }
    }
}