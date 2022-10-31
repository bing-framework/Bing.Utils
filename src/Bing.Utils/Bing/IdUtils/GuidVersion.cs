namespace Bing.IdUtils;

/// <summary>
/// 已知值的 GUID 版本字段
/// </summary>
public enum GuidVersion
{
    /// <summary>
    /// 基于时间的版本
    /// </summary>
    /// <remarks>日期时间和MAC地址</remarks>
    TimeBased = 1,

    /// <summary>
    /// DCE 安全版本
    /// </summary>
    /// <remarks>日期时间和MAC地址，DCE安全版本</remarks>
    DceSecurity = 2,

    /// <summary>
    /// 基于名称的版本（本文件中规定使用 MD5 散列）
    /// </summary>
    /// <remarks>基于名字空间名称</remarks>
    NameBasedMd5 = 3,

    /// <summary>
    /// 随机或伪随机生成的版本
    /// </summary>
    /// <remarks>随机</remarks>
    Random = 4,

    /// <summary>
    /// 基于名称的版本（本文件中规定使用 SHA-1 散列）
    /// </summary>
    /// <remarks>基于名字空间名称</remarks>
    NameBasedSha1 = 5,
}