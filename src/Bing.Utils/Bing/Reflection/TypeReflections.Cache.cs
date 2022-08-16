using System;
using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Reflection;

namespace Bing.Reflection
{
    // 类型反射 - 缓存
    public static partial class TypeReflections
    {
        /// <summary>
        /// 类型缓存管理器
        /// </summary>
        public static class TypeCacheManager
        {
            /// <summary>
            /// 类型 - 属性 缓存
            /// </summary>
            private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TypePropertyCache = new();

            /// <summary>
            /// 类型 - 字段 缓存
            /// </summary>
            private static readonly ConcurrentDictionary<Type, FieldInfo[]> TypeFieldCache = new();

            /// <summary>
            /// 类型 - 方法 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<Type, MethodInfo[]> TypeMethodCache = new();

            /// <summary>
            /// 类型 - new函数Func 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<Type, Func<ServiceContainer, object>> TypeNewFuncCache = new();

            /// <summary>
            /// 类型 - 构造函数 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<Type, ConstructorInfo> TypeConstructorCache = new();

            /// <summary>
            /// 类型 - 空构造函数Func 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<Type, Func<object>> TypeEmptyConstructorFuncCache = new();

            /// <summary>
            /// 类型 - 构造函数Func 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<Type, Func<object[], object>> TypeConstructorFuncCache = new();

            /// <summary>
            /// 属性 - 值 获取器 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> PropertyValueGetters = new();

            /// <summary>
            /// 属性 - 值 设置器 缓存
            /// </summary>
            internal static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> PropertyValueSetters = new();

            /// <summary>
            /// 获取指定类型的属性信息数组
            /// </summary>
            /// <param name="type">类型</param>
            /// <exception cref="ArgumentNullException"></exception>
            public static PropertyInfo[] GetTypeProperties(Type type)
            {
                if (null == type)
                    throw new ArgumentNullException(nameof(type));
                return TypePropertyCache.GetOrAdd(type, t => t.GetProperties());
            }

            /// <summary>
            /// 获取指定类型的字段信息数组
            /// </summary>
            /// <param name="type">类型</param>
            /// <exception cref="ArgumentNullException"></exception>
            public static FieldInfo[] GetTypeFields(Type type)
            {
                if (null == type)
                    throw new ArgumentNullException(nameof(type));
                return TypeFieldCache.GetOrAdd(type, t => t.GetFields());
            }
        }
    }
}
