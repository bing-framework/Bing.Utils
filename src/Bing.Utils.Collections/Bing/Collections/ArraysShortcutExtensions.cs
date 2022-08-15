using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Bing.Collections
{
    /// <summary>
    /// 数组捷径扩展
    /// </summary>
    public static partial class ArraysShortcutExtensions
    {
        /// <summary>
        /// 二进制查询。<br />
        /// 在整个一维排序数组中搜索特定元素，使用由数组的每个元素和指定的对象实现的 <see cref="T:System.IComparable" /> 接口。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch(this Array array, object value) => Array.BinarySearch(array, value);

        /// <summary>
        /// 二进制查询。<br />
        /// 使用指定的 <see cref="T:System.Collections.IComparer" /> 接口在整个一维排序数组中搜索值。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch(this Array array, int index, int length, object value) => Array.BinarySearch(array, index, length, value);

        /// <summary>
        /// 二进制查询。<br />
        /// 使用 <see cref="T:System.IComparable`1" /> 通用接口，由 <see cref="T:System.Array" /> 的每个元素和指定的值实现。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch(this Array array, object value, IComparer comparer) => Array.BinarySearch(array, value, comparer);

        /// <summary>
        /// 二进制查询。<br />
        /// 在一个一维排序数组的元素范围内搜索一个值，使用指定的 <see cref="T:System.Collections.Generic.IComparer`1" /> 通用接口。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        /// <param name="value">值</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch(this Array array, int index, int length, object value, IComparer comparer) => Array.BinarySearch(array, index, length, value, comparer);

        /// <summary>
        /// 清空。<br />
        /// 将数组中的元素范围设置为每个元素类型的默认值。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this Array array, int index, int length) => Array.Clear(array, index, length);

        /// <summary>
        /// 清空。<br />
        /// 将数组中的所有元素设置为每种元素类型的默认值。
        /// </summary>
        /// <param name="array">数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this Array array) => Array.Clear(array, 0, array.Length);

        /// <summary>
        /// 查找。<br />
        /// 检索与指定谓词定义的条件匹配的所有元素。
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="array">数组</param>
        /// <param name="condition">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindAll<T>(this T[] array, Predicate<T> condition) => Array.FindAll(array, condition);

        /// <summary>
        /// 获取指定对象的索引。<br />
        /// 搜索指定的对象并返回其在一维数组中第一次出现的索引。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this Array array, object value) => Array.IndexOf(array, value);

        /// <summary>
        /// 获取指定对象的索引。<br />
        /// 在一个一维数组的元素范围内搜索指定对象，并返回其第一次出现的索引。 范围从指定的索引扩展到数组的末尾。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        /// <param name="startIndex">起始索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this Array array, object value, int startIndex) => Array.IndexOf(array, value, startIndex);

        /// <summary>
        /// 获取指定对象的索引。<br />
        /// 在一维数组的元素范围内搜索指定对象，并返回ifs第一次出现的索引。 范围从指定数量的元素的指定索引开始。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">计数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this Array array, object value, int startIndex, int count) => Array.IndexOf(array, value, startIndex, count);

        /// <summary>
        /// 获取指定对象的最后索引。<br />
        /// 搜索指定对象并返回整个一维 <see cref="T:System.Array" /> 中最后一次出现的索引。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this Array array, object value) => Array.LastIndexOf(array, value);

        /// <summary>
        /// 获取指定对象的最后索引。<br />
        /// 搜索指定对象并返回一维 <see cref="T:System.Array" /> 元素范围内最后一次出现的索引，该元素从第一个元素延伸到指定索引。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        /// <param name="startIndex">起始索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this Array array, object value, int startIndex) => Array.LastIndexOf(array, value, startIndex);

        /// <summary>
        /// 获取指定对象的最后索引。<br />
        /// 搜索指定对象并返回一维 <see cref="T:System.Array" /> 中元素范围内最后一次出现的索引，该元素包含指定数量的元素并在指定索引处结束。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="value">值</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">计数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf(this Array array, object value, int startIndex, int count) => Array.LastIndexOf(array, value, startIndex, count);

        /// <summary>
        /// 反转。<br />
        /// 反转整个一维<see cref="T:System.Array" />中元素的顺序。
        /// </summary>
        /// <param name="array">数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse(this Array array) => Array.Reverse(array);

        /// <summary>
        /// 反转。<br />
        /// 反转一维 <see cref="T:System.Array" /> 中元素子集的顺序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse(this Array array, int index, int length) => Array.Reverse(array, index, length);

        /// <summary>
        /// 排序。<br />
        /// 使用 <see cref="T:System.Array" /> 的每个元素的 <see cref="T:System.IComparable" /> 实现对整个一维数组中的元素进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array) => Array.Sort(array);

        /// <summary>
        /// 排序。<br />
        /// 使用每个键的 <see cref="T:System.IComparable" /> 实现，基于第一个 Array 中的键对一对一维 Array 对象（一个包含键，另一个包含相应项）进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="items">其它项数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, Array items) => Array.Sort(array, items);

        /// <summary>
        /// 排序。<br />
        /// 使用指定的 <see cref="T:System.Collections.IComparer" /> 根据第一个 Array 中的键对一对一维 Array 对象（一个包含键，另一个包含相应项）进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, int index, int length) => Array.Sort(array, index, length);

        /// <summary>
        /// 排序。<br />
        /// 使用 <see cref="T:System.IComparable" /> 根据第一个 Array 中的键对一对一维 Array 对象（一个包含键，另一个包含相应项）中的一系列元素进行排序 每个键的实现。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="items">其它项数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, Array items, int index, int length) => Array.Sort(array, items, index, length);

        /// <summary>
        /// 排序。<br />
        /// 使用指定的 <see cref="T:System.Collections.IComparer" /> 对一维 <see cref="T:System.Array" /> 中的元素进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, IComparer comparer) => Array.Sort(array, comparer);

        /// <summary>
        /// 排序。<br />
        /// 使用指定的 IComparer 根据第一个 Array 中的键对一对一维 Array 对象（一个包含键，另一个包含相应项）中的一系列元素进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="items">其它项数组</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, Array items, IComparer comparer) => Array.Sort(array, items, comparer);

        /// <summary>
        /// 排序。<br />
        /// 使用指定的 <see cref="T:System.Collections.IComparer" /> 对一维 <see cref="T:System.Array" /> 中的元素进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, int index, int length, IComparer comparer) =>
            Array.Sort(array, index, length, comparer);

        /// <summary>
        /// 排序。<br />
        /// 使用指定的 IComparer 根据第一个 Array 中的键对一对一维 Array 对象（一个包含键，另一个包含相应项）中的一系列元素进行排序。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="items">其它项数组</param>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        /// <param name="comparer">比较器</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this Array array, Array items, int index, int length, IComparer comparer) => Array.Sort(array, items, index, length, comparer);

        /// <summary>
        /// 获取指定数组中的字节数。
        /// </summary>
        /// <param name="array">数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ByteLength(this Array array) => Buffer.ByteLength(array);

        /// <summary>
        /// 获取指定数组中指定位置的字节。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetByte(this Array array, int index) => Buffer.GetByte(array, index);

        /// <summary>
        /// 设置字节。<br />
        /// 将指定值分配给指定数组中特定位置的字节。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetByte(this Array array, int index, byte value) => Buffer.SetByte(array, index, value);
    }
}