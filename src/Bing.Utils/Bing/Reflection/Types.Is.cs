using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bing.Reflection
{
    /// <summary>
    /// 类型判断现象
    /// </summary>
    public enum TypeIsOptions
    {
        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 忽略可空
        /// </summary>
        IgnoreNullable = 1
    }

    /// <summary>
    /// Bing 类型 扩展
    /// </summary>
    internal static partial class TypeExtensions
    {
        /// <summary>
        /// 获取拆箱类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">值</param>
        public static Type GetUnboxedType<T>(this T value) => typeof(T);
    }

    /// <summary>
    /// 类型 操作
    /// </summary>
    public static partial class Types
    {
        #region Tuple


        #endregion
    }
}
