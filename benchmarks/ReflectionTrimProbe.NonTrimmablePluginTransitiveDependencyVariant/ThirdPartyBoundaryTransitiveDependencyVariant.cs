using System.ComponentModel;

namespace ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency;

/// <summary>
/// 与默认第二层依赖保持程序集身份一致但实现不同的第三方依赖变体。
/// </summary>
[Description("Third-party non-trimmable transitive dependency variant")]
public static class ThirdPartyBoundaryTransitiveDependency
{
    /// <summary>
    /// 返回用于识别依赖图变体的值。
    /// </summary>
    [Description("Third-party transitive dependency bonus variant")]
    public static int GetBonus() => 13;
}
