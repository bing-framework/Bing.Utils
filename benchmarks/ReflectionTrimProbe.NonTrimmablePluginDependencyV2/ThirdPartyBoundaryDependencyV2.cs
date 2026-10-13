using System.ComponentModel;

namespace ReflectionTrimProbe.NonTrimmablePluginDependency;

/// <summary>
/// 与插件编译时依赖同名但版本不同的第三方依赖替代物。
/// </summary>
[Description("Third-party non-trimmable dependency v2")]
public static class ThirdPartyBoundaryDependency
{
    /// <summary>
    /// 返回用于识别已加载依赖版本的值。
    /// </summary>
    [Description("Third-party dependency bonus v2")]
    public static int GetBonus() => 29;
}
