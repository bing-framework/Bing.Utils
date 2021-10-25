using System;
using System.Collections.Generic;

namespace Bing.Reflection
{
    /// <summary>
    /// 类型集合值
    /// </summary>
    public class TypesVal
    {
        /// <summary>
        /// 类型数组
        /// </summary>
        private readonly Type[] _types;

        /// <summary>
        /// 初始化一个<see cref="TypesVal"/>类型的实例
        /// </summary>
        /// <param name="size">数组大小</param>
        private TypesVal(int size)
        {
            _types = new Type[size];
            Count = size;
        }
        
        /// <summary>
        /// 计算类型集合值中的类型总数
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 从类型集合值中获取所有类型
        /// </summary>
        public Type[] TypeArray
        {
            get
            {
                var array = new Type[Count];
                Array.Copy(_types, array, Count);
                return array;
            }
        }

        /// <summary>
        /// 从类型集合值中获取所有类型
        /// </summary>
        public IEnumerable<Type> Types => TypeArray;

        /// <summary>
        /// 通过索引值获取类型
        /// </summary>
        /// <param name="index">索引</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Type this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _types[index];
            }
        }

        /// <summary>
        /// 根据给定的类型数组，构建一个类型集合值
        /// </summary>
        /// <param name="types">类型数组</param>
        public static TypesVal Create(params Type[] types)
        {
            if (types is null || types.Length <= 0)
                return Empty;
            var val = new TypesVal(types.Length);
            Array.Copy(types, val._types, val.Count);
            return val;
        }

        /// <summary>
        /// 获取空的类型集合值
        /// </summary>
        public static TypesVal Empty { get; } = new TypesVal(0);

        /// <summary>
        /// 标记是否为空的类型集合值
        /// </summary>
        public bool IsEmpty() => Count == 0;
    }
}
