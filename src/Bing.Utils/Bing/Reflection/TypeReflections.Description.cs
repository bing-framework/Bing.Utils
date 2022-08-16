using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Bing.Reflection
{
    // 类型反射操作 - 描述
    public static partial class TypeReflections
    {
        #region IsDescriptionDefined

        /// <summary>
        /// 判断给定的 <see cref="DescriptionAttribute"/> 或 <see cref="DisplayAttribute"/> 是否定义
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static bool IsDescriptionDefined(MemberInfo member, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            member is not null && (IsAttributeDefined<DescriptionAttribute>(member, refOptions) || IsAttributeDefined<DisplayAttribute>(member, refOptions));

        /// <summary>
        /// 判断给定的 <see cref="DescriptionAttribute"/> 或 <see cref="DisplayAttribute"/> 是否定义
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static bool IsDescriptionDefined(ParameterInfo parameter, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            parameter is not null && (IsAttributeDefined<DescriptionAttribute>(parameter, refOptions) || IsAttributeDefined<DisplayAttribute>(parameter, refOptions));

        #endregion

        #region GetDescription

        /// <summary>
        /// 获取描述 的实现方式
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDescriptionImpl(MemberInfo member, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (member is null)
                return string.Empty;
            if (GetAttribute(member, typeof(DescriptionAttribute), refOptions, ambOptions) is DescriptionAttribute attribute)
                return attribute.Description;
            if (GetAttribute(member, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Description;
            return member.Name;
        }

        /// <summary>
        /// 获取描述 的实现方式
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDescriptionImpl(ParameterInfo parameter, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (parameter is null)
                return string.Empty;
            if (GetAttribute(parameter, typeof(DescriptionAttribute), refOptions, ambOptions) is DescriptionAttribute attribute)
                return attribute.Description;
            if (GetAttribute(parameter, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Description;
            return parameter.Name;
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription(MemberInfo member, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionImpl(member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription(ParameterInfo parameter, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionImpl(parameter, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription<T>(ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionImpl(typeof(T), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription(Type type, string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (type is null)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = type.GetMembers();
            return GetDescriptionImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription<T>(string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = typeof(T).GetMembers();
            return GetDescriptionImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescription<T>(Expression<Func<T, object>> expression, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            var member = expression.Body as MemberExpression;
            if (member is null
                && expression.Body.NodeType == ExpressionType.Convert
                && expression.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;
            return GetDescriptionImpl(member?.Member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        #endregion

        #region GetDisplayName

        /// <summary>
        /// 获取显示名称 的实现方式
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDisplayNameImpl(MemberInfo member, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (member is null)
                return string.Empty;
            if (GetAttribute(member, typeof(DisplayNameAttribute), refOptions, ambOptions) is DisplayNameAttribute displayNameAttribute)
                return displayNameAttribute.DisplayName;
            if (GetAttribute(member, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Name;
            return member.Name;
        }

        /// <summary>
        /// 获取显示名称 的实现方式
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDisplayNameImpl(ParameterInfo parameter, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (parameter is null)
                return string.Empty;
            if (GetAttribute(parameter, typeof(DisplayNameAttribute), refOptions, ambOptions) is DisplayNameAttribute displayNameAttribute)
                return displayNameAttribute.DisplayName;
            if (GetAttribute(parameter, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Name;
            return parameter.Name;
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName(MemberInfo member, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameImpl(member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName(ParameterInfo parameter, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameImpl(parameter, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName<T>(ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameImpl(typeof(T), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName(Type type, string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (type is null)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = type.GetMembers();
            return GetDisplayNameImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName<T>(string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = typeof(T).GetMembers();
            return GetDisplayNameImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayName<T>(Expression<Func<T, object>> expression, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            var member = expression.Body as MemberExpression;
            if (member is null
                && expression.Body.NodeType == ExpressionType.Convert
                && expression.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;
            return GetDisplayNameImpl(member?.Member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        #endregion

        #region GetDescriptionOr

        /// <summary>
        /// 获取描述 的实现方式
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射消岐选项</param>
        private static string GetDescriptionOrImpl(MemberInfo member, string defaultVal, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions) =>
            member is not null && IsDescriptionDefined(member, refOptions)
                ? GetDescriptionImpl(member, refOptions, ambOptions)
                : defaultVal;

        /// <summary>
        /// 获取描述 的实现方式
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射消岐选项</param>
        private static string GetDescriptionOrImpl(ParameterInfo parameter, string defaultVal, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions) =>
            parameter is not null && IsDescriptionDefined(parameter, refOptions)
                ? GetDescriptionImpl(parameter, refOptions, ambOptions)
                : defaultVal;

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr(MemberInfo member, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrImpl(member, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr(ParameterInfo parameter, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrImpl(parameter, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr<T>(string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrImpl(typeof(T), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr(Type type, string memberName, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (type is null)
                return defaultVal;
            if (string.IsNullOrWhiteSpace(memberName))
                return defaultVal;
            var members = type.GetMembers();
            return GetDescriptionOrImpl(members.FirstOrDefault(x => x.Name == memberName), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="memberName">成员名称</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr<T>(string memberName, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return defaultVal;
            var members = typeof(T).GetMembers();
            return GetDescriptionOrImpl(members.FirstOrDefault(x => x.Name == memberName), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述，或返回默认值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOr<T>(Expression<Func<T, object>> expression, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            var member = expression.Body as MemberExpression;
            if (member is null
                && expression.Body.NodeType == ExpressionType.Convert
                && expression.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;
            return GetDescriptionOrImpl(member?.Member, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        #endregion

        #region GetDisplayNameOr

        /// <summary>
        /// 获取显示名称 的实现方式
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDisplayNameOrImpl(MemberInfo member, string defaultVal, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (member is null)
                return defaultVal;
            if (GetAttribute(member, typeof(DisplayNameAttribute), refOptions, ambOptions) is DisplayNameAttribute displayNameAttribute)
                return displayNameAttribute.DisplayName;
            if (GetAttribute(member, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Name;
            return defaultVal;
        }

        /// <summary>
        /// 获取显示名称 的实现方式
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射歧义选项</param>
        private static string GetDisplayNameOrImpl(ParameterInfo parameter, string defaultVal, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (parameter is null)
                return defaultVal;
            if (GetAttribute(parameter, typeof(DisplayNameAttribute), refOptions, ambOptions) is DisplayNameAttribute displayNameAttribute)
                return displayNameAttribute.DisplayName;
            if (GetAttribute(parameter, typeof(DisplayAttribute), refOptions, ambOptions) is DisplayAttribute displayAttribute)
                return displayAttribute.Name;
            return defaultVal;
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr(MemberInfo member, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameOrImpl(member, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr(ParameterInfo parameter, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameOrImpl(parameter, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr<T>(string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDisplayNameOrImpl(typeof(T), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr(Type type, string memberName, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (type is null)
                return defaultVal;
            if (string.IsNullOrWhiteSpace(memberName))
                return defaultVal;
            var members = type.GetMembers();
            return GetDisplayNameOrImpl(members.FirstOrDefault(x => x.Name == memberName), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="memberName">成员名称</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr<T>(string memberName, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return defaultVal;
            var members = typeof(T).GetMembers();
            return GetDisplayNameOrImpl(members.FirstOrDefault(x => x.Name == memberName), defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <param name="defaultVal">默认值</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDisplayNameOr<T>(Expression<Func<T, object>> expression, string defaultVal, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            var member = expression.Body as MemberExpression;
            if (member is null
                && expression.Body.NodeType == ExpressionType.Convert
                && expression.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;
            return GetDisplayNameOrImpl(member?.Member, defaultVal, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        #endregion

        #region GetDescriptionOrDisplayName

        /// <summary>
        /// 获取描述或显示名称 的实现方式
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射消岐选项</param>
        private static string GetDescriptionOrDisplayNameImpl(MemberInfo member, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (member is null)
                return string.Empty;
            return IsDescriptionDefined(member, refOptions)
                ? GetDescriptionImpl(member, refOptions, ambOptions)
                : GetDisplayNameImpl(member, refOptions, ambOptions);
        }

        /// <summary>
        /// 获取描述或显示名称 的实现方式
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        /// <param name="ambOptions">反射消岐选项</param>
        private static string GetDescriptionOrDisplayNameImpl(ParameterInfo parameter, ReflectionOptions refOptions, ReflectionAmbiguousOptions ambOptions)
        {
            if (parameter is null)
                return string.Empty;
            return IsDescriptionDefined(parameter, refOptions)
                ? GetDescriptionImpl(parameter, refOptions, ambOptions)
                : GetDisplayNameImpl(parameter, refOptions, ambOptions);
        }

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <param name="member">成员元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName(MemberInfo member, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrDisplayNameImpl(member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <param name="parameter">参数元数据</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName(ParameterInfo parameter, ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrDisplayNameImpl(parameter, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName<T>(ReflectionOptions refOptions = ReflectionOptions.Default) =>
            GetDescriptionOrDisplayNameImpl(typeof(T), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName(Type type, string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (type is null)
                return string.Empty;
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = type.GetMembers();
            return GetDescriptionOrDisplayNameImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="memberName">成员名称</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName<T>(string memberName, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return string.Empty;
            var members = typeof(T).GetMembers();
            return GetDescriptionOrDisplayNameImpl(members.FirstOrDefault(x => x.Name == memberName), refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        /// <summary>
        /// 获取描述或显示名称
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <param name="refOptions">反射选项</param>
        public static string GetDescriptionOrDisplayName<T>(Expression<Func<T, object>> expression, ReflectionOptions refOptions = ReflectionOptions.Default)
        {
            var member = expression.Body as MemberExpression;
            if (member is null
                && expression.Body.NodeType == ExpressionType.Convert
                && expression.Body is UnaryExpression unary)
                member = unary.Operand as MemberExpression;
            return GetDescriptionOrDisplayNameImpl(member?.Member, refOptions, ReflectionAmbiguousOptions.IgnoreAmbiguous);
        }

        #endregion
    }
}
