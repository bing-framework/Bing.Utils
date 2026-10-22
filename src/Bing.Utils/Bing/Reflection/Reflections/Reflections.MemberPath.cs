using Bing.Helpers;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

/// <summary>
/// 反射操作。
/// </summary>
public static partial class Reflections
{
    internal const int MemberPathCacheCapacity = 1024;
    private static int _memberPathResolveCount;
    private const int MaxMemberPathLength = 4096;
    private const int MaxMemberPathDepth = 64;
    internal static int MemberPathResolveCount => System.Threading.Volatile.Read(ref _memberPathResolveCount);
    private static readonly MemberPathOptions DefaultMemberPathOptions = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Type, MemberPathLookup> MemberPathCache = new();

    private sealed class MemberPathLookup
    {
        internal readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<MemberInfo[]>> Paths = new();
        internal readonly object AdmissionGate = new();
        internal MemberPathFastPath FastPath;
    }

    private sealed class MemberPathFastPath
    {
        private readonly string _path;
        private readonly bool _ignoreCase;
        private readonly bool _includeFields;
        private readonly bool _includeNonPublic;
        internal readonly MemberInfo[] Members;

        internal MemberPathFastPath(string path, bool ignoreCase, bool includeFields, bool includeNonPublic, MemberInfo[] members)
        {
            _path = path;
            _ignoreCase = ignoreCase;
            _includeFields = includeFields;
            _includeNonPublic = includeNonPublic;
            Members = members;
        }

        internal bool Matches(string path, bool ignoreCase, bool includeFields, bool includeNonPublic) =>
            _ignoreCase == ignoreCase && _includeFields == includeFields && _includeNonPublic == includeNonPublic
            && string.Equals(_path, path, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    /// <summary>
    /// 判断类型是否包含可访问的成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>路径中的每个成员均存在且可访问时返回 true；否则返回 false。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 null 时抛出。</exception>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool HasMemberPath(Type type, string memberPath, MemberPathOptions options = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        options = SnapshotOptions(options);
        if (!TryResolveMemberPath(type, memberPath, options, out var members))
            return false;
        for (var index = 0; index < members.Count - 1; index++)
        {
            if (!CanRead(members[index], options))
                return false;
        }
        return members.Count > 0 && (CanRead(members[members.Count - 1], options)
                                      || CanWrite(members[members.Count - 1], options));
    }

    /// <summary>
    /// 尝试获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">成功时为路径的值；失败时为 null。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功读取路径值时返回 true；路径不存在、中间值为 null 或成员不可读时返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TryGetMemberValueByPath(object instance, string memberPath, out object value, MemberPathOptions options = null)
    {
        return TryGetMemberValueByPath(instance, instance?.GetType(), memberPath, out value, options);
    }

    /// <summary>
    /// 按显式根类型尝试获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="rootType">路径解析的根类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="value">成功时为路径的值；失败时为 null。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功读取路径值时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按显式根类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    internal static bool TryGetMemberValueByPath(object instance, Type rootType, string memberPath,
        out object value, MemberPathOptions options = null)
    {
        if (instance == null)
        {
            value = null;
            return false;
        }
        if (rootType == null)
        {
            value = null;
            return false;
        }
        options = SnapshotOptions(options);
        if (!TryResolveMemberPath(rootType, memberPath, options, out var members))
        {
            value = null;
            return false;
        }

        object current = instance;
        foreach (var member in members)
        {
            if (current == null || !CanRead(member, options))
            {
                value = null;
                return false;
            }

            current = GetMemberValue(member, current);
        }

        value = current;
        return true;
    }

    /// <summary>
    /// 获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员路径的值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当路径不存在、成员不可读或中间值为 null 时抛出。</exception>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static object GetMemberValueByPath(object instance, string memberPath, MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        EnsureMemberPathLimits(instance.GetType(), memberPath);
        if (TryGetMemberValueByPath(instance, instance.GetType(), memberPath, out var value, options))
            return value;
        throw new InvalidOperationException($"无法读取对象类型“{instance.GetType().FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 按显式根类型获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="rootType">路径解析的根类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员路径的值。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按显式根类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    internal static object GetMemberValueByPath(object instance, Type rootType, string memberPath,
        MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (rootType == null)
            throw new ArgumentNullException(nameof(rootType));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        EnsureMemberPathLimits(rootType, memberPath);
        if (TryGetMemberValueByPath(instance, rootType, memberPath, out var value, options))
            return value;
        throw new InvalidOperationException($"无法读取对象类型“{rootType.FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 尝试设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功设置路径值时返回 true；路径不存在、成员不可写、中间值为 null 或值不能转换时返回 false。</returns>
    /// <remarks>值类型变量请使用 <c>ref</c> 重载；此重载接收装箱对象，直接传入结构体变量不会回写原变量。
    /// 嵌套值类型逐层回写时，用户定义的 setter 抛异常可能留下已执行的副作用；写入不提供事务性回滚。</remarks>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TrySetMemberValueByPath(object instance, string memberPath, object value, MemberPathOptions options = null)
    {
        return TrySetMemberValueByPath(instance, instance?.GetType(), memberPath, value, options);
    }

    /// <summary>
    /// 按显式根类型尝试设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="rootType">路径解析的根类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功设置路径值时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按显式根类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    internal static bool TrySetMemberValueByPath(object instance, Type rootType, string memberPath, object value,
        MemberPathOptions options = null)
    {
        if (instance == null)
            return false;
        if (rootType == null)
            return false;
        options = SnapshotOptions(options);
        if (!TryResolveMemberPath(rootType, memberPath, options, out var members) || members.Count == 0)
            return false;

        // 仅当最终写入发生在值类型上时，连续的值类型父节点才需要回写。
        for (var index = members.Count - 2; index >= 0; index--)
        {
            if (!GetMemberType(members[index]).IsValueType)
                break;
            if (!CanWrite(members[index], options))
                return false;
        }

        object current = instance;
        var parents = members.Count > 1 ? new List<(object Owner, MemberInfo Member)>(members.Count - 1) : null;
        for (var index = 0; index < members.Count - 1; index++)
        {
            var member = members[index];
            if (!CanRead(member, options))
                return false;

            var owner = current;
            current = GetMemberValue(member, owner);

            if (current == null)
                return false;
            parents.Add((owner, member));
        }

        var finalMember = members[members.Count - 1];
        if (!CanWrite(finalMember, options)
            || !TryGetAssignedValue(value, GetMemberType(finalMember), options, out var convertedValue))
            return false;

        SetMemberValue(finalMember, current, convertedValue);
        for (var index = (parents?.Count ?? 0) - 1;
             index >= 0 && GetMemberType(parents[index].Member).IsValueType; index--)
        {
            var (owner, member) = parents[index];
            SetMemberValue(member, owner, current);
            current = owner;
        }
        return true;
    }

    /// <summary>
    /// 设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="instance"/> 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当路径不可写、中间值为 null 或值不能转换时抛出。</exception>
    /// <remarks>值类型变量请使用 <c>ref</c> 重载；此重载接收装箱对象，直接传入结构体变量不会回写原变量。
    /// 嵌套值类型逐层回写时，用户定义的 setter 抛异常可能留下已执行的副作用；写入不提供事务性回滚。</remarks>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static void SetMemberValueByPath(object instance, string memberPath, object value, MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        EnsureMemberPathLimits(instance.GetType(), memberPath);
        if (TrySetMemberValueByPath(instance, instance.GetType(), memberPath, value, options))
            return;
        throw new InvalidOperationException($"无法设置对象类型“{instance.GetType().FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 按显式根类型设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="rootType">路径解析的根类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按显式根类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    internal static void SetMemberValueByPath(object instance, Type rootType, string memberPath, object value,
        MemberPathOptions options = null)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (rootType == null)
            throw new ArgumentNullException(nameof(rootType));
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        EnsureMemberPathLimits(rootType, memberPath);
        if (TrySetMemberValueByPath(instance, rootType, memberPath, value, options))
            return;
        throw new InvalidOperationException($"无法设置对象类型“{rootType.FullName}”的成员路径“{memberPath}”。");
    }

    /// <summary>
    /// 判断类型是否包含可访问的成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>路径可访问时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool HasMember(Type type, string memberPath, MemberPathOptions options = null) =>
        type != null && HasMemberPath(type, memberPath, options);

    /// <summary>
    /// 判断对象是否包含可访问的成员路径。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>对象非 null 且路径可访问时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool HasMember(object instance, string memberPath, MemberPathOptions options = null) =>
        instance != null && HasMemberPath(instance.GetType(), memberPath, options);

    /// <summary>
    /// 尝试获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">成功时为路径的值；失败时为 null。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功读取路径值时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TryGetMemberValue(object instance, string memberPath, out object value, MemberPathOptions options = null) =>
        TryGetMemberValueByPath(instance, memberPath, out value, options);

    /// <summary>
    /// 获取成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员路径的值。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static object GetMemberValue(object instance, string memberPath, MemberPathOptions options = null) =>
        GetMemberValueByPath(instance, memberPath, options);

    /// <summary>
    /// 尝试设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成功设置路径值时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TrySetMemberValue(object instance, string memberPath, object value, MemberPathOptions options = null) =>
        TrySetMemberValueByPath(instance, memberPath, value, options);

    /// <summary>尝试设置结构体成员路径并回写原变量。</summary>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TrySetMemberValue<T>(ref T instance, string memberPath, object value,
        MemberPathOptions options = null) where T : struct =>
        TrySetMemberValueByPath(ref instance, memberPath, value, options);

    /// <summary>
    /// 设置成员路径的值。
    /// </summary>
    /// <param name="instance">路径起始对象。</param>
    /// <param name="memberPath">以英文句点分隔的成员路径。</param>
    /// <param name="value">要设置的值。</param>
    /// <param name="options">成员路径访问选项。</param>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按对象运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static void SetMemberValue(object instance, string memberPath, object value, MemberPathOptions options = null) =>
        SetMemberValueByPath(instance, memberPath, value, options);

    /// <summary>设置结构体成员路径并回写原变量。</summary>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static void SetMemberValue<T>(ref T instance, string memberPath, object value,
        MemberPathOptions options = null) where T : struct =>
        SetMemberValueByPath(ref instance, memberPath, value, options);

    /// <summary>
    /// 尝试解析成员路径。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">成员路径。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <param name="members">解析成功后的成员列表。</param>
    /// <returns>解析成功时返回 true；否则返回 false。</returns>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径解析依赖运行时属性和字段元数据。")]
#endif
    private static bool TryResolveMemberPath(Type type, string memberPath, MemberPathOptions options, out IReadOnlyList<MemberInfo> members)
    {
        members = Array.Empty<MemberInfo>();
        if (memberPath == null || memberPath.Length > MaxMemberPathLength || string.IsNullOrWhiteSpace(memberPath))
            return false;

        options ??= DefaultMemberPathOptions;
        var normalizedPath = NormalizeMemberPath(type, memberPath);
        var ignoreCase = options.IgnoreCase;
        var includeFields = options.IncludeFields;
        var includeNonPublic = options.IncludeNonPublic;
        if (ReflectionCacheScope.IsCollectible(type))
        {
            var direct = ResolveMemberPath(type, normalizedPath, ignoreCase, includeFields, includeNonPublic);
            if (direct == null)
                return false;
            members = direct;
            return true;
        }

        var cache = MemberPathCache.GetValue(type, _ => new MemberPathLookup());
        var fastPath = System.Threading.Volatile.Read(ref cache.FastPath);
        if (fastPath != null && fastPath.Matches(normalizedPath, ignoreCase, includeFields, includeNonPublic))
        {
            CoreCacheMetrics.RecordHit();
            members = fastPath.Members;
            return true;
        }
        var keyPath = ignoreCase ? normalizedPath.ToUpperInvariant() : normalizedPath;
        var key = !ignoreCase && includeFields && !includeNonPublic
            ? "\0" + keyPath
            : "\u0001" + $"{ignoreCase}:{includeFields}:{includeNonPublic}:{keyPath}";
        if (cache.Paths.TryGetValue(key, out var existing))
        {
            CoreCacheMetrics.Record(existing);
            var cached = existing.Value;
            if (cached == null)
                return false;
            System.Threading.Volatile.Write(ref cache.FastPath,
                new MemberPathFastPath(normalizedPath, ignoreCase, includeFields, includeNonPublic, cached));
            members = cached;
            return true;
        }

        Lazy<MemberInfo[]> lazy = null;
        var bypass = false;
        lock (cache.AdmissionGate)
        {
            if (cache.Paths.TryGetValue(key, out existing))
            {
                CoreCacheMetrics.Record(existing);
                lazy = existing;
            }
            else if (cache.Paths.Count >= MemberPathCacheCapacity)
            {
                CoreCacheMetrics.RecordBypass();
                bypass = true;
            }
            else
            {
                lazy = CoreCacheMetrics.CreateLazy(() =>
                    ResolveMemberPath(type, normalizedPath, ignoreCase, includeFields, includeNonPublic));
                cache.Paths.TryAdd(key, lazy);
                CoreCacheMetrics.RecordMiss();
            }
        }

        var resolved = bypass
            ? ResolveMemberPath(type, normalizedPath, ignoreCase, includeFields, includeNonPublic)
            : lazy.Value;
        if (resolved == null)
            return false;
        System.Threading.Volatile.Write(ref cache.FastPath,
            new MemberPathFastPath(normalizedPath, ignoreCase, includeFields, includeNonPublic, resolved));
        members = resolved;
        return true;
    }

    private static MemberPathOptions SnapshotOptions(MemberPathOptions options) => options == null
        ? DefaultMemberPathOptions
        : new MemberPathOptions
        {
            IgnoreCase = options.IgnoreCase,
            IncludeFields = options.IncludeFields,
            IncludeNonPublic = options.IncludeNonPublic,
            ConvertAssignedValue = options.ConvertAssignedValue
        };

    /// <summary>设置结构体成员路径并将修改回写到原变量。</summary>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static bool TrySetMemberValueByPath<T>(ref T instance, string memberPath, object value,
        MemberPathOptions options = null) where T : struct
    {
        object boxed = instance;
        if (!TrySetMemberValueByPath(boxed, memberPath, value, options))
            return false;
        instance = (T)boxed;
        return true;
    }

    /// <summary>设置结构体成员路径并将修改回写到原变量。</summary>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型扫描属性和字段，裁剪时必须保留路径涉及的成员元数据。")]
#endif
    public static void SetMemberValueByPath<T>(ref T instance, string memberPath, object value,
        MemberPathOptions options = null) where T : struct
    {
        if (string.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("成员路径不能为空", nameof(memberPath));
        EnsureMemberPathLimits(typeof(T), memberPath);
        if (!TrySetMemberValueByPath(ref instance, memberPath, value, options))
            throw new InvalidOperationException($"无法设置对象类型“{typeof(T).FullName}”的成员路径“{memberPath}”。");
    }

#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径解析依赖运行时属性和字段元数据。")]
#endif
    private static MemberInfo[] ResolveMemberPath(Type type, string normalizedPath, bool ignoreCase, bool includeFields, bool includeNonPublic)
    {
        System.Threading.Interlocked.Increment(ref _memberPathResolveCount);
        if (!IsWithinMemberPathDepth(normalizedPath))
            return null;
        var options = new MemberPathOptions { IgnoreCase = ignoreCase, IncludeFields = includeFields, IncludeNonPublic = includeNonPublic };
        var segments = normalizedPath.Split('.');
        if (segments.Any(string.IsNullOrWhiteSpace))
            return null;

        var result = new List<MemberInfo>(segments.Length);
        var currentType = type;
        foreach (var segment in segments)
        {
            var member = FindInstanceMember(currentType, segment, options);
            if (member == null)
                return null;
            result.Add(member);
            currentType = GetMemberType(member);
        }

        return result.ToArray();
    }

    /// <summary>
    /// 移除成员路径中可选的完全限定类型前缀。
    /// </summary>
    /// <param name="type">路径起始类型。</param>
    /// <param name="memberPath">原始成员路径。</param>
    /// <returns>规范化后的成员路径。</returns>
    private static string NormalizeMemberPath(Type type, string memberPath)
    {
        var typeName = type.FullName;
        if (!string.IsNullOrEmpty(typeName) && memberPath.StartsWith(typeName + ".", StringComparison.Ordinal))
            return memberPath.Substring(typeName.Length + 1);
        return memberPath;
    }

    private static bool IsWithinMemberPathDepth(string path)
    {
        var depth = 1;
        foreach (var character in path)
        {
            if (character == '.' && ++depth > MaxMemberPathDepth)
                return false;
        }
        return true;
    }

    private static void EnsureMemberPathLimits(Type type, string path)
    {
        if (path == null)
            return;
        if (path.Length > MaxMemberPathLength || !IsWithinMemberPathDepth(NormalizeMemberPath(type, path)))
            throw new ArgumentException($"成员路径最多支持 {MaxMemberPathLength} 个字符和 {MaxMemberPathDepth} 层成员。", "memberPath");
    }

    /// <summary>在类型上查找指定名称的实例属性或字段。</summary>
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("成员路径按运行时类型查找属性和字段。")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "成员路径的当前类型来自对象运行时值或前一段成员类型，无法静态表达完整成员保留集合。")]
#endif
    private static MemberInfo FindInstanceMember(Type type, string name, MemberPathOptions options)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public;
        if (options.IncludeNonPublic)
            flags |= BindingFlags.NonPublic;
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var properties = TypeReflections.TypeCacheManager.GetTypeProperties(type, flags)
            .Where(candidate => candidate.GetIndexParameters().Length == 0
                                && string.Equals(candidate.Name, name, comparison))
            .Take(2).ToArray();
        if (properties.Length > 1)
            throw new AmbiguousMatchException($"类型“{type.FullName}”中存在多个与“{name}”匹配的属性。");
        if (properties.Length == 1)
            return properties[0];
        if (!options.IncludeFields)
            return null;
        return SelectClosestField(type, name, TypeReflections.TypeCacheManager.GetTypeFields(type, flags), comparison);
    }

    /// <summary>
    /// 判断成员是否可读。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员可读时返回 true；否则返回 false。</returns>
    private static bool CanRead(MemberInfo member, MemberPathOptions options)
    {
        return member switch
        {
            PropertyInfo property => CanReadProperty(property, options),
            FieldInfo field => options.IncludeNonPublic || field.IsPublic,
            _ => false
        };
    }

    /// <summary>
    /// 判断成员是否可写。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>成员可写时返回 true；否则返回 false。</returns>
    private static bool CanWrite(MemberInfo member, MemberPathOptions options)
    {
        return member switch
        {
            PropertyInfo property => CanWriteProperty(property, options),
            FieldInfo field => (options.IncludeNonPublic || field.IsPublic) && !field.IsInitOnly && !field.IsLiteral,
            _ => false
        };
    }

    /// <summary>
    /// 判断属性是否可读取。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>允许调用 getter 时返回 true；否则返回 false。</returns>
    private static bool CanReadProperty(PropertyInfo property, MemberPathOptions options)
    {
        var getter = property.GetMethod;
        return getter != null && (options.IncludeNonPublic || getter.IsPublic);
    }

    /// <summary>
    /// 判断属性是否可写入。
    /// </summary>
    /// <param name="property">属性元数据。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <returns>允许调用 setter 时返回 true；否则返回 false。</returns>
    private static bool CanWriteProperty(PropertyInfo property, MemberPathOptions options)
    {
        var setter = property.SetMethod;
        return setter != null && !PropertyInfoExtensions.IsInitOnlySetter(property)
               && (options.IncludeNonPublic || setter.IsPublic);
    }

    /// <summary>
    /// 根据选项获取可写入成员的值。
    /// </summary>
    /// <param name="value">源值。</param>
    /// <param name="destinationType">目标成员类型。</param>
    /// <param name="options">成员路径访问选项。</param>
    /// <param name="convertedValue">可写入的值。</param>
    /// <returns>值可写入目标成员时返回 true；否则返回 false。</returns>
    private static bool TryGetAssignedValue(object value, Type destinationType, MemberPathOptions options, out object convertedValue)
    {
        // 关闭转换时仅允许直接赋值，包括保留可直接赋值的 DBNull 对象。
        if (!options.ConvertAssignedValue && value != null)
        {
            convertedValue = value;
            return destinationType.IsInstanceOfType(value);
        }

        if (value == null || value == DBNull.Value)
        {
            convertedValue = null;
            return !destinationType.IsValueType || Nullable.GetUnderlyingType(destinationType) != null;
        }

        if (destinationType.IsInstanceOfType(value))
        {
            convertedValue = value;
            return true;
        }

        if (!options.ConvertAssignedValue)
        {
            convertedValue = null;
            return false;
        }

        if (!MapperHelper.TryConvertValue(value, destinationType, out convertedValue))
            return false;
        if (convertedValue == null)
            return !destinationType.IsValueType || Nullable.GetUnderlyingType(destinationType) != null;
        return destinationType.IsInstanceOfType(convertedValue)
               || Nullable.GetUnderlyingType(destinationType)?.IsInstanceOfType(convertedValue) == true;
    }

    /// <summary>
    /// 获取成员类型。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <returns>成员类型。</returns>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member))
        };
    }

    /// <summary>
    /// 获取成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <returns>成员值。</returns>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static object GetMemberValue(MemberInfo member, object instance)
    {
        return member switch
        {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member))
        };
    }

    /// <summary>
    /// 设置成员值。
    /// </summary>
    /// <param name="member">成员元数据。</param>
    /// <param name="instance">成员所属对象。</param>
    /// <param name="value">要设置的值。</param>
    /// <exception cref="ArgumentException">当成员不是属性或字段时抛出。</exception>
    private static void SetMemberValue(MemberInfo member, object instance, object value)
    {
        switch (member)
        {
            case PropertyInfo property:
                property.SetValue(instance, value);
                return;
            case FieldInfo field:
                field.SetValue(instance, value);
                return;
            default:
                throw new ArgumentException($"成员“{member.Name}”不是属性或字段", nameof(member));
        }
    }
}
