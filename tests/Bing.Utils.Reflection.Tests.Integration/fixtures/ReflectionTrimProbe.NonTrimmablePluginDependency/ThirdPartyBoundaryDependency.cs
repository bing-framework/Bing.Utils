using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginDependency;

/// <summary>
/// 未声明 IsTrimmable 的第三方插件依赖。
/// </summary>
[Description("Third-party non-trimmable dependency")]
public static class ThirdPartyBoundaryDependency
{
    /// <summary>
    /// 返回供插件方法调用的依赖值。
    /// </summary>
    [Description("Third-party dependency bonus")]
    public static int GetBonus() => 11 + ThirdPartyBoundaryTransitiveDependency.GetBonus();
}
