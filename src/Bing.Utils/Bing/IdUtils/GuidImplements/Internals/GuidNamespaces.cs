namespace Bing.IdUtils.GuidImplements.Internals;

/// <summary>
/// GUID 命名空间
/// </summary>
public static class GuidNamespaces
{
    /// <summary>
    /// For names that are fully-qualified domain names.
    /// </summary>
    public static readonly Guid Dns = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    /// <summary>
    /// For names that are URLs.
    /// </summary>
    public static readonly Guid Url = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    /// <summary>
    /// For names that are ISO OIDs.
    /// </summary>
    public static readonly Guid Oid = Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

    /// <summary>
    /// For names that are X.500 DNs (in DER or a text output format).
    /// </summary>
    public static readonly Guid X500 = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");
}