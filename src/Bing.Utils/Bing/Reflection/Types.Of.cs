using System;

namespace Bing.Reflection
{
    /// <summary>
    /// 类型选项
    /// </summary>
    public enum TypeOfOptions
    {
        /// <summary>
        /// 拥有者
        /// </summary>
        Owner = 0,
        /// <summary>
        /// 基础类型
        /// </summary>
        Underlying = 1
    }

    /// <summary>
    /// 类型 操作
    /// </summary>
    public static partial class Types
    {
        ///// <summary>
        ///// 根据
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="options"></param>
        ///// <returns></returns>
        //public static Type Of<T>(TypeOfOptions options = TypeOfOptions.Owner)
        //{
        //    return options switch
        //    {
        //        TypeOfOptions.Owner => typeof(T),
        //        TypeOfOptions.Underlying => Reflections.GetUnderlyingType<T>(),
        //        _ => typeof(T)
        //    };
        //}
    }
}
