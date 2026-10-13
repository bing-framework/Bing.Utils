using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency;

/// <summary>
/// Diamond 图中的第二条直接依赖边，和主依赖共享同一间接依赖。
/// </summary>
[Description("Third-party non-trimmable secondary dependency")]
public static class ThirdPartyBoundarySecondaryDependency
{
    /// <summary>
    /// 返回用于识别第二条依赖边的值。
    /// </summary>
    [Description("Third-party secondary dependency bonus")]
    public static int GetBonus() => 31 + ThirdPartyBoundaryTransitiveDependency.GetBonus();
}
