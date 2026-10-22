using System.ComponentModel;
using ReflectionTrimProbe.NonTrimmablePluginDeepDependency;

namespace ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

/// <summary>
/// 引用第三层依赖、并保持现有 transitive 程序集身份的深层变体。
/// </summary>
[Description("Third-party non-trimmable deep transitive dependency variant")]
public static class ThirdPartyBoundaryTransitiveDependency
{
    /// <summary>
    /// 返回包含第三层依赖贡献的识别值。
    /// </summary>
    [Description("Third-party deep transitive dependency bonus variant")]
    public static int GetBonus() => 9 + ThirdPartyBoundaryDeepDependency.GetBonus();
}
