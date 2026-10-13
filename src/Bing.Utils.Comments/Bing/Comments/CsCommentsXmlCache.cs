using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Bing.Comments;

/// <summary>
/// 提供按程序集和 XML 文件路径缓存文档注释的功能。
/// </summary>
public static class CsCommentsXmlCache
{
    /// <summary>
    /// 按程序集完整名称保存 XML 文档的缓存字典。
    /// </summary>
    private static readonly Dictionary<string, XmlDocument> _cache = new();

    /// <summary>
    /// 按 XML 文件路径保存文档的缓存字典，路径比较不区分大小写。
    /// </summary>
    private static readonly Dictionary<string, XmlDocument> _cacheByPath = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取成员所属程序集的 XML 文档；找不到对应文件时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="member">要读取文档注释的反射成员。</param>
    /// <returns>程序集 XML 文档；程序集旁及应用程序 <c>bin</c> 目录均无对应文件时为 <see langword="null"/>。</returns>
    public static XmlDocument Get(MemberInfo member)
    {
        var key = member.Module.Assembly.FullName;
        XmlDocument xml;

        if (_cache.TryGetValue(key, out xml))
        {
            return xml;
        }

        lock (_cache)
        {
            if (_cache.TryGetValue(key, out xml))
            {
                return xml;
            }

            var xmlFile = GetXmlPath(member.Module.FullyQualifiedName);
            if (xmlFile == null)
            {
                return null;
            }

            var xmlText = File.ReadAllText(xmlFile);
            xml = new XmlDocument();
            xml.LoadXml(xmlText);
            _cache.Add(key, xml);
            _cacheByPath[xmlFile] = xml;
            return xml;
        }
    }

    /// <summary>
    /// 将程序集对应的 XML 文档加载到缓存；路径为空或文件不存在时缓存空文档。
    /// </summary>
    /// <param name="assembly">作为缓存键来源的程序集。</param>
    /// <param name="xmlPath">XML 文档路径；为空或文件不存在时使用空文档。</param>
    internal static void Set(Assembly assembly, string xmlPath)
    {
        var key = assembly.FullName;
        lock (_cache)
        {
            if (xmlPath == null || File.Exists(xmlPath) == false)
            {
                _cacheByPath[xmlPath] = _cache[key] = new XmlDocument();
            }
            else
            {
                var xmlText = File.ReadAllText(xmlPath);
                var xml = new XmlDocument();

                xml.LoadXml(xmlText);
                _cache[key] = xml;
                _cacheByPath[xmlPath] = xml;
            }
        }
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

        lock (_cache)
        {
            XmlDocument xml;
            if (_cacheByPath.TryGetValue(xmlPath, out xml) == false)
            {
                return;
            }

            var xmlText = File.ReadAllText(xmlPath);
            xml.LoadXml(xmlText);
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

        var xmlFile = dllPath.Remove(dllPath.Length - Path.GetExtension(dllPath).Length) + ".xml";
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
}
