using System.ComponentModel;

namespace ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

/// <summary>
/// 未声明 IsTrimmable 的第三方插件第二层依赖。
/// </summary>
[Description("Third-party non-trimmable transitive dependency")]
public static class ThirdPartyBoundaryTransitiveDependency
{
    /// <summary>
    /// 返回供直接依赖调用的第二层依赖值。
    /// </summary>
    [Description("Third-party transitive dependency bonus")]
    public static int GetBonus() => 7;
}
