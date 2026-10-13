using System.ComponentModel;

namespace ReflectionTrimProbe.NonTrimmablePluginDeepDependency;

/// <summary>
/// 三层依赖图中最底层的未声明可裁剪第三方依赖。
/// </summary>
[Description("Third-party non-trimmable deep dependency")]
public static class ThirdPartyBoundaryDeepDependency
{
    /// <summary>
    /// 返回用于识别深层依赖的固定值。
    /// </summary>
    [Description("Third-party deep dependency bonus")]
    public static int GetBonus() => 5;
}
