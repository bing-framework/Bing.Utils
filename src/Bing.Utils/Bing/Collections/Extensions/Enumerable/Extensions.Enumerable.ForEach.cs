using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Bing.Collections
{
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
            var array = enumerable.ToArray();
            for (var i = 0; i < array.Length; i++) 
                action(array[i], i);
        }

        /// <summary>
        /// 对指定集合中的每个元素执行指定操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="enumerable">值</param>
        /// <param name="action">操作</param>
        /// <exception cref="ArgumentNullException">源集合对象为空、操作表达式为空</exception>
        public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable), $@"源{typeof(T).Name}集合对象不可为空！");
            if (action == null)
                throw new ArgumentNullException(nameof(action), @"操作表达式不可为空！");
            return Task.WhenAll(from item in enumerable select Task.Run(() => action(item)));
        }
    }
}
