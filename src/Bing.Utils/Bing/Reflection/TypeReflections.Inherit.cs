using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bing.Reflection
{
    /// <summary>
    /// 类型派生选项
    /// </summary>
    public enum TypeDerivedOptions
    {
        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,
        /// <summary>
        /// 允许识别抽象类
        /// </summary>
        CanAbstract = 1
    }

    // 类型反射 - 派生(继承)
    public static partial class TypeReflections
    {
        #region IsObjectDerivedFrom

        /// <summary>
        /// 判断对象是否派生自给定的类型
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <param name="value">值</param>
        /// <param name="parentType">父类型</param>
        /// <param name="isOptions">类型判断选项</param>
        /// <param name="derivedOptions">类型派生选项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsObjectDerivedFrom<TSource>(TSource value, Type parentType,
            TypeIsOptions isOptions = TypeIsOptions.Default,
            TypeDerivedOptions derivedOptions = TypeDerivedOptions.Default)
        {
            if (parentType is null)
                throw new ArgumentNullException(nameof(parentType));
            return isOptions switch
            {
                TypeIsOptions.Default => value is not null && IsTypeDerivedFrom(typeof(TSource), parentType, derivedOptions),
                TypeIsOptions.IgnoreNullable => IsTypeDerivedFrom(typeof(TSource), parentType, derivedOptions),
                _ => value is not null && IsTypeDerivedFrom(typeof(TSource), parentType, derivedOptions)
            };
        }

        /// <summary>
        /// 判断对象是否派生自给定的类型
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TParent">父类型</typeparam>
        /// <param name="value">值</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsObjectDerivedFrom<TSource, TParent>(TSource value, TypeIsOptions isOptions = TypeIsOptions.Default) =>
            IsObjectDerivedFrom(value, typeof(TParent), isOptions);

        #endregion

        #region IsTypeDerivedFrom

        /// <summary>
        /// 判断类型是否派生自给定的类型
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentType">父类型</param>
        /// <param name="derivedOptions">类型派生选项</param>
        public static bool IsTypeDerivedFrom(Type sourceType, Type parentType, TypeDerivedOptions derivedOptions = TypeDerivedOptions.Default)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));
            if (parentType == null)
                throw new ArgumentNullException(nameof(parentType));

            return sourceType.IsClass 
                   && AbstractPredicate() 
                   && IsTypeBasedOn(sourceType, parentType);

            bool AbstractPredicate()
            {
                return derivedOptions switch
                {
                    TypeDerivedOptions.Default => !sourceType.IsAbstract,
                    TypeDerivedOptions.CanAbstract => true,
                    _ => !sourceType.IsAbstract
                };
            }
        }

        /// <summary>
        /// 判断类型是否派生自给定的类型
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TParent">父类型</typeparam>
        /// <param name="derivedOptions">类型派生选项</param>
        public static bool IsTypeDerivedFrom<TSource, TParent>(TypeDerivedOptions derivedOptions = TypeDerivedOptions.Default) =>
            IsTypeDerivedFrom(typeof(TSource), typeof(TParent), derivedOptions);

        #endregion

        #region IsTypeBasedOn

        /// <summary>
        /// 判断给定的左侧类型是否派生自右侧类型
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentType">父类型</param>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTypeBasedOn(Type sourceType, Type parentType)
        {
            if (sourceType is null)
                throw new ArgumentNullException(nameof(sourceType));
            if (parentType is null)
                throw new ArgumentNullException(nameof(parentType));
            if (parentType.IsGenericTypeDefinition)
                return IsImplementationOfGenericType(sourceType, parentType, out _);
            return parentType.IsAssignableFrom(sourceType);
        }

        /// <summary>
        /// 判断给定的左侧类型是否派生自右侧类型
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TParent">父类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTypeBasedOn<TSource, TParent>() => IsTypeBasedOn(typeof(TSource), typeof(TParent));

        #endregion

        #region IsGenericImplementation

        /// <summary>
        /// 判断给定的类型是否可赋值给指定的泛型类型
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentGenericType">父泛型类型</param>
        /// <param name="genericType">泛型类型</param>
        /// <param name="genericArguments">泛型参数</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsImplementationOfGenericType(Type sourceType, Type parentGenericType, out Type genericType,
            out Type[] genericArguments)
        {
            if (sourceType is null)
                throw new ArgumentNullException(nameof(sourceType));
            if (parentGenericType is null)
                throw new ArgumentNullException(nameof(parentGenericType));

            genericArguments = null;
            genericType = null;
            Type currentType = null;

            if (parentGenericType.IsGenericType is false)
                return false;

            if (parentGenericType.IsInterface)
            {
                if (sourceType.GetInterfaces().Any(_checkRawGenericType))
                {
                    genericType = currentType;
                    genericArguments = currentType.GetGenericArguments();
                    return true;
                }
            }

            while (sourceType!=null&&sourceType!=typeof(object))
            {
                if (_checkRawGenericType(sourceType))
                {
                    genericType = currentType;
                    genericArguments = currentType.GetGenericArguments();
                    return true;
                }

                sourceType = sourceType.BaseType;
            }

            return false;

            // 检查给定的这个 test 类型是否等于指定类 Class 或接口 Interface 的类型 Type，通过 currentType 返回当前类型
            // ReSharper disable once InconsistentNaming
            bool _checkRawGenericType(Type test)
            {
                currentType = null;
                if (test.IsGenericType is false)
                    return false;
                if (parentGenericType.IsGenericTypeDefinition)
                    return parentGenericType == test.GetGenericTypeDefinition() && (currentType = test) == currentType;
                return parentGenericType == test && (currentType = test) == currentType;
            }
        }

        /// <summary>
        /// 判断给定的类型是否可赋值给指定的泛型类型
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentGenericType">父泛型类型</param>
        /// <param name="genericArguments">泛型参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImplementationOfGenericType(Type sourceType, Type parentGenericType, out Type[] genericArguments) =>
            IsImplementationOfGenericType(sourceType, parentGenericType, out _, out genericArguments);

        /// <summary>
        /// 判断给定的类型是否可赋值给指定的泛型类型
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TGenericParent">父泛型类型</typeparam>
        /// <param name="genericArguments">泛型参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImplementationOfGenericType<TSource, TGenericParent>(out Type[] genericArguments) => 
            IsImplementationOfGenericType(typeof(TSource), typeof(TGenericParent), out genericArguments);

        #endregion

        #region GetRawTypeFromGenericType

        /// <summary>
        /// 获取原始 <paramref name="sourceType"/>。 <br />
        /// 当类型从 genericType 继承时，返回该类型。
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentGenericType">父泛型类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetRawTypeFromGenericType(Type sourceType, Type parentGenericType) =>
            IsImplementationOfGenericType(sourceType, parentGenericType, out var genericType, out _)
                ? genericType
                : default;

        /// <summary>
        /// 获取原始 <typeparamref name="TSource"/>。 <br />
        /// 当类型从 genericType 继承时，返回该类型。
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TGenericParent">父泛型类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetRawTypeFromGenericType<TSource, TGenericParent>() => GetRawTypeFromGenericType(typeof(TSource), typeof(TGenericParent));

        #endregion

        #region GetRawTypeArgsFromGenericType

        /// <summary>
        /// 获取原始 <paramref name="sourceType"/>。 <br />
        /// 当type继承自genericType时，获取 <paramref name="sourceType"/> 对应的 <paramref name="parentGenericType"/> 中的所有泛型参数。
        /// </summary>
        /// <param name="sourceType">来源类型</param>
        /// <param name="parentGenericType">父泛型类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypesVal GetRawTypeArgsFromGenericType(Type sourceType, Type parentGenericType) =>
            IsImplementationOfGenericType(sourceType, parentGenericType, out _, out var genericArguments)
                ? TypesVal.Create(genericArguments)
                : TypesVal.Empty;

        /// <summary>
        /// 获取原始 <typeparamref name="TSource"/>。 <br />
        /// 当type继承自genericType时，获取 <typeparamref name="TSource"/> 对应的 <typeparamref name="TGenericParent"/> 中的所有泛型参数。
        /// </summary>
        /// <typeparam name="TSource">来源类型</typeparam>
        /// <typeparam name="TGenericParent">父泛型类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypesVal GetRawTypeArgsFromGenericType<TSource, TGenericParent>() => GetRawTypeArgsFromGenericType(typeof(TSource), typeof(TGenericParent));

        #endregion

        #region GetEnumUnderlyingType

        /// <summary>
        /// 获取枚举的底层类型
        /// </summary>
        /// <param name="type">类型</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetEnumUnderlyingType(Type type)
        {
            if (type is null)
                return null;
            if (!type.IsEnum)
                return null;
            return type.GetEnumUnderlyingType();
        }

        /// <summary>
        /// 获取枚举的底层类型
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetEnumUnderlyingType<T>() => GetEnumUnderlyingType(typeof(T));

        #endregion
    }
}
