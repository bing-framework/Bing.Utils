using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency;

/// <summary>
/// 三分支依赖图中的第三条直接依赖边。
/// </summary>
[Description("Third-party non-trimmable tertiary dependency")]
public static class ThirdPartyBoundaryTertiaryDependency
{
    /// <summary>
    /// 返回用于识别第三条依赖边的值。
    /// </summary>
    [Description("Third-party tertiary dependency bonus")]
    public static int GetBonus() => 41 + ThirdPartyBoundaryTransitiveDependency.GetBonus();
}
