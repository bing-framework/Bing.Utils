using System;
using System.Reflection;
using System.Text;

namespace Bing.Comments;

internal static class CsCommentId
{
    public static string Create(MemberInfo member)
    {
        member = GetDefinitionMember(member);
        if (member is Type type)
            return "T:" + GetTypeName(type, false);

        var declaringType = member.DeclaringType;
        if (declaringType == null)
            throw new NotSupportedException("当前成员没有声明类型");

        switch (member)
        {
            case ConstructorInfo constructor:
                return CreateMethodId(constructor, declaringType, constructor.IsStatic ? "#cctor" : "#ctor");
            case MethodInfo method:
                return CreateMethodId(method, declaringType, GetMethodName(method));
            case PropertyInfo property:
                return CreateMemberId("P", declaringType, property.Name, property.GetIndexParameters());
            case EventInfo @event:
                return CreateMemberId("E", declaringType, @event.Name, null);
            case FieldInfo field:
                return "F:" + GetTypeName(declaringType, false) + "." + EncodeMemberName(field.Name);
            default:
                throw new NotSupportedException("不支持当前类型的成员");
        }
    }

    internal static string GetTypeName(Type type)
    {
        return GetTypeName(type, true);
    }

    private static string CreateMethodId(MethodBase method, Type declaringType, string name)
    {
        var methodId = CreateMemberId("M", declaringType, name, method.GetParameters());
        if (method is MethodInfo methodInfo && IsConversionOperator(methodInfo))
            methodId += "~" + GetTypeName(methodInfo.ReturnType);
        return methodId;
    }

    private static string CreateMemberId(string prefix, Type declaringType, string name, ParameterInfo[] parameters)
    {
        var memberId = prefix + ":" + GetTypeName(declaringType, false) + "." + EncodeMemberName(name);
        if (parameters != null && parameters.Length > 0)
            memberId += "(" + string.Join(",", Array.ConvertAll(parameters, parameter => GetTypeName(parameter.ParameterType))) + ")";
        return memberId;
    }

    private static string GetMethodName(MethodInfo method)
    {
        var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        var name = EncodeMemberName(definition.Name);
        if (definition.IsGenericMethod)
            name += "``" + definition.GetGenericArguments().Length;
        return name;
    }

    private static MemberInfo GetDefinitionMember(MemberInfo member)
    {
        var declaringType = member.DeclaringType;
        if (declaringType == null || declaringType.IsGenericTypeDefinition || declaringType.IsGenericType == false)
            return member;

        var definitionType = declaringType.GetGenericTypeDefinition();
        foreach (var candidate in definitionType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                             BindingFlags.Instance | BindingFlags.Static |
                                                             BindingFlags.DeclaredOnly))
        {
            try
            {
                if (candidate.MetadataToken == member.MetadataToken)
                    return candidate;
            }
            catch (InvalidOperationException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }
        return FindUniqueDefinitionMember(member, definitionType);
    }

    private static MemberInfo FindUniqueDefinitionMember(MemberInfo member, Type definitionType)
    {
        MemberInfo match = null;
        foreach (var candidate in definitionType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                             BindingFlags.Instance | BindingFlags.Static |
                                                             BindingFlags.DeclaredOnly))
        {
            if (candidate.MemberType != member.MemberType ||
                string.Equals(candidate.Name, member.Name, StringComparison.Ordinal) == false ||
                HasMatchingParameterCount(candidate, member) == false)
                continue;
            if (match != null)
                return member;
            match = candidate;
        }
        return match ?? member;
    }

    private static bool HasMatchingParameterCount(MemberInfo candidate, MemberInfo member)
    {
        if (candidate is MethodBase candidateMethod && member is MethodBase method)
            return candidateMethod.GetParameters().Length == method.GetParameters().Length;
        if (candidate is PropertyInfo candidateProperty && member is PropertyInfo property)
            return candidateProperty.GetIndexParameters().Length == property.GetIndexParameters().Length;
        return candidate is FieldInfo || candidate is EventInfo;
    }

    private static bool IsConversionOperator(MethodInfo method)
    {
        return method.Name == "op_Implicit" || method.Name == "op_Explicit" ||
               method.Name == "op_CheckedImplicit" || method.Name == "op_CheckedExplicit";
    }

    private static string GetTypeName(Type type, bool includeConstructedArguments)
    {
        if (type == null)
            return string.Empty;

        if (type.IsByRef)
            return GetTypeName(type.GetElementType(), true) + "@";
        if (type.IsPointer)
            return GetTypeName(type.GetElementType(), true) + "*";
        if (type.IsArray)
            return GetArrayTypeName(type);
        if (type.IsGenericParameter)
            return (type.DeclaringMethod == null ? "`" : "``") + type.GenericParameterPosition;

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            if (includeConstructedArguments == false)
                return GetTypeName(type.GetGenericTypeDefinition(), false);
            return GetConstructedTypeName(type);
        }

        var name = type.FullName ?? type.Name;
        return name.Replace('+', '.');
    }

    private static string GetArrayTypeName(Type type)
    {
        var elementType = GetTypeName(type.GetElementType(), true);
        if (type.GetArrayRank() == 1)
            return elementType + (type == type.GetElementType().MakeArrayType() ? "[]" : "[*]");
        return elementType + "[" + new string(',', type.GetArrayRank() - 1).Replace(",", "0:,") + "0:]";
    }

    private static string GetConstructedTypeName(Type type)
    {
        var definition = type.GetGenericTypeDefinition();
        var definitionName = definition.FullName ?? definition.Name;
        var segments = definitionName.Split('+');
        var arguments = type.GetGenericArguments();
        var argumentIndex = 0;
        var builder = new StringBuilder(definitionName.Length + arguments.Length * 16);
        for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            if (segmentIndex > 0)
                builder.Append('.');

            var segment = segments[segmentIndex];
            var arityMarker = segment.LastIndexOf('`');
            var arity = 0;
            if (arityMarker >= 0 && int.TryParse(segment.Substring(arityMarker + 1), out arity))
                segment = segment.Substring(0, arityMarker);
            builder.Append(segment);
            if (arity == 0)
                continue;

            builder.Append('{');
            for (var index = 0; index < arity; index++)
            {
                if (index > 0)
                    builder.Append(',');
                builder.Append(GetTypeName(arguments[argumentIndex++], true));
            }
            builder.Append('}');
        }
        return builder.ToString();
    }

    private static string EncodeMemberName(string name)
    {
        return name.Replace('<', '{').Replace('>', '}').Replace('.', '#');
    }
}