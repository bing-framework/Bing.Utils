using System;
using System.Reflection;

namespace Bing.Reflection
{
    /// <summary>
    /// 成员访问器帮助类
    /// </summary>
    internal static class MemberVisitHelper
    {
        /// <summary>
        /// 获取实际类型
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static Type GetActualType(MemberInfo member) =>
            member switch
            {
                TypeInfo typeInfo => typeInfo.AsType(),
                Type type => type,
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                MethodInfo method => method.ReturnType,
                ConstructorInfo constructor => constructor.DeclaringType,
                _ => throw new InvalidOperationException("Current MemberInfo cannot be converted to Reflector.")
            };

        /// <summary>
        /// 获取实际类型
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        public static Type GetActualType(ParameterInfo parameter) => parameter.ParameterType;
    }

    /// <summary>
    /// 类型元数据访问器
    /// </summary>
    public static partial class TypeMetaVisitExtensions
    {
        /// <summary>
        /// 判断给定的成员是否为数字类型
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="options">类型判断选项</param>
        public static bool IsNumeric(this MemberInfo member, TypeIsOptions options = TypeIsOptions.Default) =>
            Types.IsNumericType(MemberVisitHelper.GetActualType(member), options);

        /// <summary>
        /// 判断给定的参数是否为数字类型
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="options">类型判断选项</param>
        public static bool IsNumeric(this ParameterInfo parameter, TypeIsOptions options = TypeIsOptions.Default) =>
            Types.IsNumericType(MemberVisitHelper.GetActualType(parameter), options);

        /// <summary>
        /// 判断给定的成员是否为元祖类型
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="ofOptions">类型选项</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsTupleType(this MemberInfo member, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default) =>
            Types.IsTupleType(MemberVisitHelper.GetActualType(member), ofOptions, isOptions);

        /// <summary>
        /// 判断给定的参数是否为元祖类型
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="ofOptions">类型选项</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsTupleType(this ParameterInfo parameter, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default) =>
            Types.IsTupleType(MemberVisitHelper.GetActualType(parameter), ofOptions, isOptions);

        /// <summary>
        /// 判断给定的成员是否为结构类型
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsStructType(this MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default) =>
            Types.IsStructType(MemberVisitHelper.GetActualType(member), isOptions);

        /// <summary>
        /// 判断给定的参数是否为结构类型
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="isOptions">类型判断选项</param>
        public static bool IsStructType(this ParameterInfo parameter, TypeIsOptions isOptions = TypeIsOptions.Default) =>
            Types.IsStructType(MemberVisitHelper.GetActualType(parameter), isOptions);
    }
}