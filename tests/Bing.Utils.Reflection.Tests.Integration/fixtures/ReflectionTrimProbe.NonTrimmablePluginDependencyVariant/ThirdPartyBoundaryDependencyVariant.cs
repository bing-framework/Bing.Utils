using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginDependency;

/// <summary>
/// 与默认直接依赖保持程序集身份一致但实现不同的第三方依赖变体。
/// </summary>
[Description("Third-party non-trimmable dependency variant")]
public static class ThirdPartyBoundaryDependency
{
    /// <summary>
    /// 返回用于识别直接和间接依赖图变体的值。
    /// </summary>
    [Description("Third-party dependency bonus variant")]
    public static int GetBonus() => 17 + ThirdPartyBoundaryTransitiveDependency.GetBonus();
}
