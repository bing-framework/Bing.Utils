using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace Bing.Comments;

internal static class CsCommentsInheritanceResolver
{
    public static XmlNode Resolve(MemberInfo member, string memberId, XmlNode node)
    {
        var stack = new HashSet<VisitKey>();
        return ResolveNode(member, member.Module.Assembly, memberId, node, stack, null);
    }

    private static XmlNode ResolveNode(MemberInfo member, Assembly assembly, string memberId, XmlNode node,
        HashSet<VisitKey> stack, string path)
    {
        var key = new VisitKey(assembly, memberId, path);
        if (stack.Add(key) == false)
            return node;

        try
        {
            var inheritdocNodes = GetDirectInheritdocNodes(node);
            var inherited = new List<XmlNode>();
            foreach (var inheritdoc in inheritdocNodes)
                inherited.AddRange(ResolveFragments(member, assembly, memberId, inheritdoc, stack));

            if (inheritdocNodes.Count == 0 && ContainsInlineInheritdoc(node) == false)
                return node;

            var document = new XmlDocument { PreserveWhitespace = true };
            var result = document.CreateElement(node.Name);
            if (node.Attributes != null)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                    result.SetAttribute(attribute.Name, attribute.Value);
            }
            document.AppendChild(result);

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element || child.Name == "inheritdoc")
                    continue;
                result.AppendChild(CloneWithInlineInheritance(document, child, member, assembly, memberId, stack));
            }

            foreach (var item in inherited)
            {
                if (ShouldAddInherited(result, item))
                    result.AppendChild(document.ImportNode(item, true));
            }

            if (inheritdocNodes.Count > 0 && inherited.Count == 0)
            {
                foreach (var inheritdoc in inheritdocNodes)
                    result.AppendChild(document.ImportNode(inheritdoc, true));
            }

            return result;
        }
        finally
        {
            stack.Remove(key);
        }
    }

    private static XmlNode CloneWithInlineInheritance(XmlDocument document, XmlNode source, MemberInfo member,
        Assembly assembly, string memberId, HashSet<VisitKey> stack)
    {
        var clone = document.ImportNode(source, true);
        ReplaceInlineInheritdoc(clone, member, assembly, memberId, stack);
        return clone;
    }

    private static void ReplaceInlineInheritdoc(XmlNode node, MemberInfo member, Assembly assembly, string memberId,
        HashSet<VisitKey> stack)
    {
        var children = new List<XmlNode>();
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
                children.Add(child);
        }

        foreach (var child in children)
        {
            if (child.Name == "inheritdoc")
            {
                var fragments = ResolveFragments(member, assembly, memberId, child, stack);
                var replacement = GetInlineReplacement(node.Name, fragments);
                if (replacement.Count == 0)
                    continue;

                foreach (var item in replacement)
                    node.InsertBefore(node.OwnerDocument.ImportNode(item, true), child);
                node.RemoveChild(child);
            }
            else
            {
                ReplaceInlineInheritdoc(child, member, assembly, memberId, stack);
            }
        }
    }

    private static List<XmlNode> GetInlineReplacement(string parentName, List<XmlNode> fragments)
    {
        var matching = new List<XmlNode>();
        foreach (var fragment in fragments)
        {
            if (fragment.Name == parentName)
            {
                foreach (XmlNode child in fragment.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element || child.NodeType == XmlNodeType.Text ||
                        child.NodeType == XmlNodeType.CDATA)
                        matching.Add(child);
                }
            }
        }
        return matching;
    }

    private static List<XmlNode> ResolveFragments(MemberInfo member, Assembly assembly, string memberId,
        XmlNode inheritdoc, HashSet<VisitKey> stack)
    {
        var sources = GetSources(member, assembly, memberId, inheritdoc);
        var result = new List<XmlNode>();
        var path = inheritdoc.Attributes?["path"]?.Value;
        foreach (var source in sources)
        {
            var resolved = ResolveNode(source.Member, source.Assembly, source.MemberId, source.Node, stack, path);
            if (string.IsNullOrEmpty(path))
            {
                foreach (XmlNode child in resolved.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                        result.Add(child);
                }
                continue;
            }

            try
            {
                foreach (XmlNode selected in SelectPath(resolved, path))
                {
                    if (selected.NodeType == XmlNodeType.Element)
                        result.Add(selected);
                }
            }
            catch (XPathException)
            {
            }
        }
        return result;
    }

    private static XmlNodeList SelectPath(XmlNode node, string path)
    {
        return node.SelectNodes(path.Replace("/member", "."));
    }

    private static List<Source> GetSources(MemberInfo member, Assembly assembly, string memberId, XmlNode inheritdoc)
    {
        var cref = inheritdoc.Attributes?["cref"]?.Value;
        if (!string.IsNullOrEmpty(cref))
        {
            var explicitSource = FindExplicitSource(assembly, cref);
            return explicitSource == null ? new List<Source>() : new List<Source> { explicitSource };
        }

        var result = new List<Source>();
        foreach (var candidate in GetImplicitMembers(member))
        {
            var candidateId = TryCreateId(candidate);
            if (candidateId == null)
                continue;
            var node = CsCommentsXmlCache.GetMember(candidate, candidateId);
            if (node != null)
                result.Add(new Source(candidate, candidate.Module.Assembly, candidateId, node));
        }

        if (result.Count == 0 && member?.DeclaringType != null && member is not Type)
            result.AddRange(GetTypeInheritedMembers(member, member.DeclaringType));
        return result;
    }

    private static IEnumerable<Source> GetTypeInheritedMembers(MemberInfo member, Type declaringType)
    {
        var typeId = TryCreateId(declaringType);
        if (typeId == null)
            yield break;

        var typeNode = CsCommentsXmlCache.GetMember(declaringType, typeId);
        if (typeNode == null || GetDirectInheritdocNodes(typeNode).Count == 0)
            yield break;

        foreach (var source in GetSources(declaringType, declaringType.Assembly, typeId, typeNode))
        {
            if (source.Member is not Type sourceType)
                continue;

            var sourceMember = FindCorrespondingMember(sourceType, member);
            var sourceMemberId = TryCreateId(sourceMember);
            if (sourceMember == null || sourceMemberId == null)
                continue;

            var sourceNode = CsCommentsXmlCache.GetMember(sourceMember, sourceMemberId);
            if (sourceNode != null)
                yield return new Source(sourceMember, sourceMember.Module.Assembly, sourceMemberId, sourceNode);
        }
    }

    private static MemberInfo FindCorrespondingMember(Type sourceType, MemberInfo member)
    {
        var members = sourceType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static |
                                            BindingFlags.DeclaredOnly);
        foreach (var candidate in members)
        {
            if (candidate.MemberType != member.MemberType ||
                string.Equals(candidate.Name, member.Name, StringComparison.Ordinal) == false)
                continue;
            if (candidate is MethodBase candidateMethod && member is MethodBase method &&
                AreMethodSignaturesEqual(candidateMethod, method))
                return candidate;
            if (candidate is PropertyInfo candidateProperty && member is PropertyInfo property &&
                AreParameterListsEqual(candidateProperty.GetIndexParameters(), property.GetIndexParameters()))
                return candidate;
            if (candidate is EventInfo or FieldInfo)
                return candidate;
        }
        return null;
    }

    private static bool AreMethodSignaturesEqual(MethodBase left, MethodBase right)
    {
        if (left.IsGenericMethod != right.IsGenericMethod ||
            (left.IsGenericMethod && left.GetGenericArguments().Length != right.GetGenericArguments().Length))
            return false;
        return AreParameterListsEqual(left.GetParameters(), right.GetParameters());
    }

    private static bool AreParameterListsEqual(ParameterInfo[] left, ParameterInfo[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (var index = 0; index < left.Length; index++)
        {
            if (CsCommentId.GetTypeName(left[index].ParameterType) !=
                CsCommentId.GetTypeName(right[index].ParameterType))
                return false;
        }
        return true;
    }

    private static Source FindExplicitSource(Assembly currentAssembly, string memberId)
    {
        var assemblies = new List<Assembly> { currentAssembly };
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assemblies.Contains(assembly) == false)
                assemblies.Add(assembly);
        }

        foreach (var assembly in assemblies)
        {
            var node = CsCommentsXmlCache.GetMember(assembly, memberId);
            if (node == null)
                continue;
            var member = FindMember(assembly, memberId);
            if (member == null)
                continue;
            return new Source(member, assembly, memberId, node);
        }
        return null;
    }

    private static IEnumerable<MemberInfo> GetImplicitMembers(MemberInfo member)
    {
        if (member is Type type)
        {
            if (type.BaseType != null)
                yield return type.BaseType;
            foreach (var @interface in type.GetInterfaces())
                yield return @interface;
            yield break;
        }

        if (member is MethodInfo method)
        {
            var definition = method.GetBaseDefinition();
            if (definition != method)
                yield return definition;
            foreach (var item in GetInterfaceMethods(method))
                yield return item;
            yield break;
        }

        if (member is PropertyInfo property)
        {
            var baseProperty = GetBaseProperty(property);
            if (baseProperty != null)
                yield return baseProperty;
            foreach (var item in GetInterfaceProperties(property))
                yield return item;
            yield break;
        }

        if (member is EventInfo @event)
        {
            var baseEvent = GetBaseEvent(@event);
            if (baseEvent != null)
                yield return baseEvent;
            foreach (var item in GetInterfaceEvents(@event))
                yield return item;
        }
    }

    private static PropertyInfo GetBaseProperty(PropertyInfo property)
    {
        var accessor = property.GetMethod ?? property.SetMethod;
        var baseAccessor = accessor?.GetBaseDefinition();
        if (baseAccessor == null || SameMember(accessor, baseAccessor))
            return null;

        var declaringType = baseAccessor.DeclaringType;
        if (declaringType == null)
            return null;
        foreach (var candidate in declaringType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                              BindingFlags.Instance | BindingFlags.Static |
                                                              BindingFlags.DeclaredOnly))
        {
            if (SameMember(candidate.GetMethod, baseAccessor) || SameMember(candidate.SetMethod, baseAccessor))
                return candidate;
        }
        return null;
    }

    private static EventInfo GetBaseEvent(EventInfo @event)
    {
        var accessor = @event.AddMethod ?? @event.RemoveMethod;
        var baseAccessor = accessor?.GetBaseDefinition();
        if (baseAccessor == null || SameMember(accessor, baseAccessor))
            return null;

        var declaringType = baseAccessor.DeclaringType;
        if (declaringType == null)
            return null;
        foreach (var candidate in declaringType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Instance | BindingFlags.Static |
                                                           BindingFlags.DeclaredOnly))
        {
            if (SameMember(candidate.AddMethod, baseAccessor) || SameMember(candidate.RemoveMethod, baseAccessor))
                return candidate;
        }
        return null;
    }

    private static IEnumerable<MethodInfo> GetInterfaceMethods(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
            yield break;
        foreach (var @interface in declaringType.GetInterfaces())
        {
            InterfaceMapping map;
            try
            {
                map = declaringType.GetInterfaceMap(@interface);
            }
            catch (ArgumentException)
            {
                continue;
            }

            for (var index = 0; index < map.TargetMethods.Length; index++)
            {
                if (SameMember(map.TargetMethods[index], method))
                    yield return map.InterfaceMethods[index];
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetInterfaceProperties(PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null)
            yield break;
        foreach (var @interface in declaringType.GetInterfaces())
        {
            InterfaceMapping map;
            try
            {
                map = declaringType.GetInterfaceMap(@interface);
            }
            catch (ArgumentException)
            {
                continue;
            }

            foreach (var candidate in @interface.GetProperties())
            {
                if (SameAccessor(map, candidate.GetMethod, property.GetMethod) ||
                    SameAccessor(map, candidate.SetMethod, property.SetMethod))
                    yield return candidate;
            }
        }
    }

    private static IEnumerable<EventInfo> GetInterfaceEvents(EventInfo @event)
    {
        var declaringType = @event.DeclaringType;
        if (declaringType == null)
            yield break;
        foreach (var @interface in declaringType.GetInterfaces())
        {
            InterfaceMapping map;
            try
            {
                map = declaringType.GetInterfaceMap(@interface);
            }
            catch (ArgumentException)
            {
                continue;
            }

            foreach (var candidate in @interface.GetEvents())
            {
                if (SameAccessor(map, candidate.AddMethod, @event.AddMethod) ||
                    SameAccessor(map, candidate.RemoveMethod, @event.RemoveMethod))
                    yield return candidate;
            }
        }
    }

    private static bool SameAccessor(InterfaceMapping map, MethodInfo expected, MethodInfo actual)
    {
        if (expected == null || actual == null)
            return false;
        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            if (SameMember(map.InterfaceMethods[index], expected) && SameMember(map.TargetMethods[index], actual))
                return true;
        }
        return false;
    }

    private static bool SameMember(MemberInfo left, MemberInfo right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null)
            return false;
        try
        {
            return left.Module == right.Module && left.MetadataToken == right.MetadataToken;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static MemberInfo FindMember(Assembly assembly, string memberId)
    {
        foreach (var type in GetTypes(assembly))
        {
            var typeId = TryCreateId(type);
            if (typeId == memberId)
                return type;
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (TryCreateId(member) == memberId)
                    return member;
            }
        }
        return null;
    }

    private static IEnumerable<Type> GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var result = new List<Type>();
            foreach (var type in exception.Types)
            {
                if (type != null)
                    result.Add(type);
            }
            return result;
        }
    }

    private static string TryCreateId(MemberInfo member)
    {
        if (member == null)
            return null;
        try
        {
            return CsCommentId.Create(member);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static List<XmlNode> GetDirectInheritdocNodes(XmlNode node)
    {
        var result = new List<XmlNode>();
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.Name == "inheritdoc")
                result.Add(child);
        }
        return result;
    }

    private static bool ContainsInlineInheritdoc(XmlNode node)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;
            if (child.Name == "inheritdoc" || ContainsInlineInheritdoc(child))
                return true;
        }
        return false;
    }

    private static bool ShouldAddInherited(XmlNode result, XmlNode inherited)
    {
        var name = inherited.Name;
        foreach (XmlNode child in result.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element || child.Name != name)
                continue;
            if (name == "param" || name == "typeparam")
            {
                var currentName = child.Attributes?["name"]?.Value;
                var inheritedName = inherited.Attributes?["name"]?.Value;
                if (string.Equals(currentName, inheritedName, StringComparison.Ordinal))
                    return false;
                continue;
            }
            if (name == "exception")
            {
                var currentCref = child.Attributes?["cref"]?.Value;
                var inheritedCref = inherited.Attributes?["cref"]?.Value;
                if (string.Equals(currentCref, inheritedCref, StringComparison.Ordinal))
                    return false;
                continue;
            }
            if (name == "summary" || name == "remarks" || name == "returns" || name == "value")
                return false;
        }
        return true;
    }

    private readonly struct VisitKey : IEquatable<VisitKey>
    {
        public VisitKey(Assembly assembly, string memberId, string path)
        {
            Assembly = assembly;
            MemberId = memberId;
            Path = path;
        }

        private Assembly Assembly { get; }

        private string MemberId { get; }

        private string Path { get; }

        public bool Equals(VisitKey other)
        {
            return ReferenceEquals(Assembly, other.Assembly) &&
                   string.Equals(MemberId, other.MemberId, StringComparison.Ordinal) &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is VisitKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Assembly);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(MemberId ?? string.Empty);
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path ?? string.Empty);
            return hash;
        }
    }

    private sealed class Source
    {
        public Source(MemberInfo member, Assembly assembly, string memberId, XmlNode node)
        {
            Member = member;
            Assembly = assembly;
            MemberId = memberId;
            Node = node;
        }

        public MemberInfo Member { get; }

        public Assembly Assembly { get; }

        public string MemberId { get; }

        public XmlNode Node { get; }
    }
}