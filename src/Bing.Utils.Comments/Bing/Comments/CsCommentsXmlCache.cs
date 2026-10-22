using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 提供按程序集和 XML 文件路径缓存文档注释的功能。
/// </summary>
public static class CsCommentsXmlCache
{
    internal const long MaximumDocumentCharacters = 64L * 1024 * 1024;

    private static readonly object _syncRoot = new();

    private static readonly ConditionalWeakTable<Assembly, CacheItem> _cache = new();

    private static readonly List<WeakReference<Assembly>> _assemblyReferences = new();

    /// <summary>
    /// 获取成员所属程序集的 XML 文档；找不到对应文件时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="member">要读取文档注释的反射成员。</param>
    /// <returns>XML 文档的防御性副本；找不到对应 XML 文件时返回 null。</returns>
    public static XmlDocument Get(MemberInfo member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        var item = GetOrLoad(member.Module.Assembly, member.Module.FullyQualifiedName);
        return item == null ? null : (XmlDocument)item.Document.CloneNode(true);
    }

    internal static XmlNode GetMember(MemberInfo member, string memberId)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));
        return GetMember(member.Module.Assembly, memberId);
    }

    internal static XmlNode GetMember(Assembly assembly, string memberId)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        if (string.IsNullOrEmpty(memberId))
            return null;

        var item = GetOrLoad(assembly, assembly.ManifestModule.FullyQualifiedName);
        return item != null && item.Members.TryGetValue(memberId, out var node) ? node : null;
    }

    /// <summary>
    /// 将程序集对应的 XML 文档加载到缓存；路径为空或文件不存在时移除该程序集的缓存。
    /// </summary>
    /// <param name="assembly">作为缓存键来源的程序集。</param>
    /// <param name="xmlPath">XML 文档路径；为空或文件不存在时移除已有缓存。</param>
    internal static void Set(Assembly assembly, string xmlPath)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        if (string.IsNullOrEmpty(xmlPath) || File.Exists(xmlPath) == false)
        {
            lock (_syncRoot)
                RemoveAssembly(assembly);
            return;
        }

        var path = NormalizePath(xmlPath);
        var item = CreateCacheItem(path);
        lock (_syncRoot)
            ReplaceAssembly(assembly, item);
    }

    /// <summary>
    /// 从指定路径重新加载已缓存的 XML 文档；路径为空或未缓存时不执行操作。
    /// </summary>
    /// <param name="xmlPath">要重新加载的 XML 文档路径。</param>
    internal static void Reset(string xmlPath)
    {
        if (xmlPath == null)
        {
            return;
        }

        var path = NormalizePath(xmlPath);
        var targets = new List<KeyValuePair<Assembly, CacheItem>>();
        lock (_syncRoot)
        {
            for (var index = _assemblyReferences.Count - 1; index >= 0; index--)
            {
                if (_assemblyReferences[index].TryGetTarget(out var assembly) == false)
                {
                    _assemblyReferences.RemoveAt(index);
                    continue;
                }

                if (_cache.TryGetValue(assembly, out var item) &&
                    string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
                    targets.Add(new KeyValuePair<Assembly, CacheItem>(assembly, item));
            }
            if (targets.Count == 0)
                return;
        }

        var newItem = CreateCacheItem(path);
        lock (_syncRoot)
        {
            foreach (var target in targets)
            {
                if (_cache.TryGetValue(target.Key, out var currentItem) &&
                    ReferenceEquals(currentItem, target.Value))
                    ReplaceAssembly(target.Key, newItem);
            }
        }
    }

    /// <summary>
    /// 获取程序集 XML 文档路径；找不到文档或路径无法映射到文件时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="dllPath">程序集文件的完整路径。</param>
    /// <returns>程序集旁的 XML 文档路径，或应用程序 <c>bin</c> 目录中的同名文档路径；均不存在时为 <see langword="null"/>。</returns>
    internal static string GetXmlPath(string dllPath)
    {
        if (string.IsNullOrEmpty(dllPath) || dllPath[0] == '<')
        {
            return null;
        }

        var extension = Path.GetExtension(dllPath);
        if (string.IsNullOrEmpty(extension))
            return null;
        var xmlFile = dllPath.Remove(dllPath.Length - extension.Length) + ".xml";
        if (File.Exists(xmlFile) == false)
        {
            xmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin",
                Path.GetFileNameWithoutExtension(dllPath) + ".xml");
            if (File.Exists(xmlFile) == false)
            {
                return null;
            }
        }

        return xmlFile;
    }
    private static CacheItem GetOrLoad(Assembly assembly, string dllPath)
    {
        string path;
        CacheItem cached = null;
        lock (_syncRoot)
        {
            if (_cache.TryGetValue(assembly, out cached))
            {
                if (IsCurrent(cached))
                    return cached;
                path = cached.Path;
            }
            else
            {
                path = GetXmlPath(dllPath);
                if (path == null)
                    return null;

                path = NormalizePath(path);
            }
        }

        CacheItem item;
        try
        {
            item = CreateCacheItem(path);
        }
        catch (XmlException) when (cached != null)
        {
            return cached;
        }
        catch (IOException) when (cached != null)
        {
            return cached;
        }
        catch (UnauthorizedAccessException) when (cached != null)
        {
            return cached;
        }

        lock (_syncRoot)
        {
            if (_cache.TryGetValue(assembly, out var current))
            {
                if (ReferenceEquals(current, cached) == false && IsCurrent(current))
                    return current;
            }

            ReplaceAssembly(assembly, item);
            return item;
        }
    }

    private static bool IsCurrent(CacheItem item)
    {
        var fileInfo = new FileInfo(item.Path);
        return fileInfo.Exists && fileInfo.Length == item.Length &&
               fileInfo.LastWriteTimeUtc == item.LastWriteTimeUtc;
    }

    private static CacheItem CreateCacheItem(string path)
    {
        var document = LoadDocument(path);
        var fileInfo = new FileInfo(path);
        var members = new Dictionary<string, XmlNode>(StringComparer.Ordinal);
        var nodes = document.SelectNodes("/doc/members/member");
        foreach (XmlNode node in nodes)
        {
            var name = node.Attributes?["name"]?.Value;
            if (!string.IsNullOrEmpty(name) && members.ContainsKey(name) == false)
                members.Add(name, node);
        }

        return new CacheItem(path, document, members, fileInfo.Length, fileInfo.LastWriteTimeUtc);
    }

    private static XmlDocument LoadDocument(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = false,
            IgnoreWhitespace = false,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = MaximumDocumentCharacters,
            XmlResolver = null
        };

        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };
        using (var stream = File.OpenRead(path))
        using (var reader = XmlReader.Create(stream, settings))
        {
            document.Load(reader);
        }

        return document;
    }

    private static void ReplaceAssembly(Assembly assembly, CacheItem item)
    {
        _cache.Remove(assembly);
        _cache.Add(assembly, item);
        TrackAssembly(assembly);
    }

    private static void RemoveAssembly(Assembly assembly)
    {
        _cache.Remove(assembly);
    }

    private static void TrackAssembly(Assembly assembly)
    {
        for (var index = _assemblyReferences.Count - 1; index >= 0; index--)
        {
            if (_assemblyReferences[index].TryGetTarget(out var target) == false)
            {
                _assemblyReferences.RemoveAt(index);
                continue;
            }

            if (ReferenceEquals(target, assembly))
                return;
        }

        _assemblyReferences.Add(new WeakReference<Assembly>(assembly));
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private sealed class CacheItem
    {
        public CacheItem(string path, XmlDocument document, Dictionary<string, XmlNode> members,
            long length, DateTime lastWriteTimeUtc)
        {
            Path = path;
            Document = document;
            Members = members;
            Length = length;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string Path { get; }

        public XmlDocument Document { get; }

        public Dictionary<string, XmlNode> Members { get; }

        public long Length { get; }

        public DateTime LastWriteTimeUtc { get; }
    }
}
