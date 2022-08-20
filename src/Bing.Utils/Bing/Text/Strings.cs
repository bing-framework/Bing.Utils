using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bing.Text
{
    /// <summary>
    /// 字符串工具
    /// </summary>
    public static partial class Strings
    {
        
        #region Filter

        /// <summary>
        /// 过滤为字符
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="predicate">条件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<char> FilterByChar(string text, Func<char, bool> predicate) => text.ToCharArray().Where(predicate);

        /// <summary>
        /// 只获取字母和数字
        /// </summary>
        /// <param name="text">文本</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<char> FilterForNumbersAndLetters(string text)
        {
            return FilterByChar(text, LocalCheck);

            bool LocalCheck(char @char) => (@char >= 'a' && @char <= 'z') || (@char >= 'A' && @char <= 'Z') || (@char >= '0' && @char <= '9');
        }

        /// <summary>
        /// 只获取数字
        /// </summary>
        /// <param name="text">文本</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<char> FilterForNumbers(string text)
        {
            return FilterByChar(text, LocalCheck);

            bool LocalCheck(char @char) => @char >= '0' && @char <= '9';
        }

        /// <summary>
        /// 只获取字母
        /// </summary>
        /// <param name="text">文本</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<char> FilterForLetters(string text)
        {
            return FilterByChar(text, LocalCheck);

            bool LocalCheck(char @char) => (@char >= 'a' && @char <= 'z') || (@char >= 'A' && @char <= 'Z');
        }

        #endregion

        #region Merge

        /// <summary>
        /// 将字符集合合并为一个字符串
        /// </summary>
        /// <param name="chars">字符集合</param>
        public static string Merge(IEnumerable<char> chars)
        {
            var sb = new StringBuilder();
            if(chars is not null)
                foreach (var val in chars)
                    sb.Append(val);
            return sb.ToString();
        }

        /// <summary>
        /// 将字符集合合并为一个字符串
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="strings">字符集合</param>
        public static string Merge(string text, params string[] strings)
        {
            var sb = new StringBuilder();
            sb.Append(text);
            if(strings is not null)
                foreach (var val in strings)
                    sb.Append(val);
            return sb.ToString();
        }

        #endregion
    }
}