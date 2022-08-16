using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Bing.Reflection
{
    /// <summary>
    /// 属性访问选项
    /// </summary>
    public enum PropertyAccessOptions
    {
        /// <summary>
        /// 访问器
        /// </summary>
        Getters,

        /// <summary>
        /// 设置器
        /// </summary>
        Setters,

        /// <summary>
        /// 访问器 + 设置器
        /// </summary>
        Both,
    }

    /// <summary>
    /// 属性反射帮助类
    /// </summary>
    internal static class PropertyReflectionHelper
    {
        /// <summary>
        /// 获取可设置的属性列表
        /// </summary>
        /// <param name="type">类型</param>
        public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstanceSetters(Type type) =>
            type.GetRuntimeProperties().Where(p => p.SetMethod != null && !p.SetMethod.IsStatic && p.SetMethod.IsPublic);

        /// <summary>
        /// 获取可访问的属性列表
        /// </summary>
        /// <param name="type">类型</param>
        public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstanceGetters(Type type) =>
            type.GetRuntimeProperties().Where(p => p.GetMethod != null && !p.GetMethod.IsStatic && p.GetMethod.IsPublic);

        /// <summary>
        /// 获取可访问及设置的属性列表
        /// </summary>
        /// <param name="type">类型</param>
        public static IEnumerable<PropertyInfo> GetPropertiesWithPublicInstance(Type type) =>
            type.GetRuntimeProperties().Where(p =>
                p.SetMethod != null && !p.SetMethod.IsStatic && p.SetMethod.IsPublic
                && p.GetMethod != null && !p.GetMethod.IsStatic && p.GetMethod.IsPublic);

        /// <summary>
        /// 检查属性访问
        /// </summary>
        /// <param name="propertyInfo">属性元数据</param>
        /// <param name="accessOptions">属性访问选项</param>
        public static void CheckPropertyAccess(PropertyInfo propertyInfo, PropertyAccessOptions accessOptions)
        {
            ArgumentException CreatePropertyNotMatchAccessException() => new($"The property ({propertyInfo.Name}) does not match accessibility restrictions.");
            switch (accessOptions)
            {
                case PropertyAccessOptions.Getters:
                    if (!(propertyInfo.GetMethod != null && !propertyInfo.GetMethod.IsStatic && propertyInfo.GetMethod.IsPublic))
                        throw CreatePropertyNotMatchAccessException();
                    break;
                case PropertyAccessOptions.Setters:
                    if (!(propertyInfo.SetMethod != null && !propertyInfo.SetMethod.IsStatic && propertyInfo.SetMethod.IsPublic))
                        throw CreatePropertyNotMatchAccessException();
                    break;
            }
        }
    }

    /// <summary>
    /// 类型访问器
    /// </summary>
    public static partial class TypeVisit
    {
        /// <summary>
        /// 从给定的 <see cref="Type"/> 中获得所有属性
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="accessOptions">属性访问选项</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IEnumerable<PropertyInfo> GetProperties(Type type, PropertyAccessOptions accessOptions)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            return accessOptions switch
            {
                PropertyAccessOptions.Getters => PropertyReflectionHelper.GetPropertiesWithPublicInstanceGetters(type),
                PropertyAccessOptions.Setters => PropertyReflectionHelper.GetPropertiesWithPublicInstanceSetters(type),
                PropertyAccessOptions.Both => PropertyReflectionHelper.GetPropertiesWithPublicInstance(type),
                _ => throw new InvalidOperationException("Invalid operation for unknown access type")
            };
        }

        /// <summary>
        /// 从给定的 <see cref="Type"/> 中获得所有属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="accessOptions">属性访问选项</param>
        /// <param name="propertySelectors">属性选择器集合</param>
        public static IEnumerable<PropertyInfo> GetProperties<T>(PropertyAccessOptions accessOptions, params Expression<Func<T, object>>[] propertySelectors)
        {
            if (propertySelectors is null)
                return GetProperties(typeof(T), accessOptions);
            return GetProperties(propertySelectors, accessOptions);
        }

        /// <summary>
        /// 从给定的 <see cref="Type"/> 中获得所有属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="propertySelectors">属性选择器集合</param>
        /// <param name="accessOptions">属性访问选项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<PropertyInfo> GetProperties<T>(IEnumerable<Expression<Func<T, object>>> propertySelectors, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
        {
            if (propertySelectors is null)
                throw new ArgumentNullException(nameof(propertySelectors));
            return propertySelectors.Select(p => GetProperty(p, accessOptions));
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TProperty">属性类型</typeparam>
        /// <param name="propertySelector">属性选择器</param>
        /// <param name="accessOptions">属性访问选项</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static PropertyInfo GetProperty<T, TProperty>(Expression<Func<T, TProperty>> propertySelector, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
        {
            if (propertySelector is null)
                throw new ArgumentNullException(nameof(propertySelector));
            var member = propertySelector.Body as MemberExpression;

            if (member is null
                && propertySelector.Body.NodeType == ExpressionType.Convert
                && propertySelector.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;

            if (member?.Member is not PropertyInfo propertyInfo)
                throw new ArgumentException($"The expression parameter ({nameof(propertySelector)}) is not a property expression.");

            PropertyReflectionHelper.CheckPropertyAccess(propertyInfo, accessOptions);

            return propertyInfo;
        }

        /// <summary>
        /// 从 <see cref="PropertyInfo"/> 列表中排除所有满足给定条件的 <see cref="PropertyInfo"/>，并返回其余的 <see cref="PropertyInfo"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="properties">属性元数据集合</param>
        /// <param name="expressions">排除属性选择器集合</param>
        public static IEnumerable<PropertyInfo> Exclude<T>(IEnumerable<PropertyInfo> properties, params Expression<Func<T, object>>[] expressions) =>
            Exclude(properties, (IEnumerable<Expression<Func<T, object>>>)expressions);

        /// <summary>
        /// 从 <see cref="PropertyInfo"/> 列表中排除所有满足给定条件的 <see cref="PropertyInfo"/>，并返回其余的 <see cref="PropertyInfo"/>
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="properties">属性元数据集合</param>
        /// <param name="expressions">排除属性选择器集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<PropertyInfo> Exclude<T>(IEnumerable<PropertyInfo> properties, IEnumerable<Expression<Func<T, object>>> expressions)
        {
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));
            if (expressions is null)
                throw new ArgumentNullException(nameof(expressions));
            
            ISet<PropertyInfo> excluded = new HashSet<PropertyInfo>(GetProperties(expressions));

            return properties.Where(p => !excluded.Contains(p));
        }

        /// <summary>
        /// 判断属性是可见且为虚方法
        /// </summary>
        /// <param name="property">属性元数据</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsVisibleAndVirtual(PropertyInfo property)
        {
            if (property is null)
                throw new ArgumentNullException(nameof(property));
            return (property.CanRead && property.GetMethod.IsVisibleAndVirtual()) ||
                   (property.CanWrite && property.GetMethod.IsVisibleAndVirtual());
        }
    }

    /// <summary>
    /// 类型访问器扩展
    /// </summary>
    public static partial class TypeVisitExtensions
    {
        /// <summary>
        /// 获取属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <typeparam name="TProperty">属性类型</typeparam>
        /// <param name="this">当前对象</param>
        /// <param name="propertySelector">属性选择器</param>
        /// <param name="accessOptions">属性访问选项</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static PropertyInfo GetProperty<T, TProperty>(this T @this, Expression<Func<T, TProperty>> propertySelector, PropertyAccessOptions accessOptions = PropertyAccessOptions.Both)
        {
            if (@this is null)
                throw new ArgumentNullException(nameof(@this));
            if (propertySelector is null)
                throw new ArgumentNullException(nameof(propertySelector));
            return TypeVisit.GetProperty(propertySelector, accessOptions);
        }


        /// <summary>
        /// 从给定的 <see cref="Type"/> 中获得所有属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="this">当前对象</param>
        /// <param name="propertySelectors">属性选择器集合</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<PropertyInfo> GetProperties<T>(this T @this, params Expression<Func<T, object>>[] propertySelectors)
        {
            if (@this is null)
                throw new ArgumentNullException(nameof(@this));
            return TypeVisit.GetProperties(PropertyAccessOptions.Both, propertySelectors);
        }

        /// <summary>
        /// 从给定的 <see cref="Type"/> 中获得所有属性
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="this">当前对象</param>
        /// <param name="accessOptions"></param>
        /// <param name="propertySelectors">属性选择器集合</param>
        /// <exception cref="ArgumentNullException">属性访问选项</exception>
        public static IEnumerable<PropertyInfo> GetProperties<T>(this T @this, PropertyAccessOptions accessOptions, params Expression<Func<T, object>>[] propertySelectors)
        {
            if (@this is null)
                throw new ArgumentNullException(nameof(@this));
            return TypeVisit.GetProperties(accessOptions, propertySelectors);
        }
    }

    /// <summary>
    /// 类型元数据访问器扩展
    /// </summary>
    public static partial class TypeMetaVisitExtensions
    {
        /// <summary>
        /// 判断指定属性是否是虚属性
        /// </summary>
        /// <param name="property">属性元数据</param>
        public static bool IsVirtual(this PropertyInfo property)
        {
            var accessor = property.GetAccessors().FirstOrDefault();
            return accessor is not null && accessor.IsVirtual && !accessor.IsFinal;
        }

        /// <summary>
        /// 判断指定属性是否是抽象属性
        /// </summary>
        /// <param name="property">属性元数据</param>
        public static bool IsAbstract(this PropertyInfo property)
        {
            var accessor = property.GetAccessors().FirstOrDefault();
            return accessor is not null && accessor.IsAbstract && !accessor.IsFinal;
        }

        /// <summary>
        /// 判断属性是可见且为虚方法
        /// </summary>
        /// <param name="property">属性元数据</param>
        public static bool IsVisibleAndVirtual(this PropertyInfo property) => TypeVisit.IsVisibleAndVirtual(property);
    }
}
