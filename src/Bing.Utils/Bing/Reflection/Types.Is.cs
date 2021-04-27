using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bing.Reflection
{
    /// <summary>
    /// 类型判断选项
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

        /// <summary>
        /// 判断给定的类型是否为元祖类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="ofOptions">类型选项</param>
        /// <param name="isOptions">类型判断选项</param>
        /// <returns></returns>
        public static bool IsTupleType(Type type, TypeOfOptions ofOptions = TypeOfOptions.Owner,
            TypeIsOptions isOptions = TypeIsOptions.Default)
        {
            if (type is null)
                return false;
            if (isOptions == TypeIsOptions.IgnoreNullable)
                type = TypeConv.GetNonNullableType(type);

            if (type == typeof(Tuple) || type == typeof(ValueTuple))
                return true;

            while (type != null)
            {
                if (type.IsGenericType)
                {
                    var genType = type.GetGenericTypeDefinition();
                    if (genType == typeof(Tuple<>)
                        || genType == typeof(Tuple<,>)
                        || genType == typeof(Tuple<,,>)
                        || genType == typeof(Tuple<,,,>)
                        || genType == typeof(Tuple<,,,,>)
                        || genType == typeof(Tuple<,,,,,>)
                        || genType == typeof(Tuple<,,,,,,>)
                        || genType == typeof(Tuple<,,,,,,,>))
                        return true;

                    if (genType == typeof(ValueTuple<>)
                        || genType == typeof(ValueTuple<,>)
                        || genType == typeof(ValueTuple<,,>)
                        || genType == typeof(ValueTuple<,,,>)
                        || genType == typeof(ValueTuple<,,,,>)
                        || genType == typeof(ValueTuple<,,,,,>)
                        || genType == typeof(ValueTuple<,,,,,,>)
                        || genType == typeof(ValueTuple<,,,,,,,>))
                        return true;
                }

                if (ofOptions == TypeOfOptions.Owner)
                    break;

                type = type.BaseType;
            }

            return false;
        }
        /// <summary>
        /// 判断给定的类型是否为元组类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="ofOptions">类型选项</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsTupleType<T>(TypeOfOptions ofOptions = TypeOfOptions.Owner,
            TypeIsOptions isOptions = TypeIsOptions.Default) =>
            IsTupleType(typeof(T), ofOptions, isOptions);

        /// <summary>
        /// 判断给定的对象是否为元祖类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="value">值</param>
        /// <param name="ofOptions">类型选项</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsTupleType<T>(T value, TypeOfOptions ofOptions = TypeOfOptions.Owner,
            TypeIsOptions isOptions = TypeIsOptions.Default)
        {
            var type = value?.GetUnboxedType();
            return type != null && IsTupleType(type, ofOptions, isOptions);
        }

        #endregion
    }
}
